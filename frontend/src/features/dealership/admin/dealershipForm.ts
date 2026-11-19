import { ApiError } from '../../../shared/api/client'
import { catalogKeys } from '../../../shared/i18n/i18n'
import type { AppLocale } from '../../../shared/lib/locale'

/**
 * `0020` — galeri ve şube ekranlarının SAF mantığı: sunucunun alan hatalarını okuma, hata
 * kodunu çeviri anahtarına çevirme, saat dilimi listesi. Yan etkisizdir ve kendi birim testi
 * vardır (docs/frontend.md § 8.1).
 */

/**
 * C5 · R15 — sunucunun alan bazındaki hatalarını okur: `fields` virgüllü alan listesi
 * (`name.tr`, `slug`), her alan kendi kodunu taşır. Kod metne uçta çevrilir.
 */
export function fieldErrorsOf(cause: unknown): Readonly<Record<string, string>> {
  if (!(cause instanceof ApiError) || cause.errorCode !== 'dealership.validation_failed') {
    return {}
  }

  const fields = (cause.parameters['fields'] ?? '').split(',').filter((field) => field.length > 0)

  return Object.fromEntries(fields.map((field) => [field, cause.parameters[field] ?? '']))
}

/** R16 — sürüm çakışması mı (`409` + `concurrency.lost`). */
export function isConflict(cause: unknown): boolean {
  return cause instanceof ApiError && cause.errorCode === 'concurrency.lost'
}

/**
 * Alan hata kodunun katalog anahtarı: `branch.slug_taken` → `dealership.fieldError.branch_slug_taken`.
 * Katalogda karşılığı yoksa genel alan hatasına düşülür; kodun kendisi ASLA gösterilmez
 * (docs/frontend.md § 3.1).
 */
export function fieldErrorKey(code: string, locale: AppLocale): string {
  const candidate = `dealership.fieldError.${code.replace(/\./g, '_')}`
  return catalogKeys(locale).includes(candidate) ? candidate : 'dealership.fieldError.generic'
}

/**
 * Eylem ve kaydetme reddinin katalog anahtarı (`branch.last_active_branch` →
 * `dealership.error.branch_last_active_branch`). Bilinmeyen kod genel mesaja düşer.
 */
export function actionErrorKey(cause: unknown, locale: AppLocale): string {
  if (!(cause instanceof ApiError)) {
    return 'dealership.error.generic'
  }

  const candidate = `dealership.error.${cause.errorCode.replace(/\./g, '_')}`
  return catalogKeys(locale).includes(candidate) ? candidate : 'dealership.error.generic'
}

/** Durum KODU → çeviri anahtarı: `TemporarilyClosed` → `branch.status.temporarily_closed`. */
export function statusKey(code: string): string {
  return `branch.status.${code.replace(/([a-z0-9])([A-Z])/g, '$1_$2').toLowerCase()}`
}

/**
 * `0020` **D11** — saat dilimi serbest metin değil, tarayıcının bildiği IANA bölge
 * kimliklerinin listesidir (AC-28). Kayıtlı değer listede yoksa (tarayıcı eski ya da
 * kimlik bir eş ad) yine seçenek olarak kalır: aksi hâlde form kayıtlı değeri sessizce
 * kaybederdi. Sunucu tzdata ile yine doğrular (AC-27).
 */
export function timeZoneOptions(current: string, intl: typeof Intl = Intl): readonly string[] {
  const known =
    typeof intl.supportedValuesOf === 'function' ? intl.supportedValuesOf('timeZone') : []
  const zones = new Set(known.filter((zone) => zone.includes('/')))

  if (current.length > 0) {
    zones.add(current)
  }

  return [...zones].sort()
}

/** Boşluktan ibaret değerleri düşürür: sunucu boş dili saklamaz (AC-6). */
export function localizedPayload(
  values: Readonly<Record<string, string>>,
): Readonly<Record<string, string>> {
  return Object.fromEntries(Object.entries(values).filter(([, value]) => value.trim().length > 0))
}

/**
 * Koordinat alanı: boş → `null` (D9: temizlenir), sayı → sayı, başka bir şey →
 * `undefined` (form alan hatası gösterir ve isteği göndermez).
 */
export function parseCoordinate(value: string): number | null | undefined {
  const trimmed = value.trim()

  if (trimmed.length === 0) {
    return null
  }

  const parsed = Number(trimmed.replace(',', '.'))
  return Number.isFinite(parsed) ? parsed : undefined
}
