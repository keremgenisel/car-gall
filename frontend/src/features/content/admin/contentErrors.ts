import { ApiError } from '../../../shared/api/client'

/**
 * `0017` C5/AC-119/AC-120 — sunucunun alan bazındaki hatalarını okur: `fields` virgüllü
 * alan listesi (`tr.title`), her alan kendi kodunu taşır. Kodlar metne uçta çevrilir.
 */
export function fieldErrorsOf(cause: unknown): Readonly<Record<string, string>> {
  if (!(cause instanceof ApiError) || cause.errorCode !== 'content.validation_failed') {
    return {}
  }

  const fields = (cause.parameters['fields'] ?? '').split(',').filter((field) => field.length > 0)

  return Object.fromEntries(fields.map((field) => [field, cause.parameters[field] ?? '']))
}

/** R10 — sürüm çakışması mı (`409`). */
export function isConflict(cause: unknown): boolean {
  return (
    cause instanceof ApiError && (cause.errorCode === 'concurrency.lost' || cause.status === 409)
  )
}

/** Hata kodunun katalog anahtarı: `content.title_too_long` → `content.fieldError.title_too_long`. */
export function fieldErrorKey(code: string): string {
  return `content.fieldError.${code.replace(/^content\./, '')}`
}
