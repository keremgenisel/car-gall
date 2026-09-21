import { writeFileSync, mkdirSync, readFileSync, existsSync } from 'node:fs'
import lighthouse from 'lighthouse'
import * as chromeLauncher from 'chrome-launcher'

/**
 * C3 uç ölçüm protokolü — HARFİYEN: Lighthouse MOBİL profili (4x CPU kısıtı, Slow 4G),
 * 3 koşu, MEDYAN alınır. Eşikler: LCP < 2,5 s (AC-35) ve CLS < 0,1 (AC-25).
 */
const URL_TO_TEST = process.env.PERF_URL ?? 'http://localhost:5080/tr/'
const RUNS = 3
const LCP_THRESHOLD_MS = 2500
const CLS_THRESHOLD = 0.1

const chrome = await chromeLauncher.launch({ chromeFlags: ['--headless=new', '--no-sandbox'] })

const results = []

try {
  for (let run = 1; run <= RUNS; run += 1) {
    const { lhr } = await lighthouse(
      URL_TO_TEST,
      { port: chrome.port, output: 'json', logLevel: 'error' },
      // Lighthouse'un hazır mobil ayarı: 4x CPU kısıtı ve Slow 4G ağ profili.
      undefined,
    )

    const lcp = lhr.audits['largest-contentful-paint'].numericValue
    const cls = lhr.audits['cumulative-layout-shift'].numericValue
    const lcpElement =
      lhr.audits['largest-contentful-paint-element']?.details?.items?.[0]?.items?.[0]?.node
        ?.snippet ?? 'bilinmiyor'

    console.log(
      `koşu ${run}: LCP ${Math.round(lcp)} ms · CLS ${cls.toFixed(3)} · LCP öğesi: ${lcpElement}`,
    )
    results.push({ lcp, cls, lcpElement })
  }
} finally {
  await chrome.kill()
}

const median = (values) => {
  const sorted = [...values].sort((a, b) => a - b)
  return sorted[Math.floor(sorted.length / 2)]
}

const lcpMedian = median(results.map((r) => r.lcp))
const clsMedian = median(results.map((r) => r.cls))

mkdirSync('../docs/evidence', { recursive: true })

const reportPath = '../docs/evidence/0001-performance.json'

// Sunucu ölçümü aynı dosyanın "server" bölümünü yazar; birbirinin üstüne yazmazlar.
const report = existsSync(reportPath) ? JSON.parse(readFileSync(reportPath, 'utf8')) : {}

report.spec = '0001'
report.client = {
  url: URL_TO_TEST,
  protocol: 'Lighthouse mobil profili (4x CPU kısıtı, Slow 4G), 3 koşu, medyan (spec 0001 C3)',
  runs: results.map((r) => ({
    lcpMilliseconds: Math.round(r.lcp),
    cumulativeLayoutShift: Number(r.cls.toFixed(3)),
  })),
  median: {
    lcpMilliseconds: Math.round(lcpMedian),
    cumulativeLayoutShift: Number(clsMedian.toFixed(3)),
  },
  thresholds: { lcpMilliseconds: LCP_THRESHOLD_MS, cumulativeLayoutShift: CLS_THRESHOLD },
  met: lcpMedian < LCP_THRESHOLD_MS && clsMedian < CLS_THRESHOLD,
}

writeFileSync(reportPath, JSON.stringify(report, null, 2) + '\n')

console.log(`medyan LCP: ${Math.round(lcpMedian)} ms (eşik ${LCP_THRESHOLD_MS} ms)`)
console.log(`medyan CLS: ${clsMedian.toFixed(3)} (eşik ${CLS_THRESHOLD})`)

if (lcpMedian >= LCP_THRESHOLD_MS || clsMedian >= CLS_THRESHOLD) {
  console.error(
    'KIRMIZI — C3 uç eşiği karşılanmadı. AP-12: önce ölç, TEK optimizasyon, yeniden ölç.',
  )
  process.exit(1)
}

console.log('YEŞİL — C3 uç eşikleri karşılandı.')
