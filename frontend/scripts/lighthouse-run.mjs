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
 *
 * `0016b` **C13** · **D19** — ÜÇÜNCÜ adres ilan detayıdır ve aynı eşiklerle ölçülür.
 * Adres betikte sabit DEĞİL (**D30**): seed ilanlarının adresi her kurulumda farklı, bu
 * yüzden ölçümden önce liste ucundan okunur. Seçilen ilan **en az bir hazır görsel**
 * taşımalıdır; yoksa koşu kırmızıdır (**D36**, AC-80) — görselsiz ilan seçilseydi LCP öğesi
 * metin olur ve ilk görselin LCP'si hiç ölçülmeden "geçti" yazılırdı. Aynı ölçümde LCP
 * öğesinin galerinin ilk görseli olduğu da her koşuda doğrulanır (AC-83).
 */
/*
 * `0022` — perf ortamı ana sayfa metinlerini her alan sınırında tohumlar (`perf.yml`,
 * `Development__SeedHomeTexts`, D11). Bu yüzden iki hedef iki kriteri de ölçer:
 *   0022/AC-21: Her alanı sınırında (120/500, satır sonlu) kaydedilmiş metinlerle ana sayfa için performans kapısı (LCP < 2,5 sn, CLS < 0,1) yeşildir (D11).
 *   0022/AC-24: Liste için performans kapısı yeşil kalır.
 * Ana sayfanın LCP öğesi her koşuda loglanır ve rapora yazılır (`lcpElement`, D22).
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
  {
    spec: '0016b',
    page: 'ilan detayı',
    resolvePath: detayAdresi,
    reportPath: '../docs/evidence/0016b-performance.json',
    // AC-83 — LCP öğesi galerinin ilk görseli olmalı; `data-testid` React'in bastığı işaret.
    lcpElementMustMatch: 'data-testid="gallery-image"',
  },
]

/** Liste ucunun sayfa boyutu; ilk sayfada görselli ilan yoksa sonraki sayfalara bakılır. */
const LIST_PAGE_SIZE = 24
const LIST_PAGE_LIMIT = 5

/**
 * `0016b` D30/D36 — ölçülecek detay adresi: liste sırasındaki, EN AZ BİR hazır görseli
 * olan ilk ilan. "Hazır görsel" detay yanıtının `images` listesidir (yalnız hazır
 * görselleri taşır, D14); kartın kapak alanı bu soruyu cevaplamaz (X10).
 */
async function detayAdresi() {
  for (let page = 1; page <= LIST_PAGE_LIMIT; page += 1) {
    const list = await getJson(
      `/api/catalog/showcase-listings?locale=tr&page=${page}&pageSize=${LIST_PAGE_SIZE}`,
    )

    for (const item of list.items) {
      const detail = await getJson(
        `/api/catalog/showcase-listings/${encodeURIComponent(item.slug)}?locale=tr`,
      )

      if (Array.isArray(detail.images) && detail.images.length > 0) {
        return `/tr/cars/${encodeURIComponent(item.slug)}`
      }
    }

    if (list.items.length < LIST_PAGE_SIZE) {
      break
    }
  }

  return null
}

async function getJson(path) {
  const response = await fetch(`${BASE_URL}${path}`)

  if (!response.ok) {
    throw new Error(`${path} → ${response.status}`)
  }

  return response.json()
}
const RUNS = 3
const LCP_THRESHOLD_MS = 2500
const CLS_THRESHOLD = 0.1

const median = (values) => {
  const sorted = [...values].sort((a, b) => a - b)
  return sorted[Math.floor(sorted.length / 2)]
}

/**
 * LCP öğesinin düğümü. Lighthouse 13 `largest-contentful-paint-element` denetimini
 * kaldırdı; öğe artık `lcp-breakdown-insight`'ın `node` kalemindedir. Eski yol bu sürümde
 * her koşuda "bilinmiyor" yazıyordu — `0016b` AC-83 onu okuyana kadar fark edilmedi.
 */
function lcpNode(lhr) {
  const insight = lhr.audits['lcp-breakdown-insight']?.details?.items ?? []
  const fromInsight = insight.find((item) => item.type === 'node')

  return (
    fromInsight ??
    lhr.audits['largest-contentful-paint-element']?.details?.items?.[0]?.items?.[0]?.node
  )
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
    const lcpElement = lcpNode(lhr)?.snippet ?? 'bilinmiyor'

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
  const lcpElementOk =
    target.lcpElementMustMatch === undefined ||
    results.every((r) => r.lcpElement.includes(target.lcpElementMustMatch))
  const met = lcpMedian < LCP_THRESHOLD_MS && clsMedian < CLS_THRESHOLD && lcpElementOk

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
      lcpElement: r.lcpElement,
    })),
    ...(target.lcpElementMustMatch === undefined
      ? {}
      : { lcpElementExpected: target.lcpElementMustMatch, lcpElementMet: lcpElementOk }),
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
  if (target.lcpElementMustMatch !== undefined) {
    console.log(`  LCP öğesi galerinin ilk görseli: ${lcpElementOk ? 'evet' : 'HAYIR'} (her koşu)`)
  }
  console.log(`  ${met ? 'YEŞİL' : 'KIRMIZI'} — ${target.page} (${target.spec})`)

  return met
}

const chrome = await chromeLauncher.launch({ chromeFlags: ['--headless=new', '--no-sandbox'] })
const outcomes = []

try {
  // Bir adres kırmızı olsa da diğeri ölçülür: iki rapor da her koşuda yazılır (D10).
  for (const target of TARGETS) {
    const path = target.path ?? (await target.resolvePath())

    if (path === null) {
      // D36/AC-80 — ölçülecek görselli ilan yok: "geçti" yazılmaz, koşu kırmızıdır.
      console.error(`${target.page} — liste ucunda en az bir hazır görseli olan ilan YOK.`)
      outcomes.push({ target: { ...target, path: '(görselli ilan yok)' }, met: false })
      continue
    }

    const url = `${BASE_URL}${path}`
    console.log(`${target.page} — ${url}`)
    outcomes.push({
      target: { ...target, path },
      met: writeReport(target, url, await measure(chrome.port, url)),
    })
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

console.log(`YEŞİL — C3 uç eşikleri ${TARGETS.length} adreste de karşılandı.`)
