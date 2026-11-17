import { writeFileSync, mkdirSync, readFileSync, existsSync } from 'node:fs'
import lighthouse from 'lighthouse'
import * as chromeLauncher from 'chrome-launcher'

/**
 * C3 uç ölçüm protokolü — HARFİYEN: Lighthouse MOBİL profili (4x CPU kısıtı, Slow 4G),
 * 3 koşu, MEDYAN alınır. Eşikler: LCP < 2,5 s ve CLS < 0,1 (`0001` AC-35, AC-25).
 *
 * `0014` **D10** — İKİ adres ölçülür ve her biri eşiği AYRI AYRI karşılar: ana sayfa
 * (`/tr/` → `0014` AC-59, AC-60) ve araç listesi (`/tr/cars` → `0014` AC-61, `0001`'in
 * eşikleri). Liste `/{locale}/cars`'a taşınmadan önce bu betik `/tr/`'yi ölçüyordu; adresi
 * yalnız değiştirmek ana sayfayı, yalnız bırakmak listeyi ölçümsüz bırakırdı (K4). Her
 * adres kendi spec'inin raporuna yazılır; `0001`'in raporu listeyi ölçmeye devam eder.
 */
const BASE_URL = process.env.PERF_BASE_URL ?? 'http://localhost:5080'
const TARGETS = [
  {
    spec: '0014',
    page: 'ana sayfa',
    path: '/tr/',
    reportPath: '../docs/evidence/0014-performance.json',
  },
  {
    spec: '0001',
    page: 'araç listesi',
    path: '/tr/cars',
    reportPath: '../docs/evidence/0001-performance.json',
  },
]
const RUNS = 3
const LCP_THRESHOLD_MS = 2500
const CLS_THRESHOLD = 0.1

const median = (values) => {
  const sorted = [...values].sort((a, b) => a - b)
  return sorted[Math.floor(sorted.length / 2)]
}

async function measure(port, url) {
  const results = []

  for (let run = 1; run <= RUNS; run += 1) {
    const { lhr } = await lighthouse(
      url,
      { port, output: 'json', logLevel: 'error' },
      // Lighthouse'un hazır mobil ayarı: 4x CPU kısıtı ve Slow 4G ağ profili.
      undefined,
    )

    const lcp = lhr.audits['largest-contentful-paint'].numericValue
    const cls = lhr.audits['cumulative-layout-shift'].numericValue
    const lcpElement =
      lhr.audits['largest-contentful-paint-element']?.details?.items?.[0]?.items?.[0]?.node
        ?.snippet ?? 'bilinmiyor'

    console.log(
      `  koşu ${run}: LCP ${Math.round(lcp)} ms · CLS ${cls.toFixed(3)} · LCP öğesi: ${lcpElement}`,
    )
    results.push({ lcp, cls, lcpElement })
  }

  return results
}

function writeReport(target, url, results) {
  const lcpMedian = median(results.map((r) => r.lcp))
  const clsMedian = median(results.map((r) => r.cls))
  const met = lcpMedian < LCP_THRESHOLD_MS && clsMedian < CLS_THRESHOLD

  mkdirSync('../docs/evidence', { recursive: true })

  // Sunucu ölçümü aynı dosyanın "server" bölümünü yazar; birbirinin üstüne yazmazlar.
  const report = existsSync(target.reportPath)
    ? JSON.parse(readFileSync(target.reportPath, 'utf8'))
    : {}

  report.spec = target.spec
  report.client = {
    url,
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
    met,
  }

  writeFileSync(target.reportPath, JSON.stringify(report, null, 2) + '\n')

  console.log(`  medyan LCP: ${Math.round(lcpMedian)} ms (eşik ${LCP_THRESHOLD_MS} ms)`)
  console.log(`  medyan CLS: ${clsMedian.toFixed(3)} (eşik ${CLS_THRESHOLD})`)
  console.log(`  ${met ? 'YEŞİL' : 'KIRMIZI'} — ${target.page} (${target.spec})`)

  return met
}

const chrome = await chromeLauncher.launch({ chromeFlags: ['--headless=new', '--no-sandbox'] })
const outcomes = []

try {
  // Bir adres kırmızı olsa da diğeri ölçülür: iki rapor da her koşuda yazılır (D10).
  for (const target of TARGETS) {
    const url = `${BASE_URL}${target.path}`
    console.log(`${target.page} — ${url}`)
    outcomes.push({ target, met: writeReport(target, url, await measure(chrome.port, url)) })
  }
} finally {
  await chrome.kill()
}

const failed = outcomes.filter((outcome) => !outcome.met)

if (failed.length > 0) {
  console.error(
    `KIRMIZI — C3 uç eşiği karşılanmadı: ${failed.map((f) => f.target.path).join(', ')}. ` +
      'AP-12: önce ölç, TEK optimizasyon, yeniden ölç.',
  )
  process.exit(1)
}

console.log('YEŞİL — C3 uç eşikleri iki adreste de karşılandı.')
