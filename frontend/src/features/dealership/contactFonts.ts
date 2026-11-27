import type { AppLocale } from '../../shared/lib/locale'

/**
 * `0028` **AC-77** · AP-08 (Proje Yöneticisi kararı, 2026-11-27) — iletişim sayfasının şube bölümleri,
 * kullandıkları yazı tipi yüzleri yüklenince basılır; bekleme EN ÇOK {@link FONT_WAIT_MS} ms sürer.
 *
 * <b>Neden:</b> `font-display: swap` web fontu gelince metni yeniden dizer (ölçüldü, fontlar bekletilip
 * bırakılarak: tr 320 px'te adres bir satır fazla kırılıyor ve altındaki her şey 24 px kayıyordu; ar'da
 * karışık yazılı satır kutusu 1 px). Bölümler yüzlerden ÖNCE basılsaydı bu kayma CLS'e girerdi.
 *
 * <b>Neden `document.fonts.ready` değil:</b> o yalnız O ANA kadar istenmiş yüzleri bekler; bölümde ilk
 * kez kullanılan bir yüz (600 ağırlık, Türkçe harflerin `latin-ext` alt kümesi, Arapça yüz) ondan sonra
 * istenir ve yine kaydırır. Her yüz `document.fonts.load` ile, o dilin örnek metniyle AÇIKÇA yüklenir.
 *
 * <b>Üst süre</b> cihaz saatine değil zamanlayıcıya bağlıdır; süre dolunca bölümler basılır, sonra gelen
 * font kayma üretebilir (bilinen bedel). Font isteği başarısız olursa (404, çevrimdışı) bekleme HEMEN
 * biter. Söz dil başına bir kez kurulur: StrictMode'un ikinci render'ı aynı sözü okur, ikinci bekleme yok.
 */
export const FONT_WAIT_MS = 1500

/** `--font-family-base` (tr, en) ve `--font-family-arabic` (ar) yığınının ilk ailesi (`fonts.css`). */
const FAMILIES: Readonly<Record<AppLocale, string>> = {
  tr: 'Space Grotesk',
  en: 'Space Grotesk',
  ar: 'Readex Pro',
}

/**
 * Yüzün `unicode-range` alt kümelerini tetikleyen örnek: Latin, Türkçe harfler (`latin-ext`) ve
 * rakamlar; ar'da Arapça ve sayfaya varsayılan dilden düşen Latin/Türkçe metin.
 */
const SAMPLES: Readonly<Record<AppLocale, string>> = {
  tr: 'İletişim Şubesi Ağrı Çğış 0123456789',
  en: 'Contact Branch 0123456789 Ağrı Şİ',
  ar: 'اتصل بنا فرع Kadıköy Şİğ 0123456789',
}

/** Token katmanının iki ağırlığı (`--font-weight-regular`, `--font-weight-semibold`). */
const WEIGHTS = ['400', '600'] as const

/** React `use`'unun okuduğu durum alanı: çözülmüş söz ikinci render'da askıya almaz. */
type Thenable = Promise<void>

function fulfil(promise: Thenable): void {
  Object.assign(promise, { status: 'fulfilled', value: undefined })
}

const pending = new Map<AppLocale, Thenable>()

/** Hazır söz: React'in `use`'u askıya almadan geçer (yazı tipi API'si olmayan ortam, ör. jsdom). */
function settled(): Thenable {
  const done: Thenable = Promise.resolve()
  fulfil(done)
  return done
}

export function contactFonts(locale: AppLocale): Thenable {
  const cached = pending.get(locale)

  if (cached !== undefined) {
    return cached
  }

  if (typeof document === 'undefined' || !('fonts' in document)) {
    const done = settled()
    pending.set(locale, done)
    return done
  }

  let timer: ReturnType<typeof setTimeout> | undefined
  const loads = Promise.all(
    WEIGHTS.map((weight) =>
      document.fonts.load(`${weight} 1em "${FAMILIES[locale]}"`, SAMPLES[locale]),
    ),
  ).then(
    () => undefined,
    // Font isteği başarısız: bekleme hemen biter, bölümler yedek fontla basılır.
    () => undefined,
  )
  const timeout = new Promise<void>((resolve) => {
    timer = setTimeout(resolve, FONT_WAIT_MS)
  })
  const gate: Thenable = Promise.race([loads, timeout]).then(() => {
    clearTimeout(timer)
    fulfil(gate)
  })
  pending.set(locale, gate)
  return gate
}
