import type { AppLocale } from '../lib/locale'
import { getJson, sendJson } from './client'
import type { OpeningInterval, BranchSummary } from './types'

const ROOT = '/api/admin'

/**
 * `0020` R1 — panelin galeri profili. Görünen ad dil → metin haritasıdır; eksik dil haritada
 * YOKTUR ve panelde boş alan olarak görünür (AC-1, AC-7). `version` iyimser eşzamanlılık
 * içindir (R16).
 */
export interface AdminGallery {
  readonly commercialName: string
  readonly taxNumber: string | null
  readonly tradeRegistryNumber: string | null
  readonly displayNames: Readonly<Record<string, string>>
  /** `0024` AC-32 — platform kodundan adrese; hiç hesap yoksa boş. */
  readonly socialLinks: Readonly<Record<string, string>>
  readonly version: number
  /** `0026` D9 — atanmış logonun medya kimliği; yoksa `null`. */
  readonly logoMediaAssetId: string | null
}

/** `0020` R1 — galeri profili kaydetme girdisi. */
export interface GalleryProfileInput {
  readonly commercialName: string
  readonly taxNumber: string
  readonly tradeRegistryNumber: string
  readonly displayNames: Readonly<Record<string, string>>
  /**
   * `0024` D4 · P8 — panel kümeyi HER ZAMAN bütünüyle gönderir: altı kodun hepsi, boş değer
   * "bu platformda hesap yok" (sunucu siler). Alanın yokluğu (`0020` istemcisi) değişmez demektir.
   */
  readonly socialLinks: Readonly<Record<string, string>>
}

/** `0020` R10 — panelin şube düzenleme formu; durum bir KOD'dur (`Active`, `TemporarilyClosed`, `Closed`). */
export interface AdminBranch {
  readonly branchId: string
  readonly names: Readonly<Record<string, string>>
  readonly addresses: Readonly<Record<string, string>>
  readonly phone: string
  readonly whatsApp: string | null
  readonly email: string | null
  readonly timeZone: string
  readonly slug: string
  readonly latitude: number | null
  readonly longitude: number | null
  readonly status: string
  readonly version: number
  /** `0025` D14 — D8'in biçimi; programsız şubede `null`. */
  readonly openingHours: Readonly<Record<string, readonly OpeningInterval[]>> | null
  /** `0027` D13 — güncel ve gelecek bütün kayıtlar, `from` sırasıyla; geçmiş hiç dönmez. */
  readonly specialHours?: readonly AdminSpecialDay[]
}

/** `0027` D2 — bir özel gün; aralıksız kayıt tam gün kapalıdır. */
export interface AdminSpecialDay {
  readonly from: string
  readonly through: string
  readonly intervals: readonly OpeningInterval[]
}

/** `0020` R9 — şube açma ve kaydetme girdisi; opsiyonel alan boş metinle boşaltılır (D9). */
export interface BranchInput {
  readonly names: Readonly<Record<string, string>>
  readonly addresses: Readonly<Record<string, string>>
  readonly phone: string
  readonly whatsApp: string
  readonly email: string
  readonly timeZone: string
  readonly slug: string
  readonly latitude: number | null
  readonly longitude: number | null
  /** `0025` D11 · P8 — panel yedi günün hepsini gönderir; yedi boş gün programı siler. */
  readonly openingHours: Readonly<Record<string, readonly OpeningInterval[]>>
  /** `0027` D13 — dizi bütün kayıtların yerine geçer; boş "Bitiş" `null` (başlangıçla aynı gün). */
  readonly specialHours?: readonly {
    from: string
    through: string | null
    intervals: OpeningInterval[]
  }[]
}

/** `0020` R11, R12 — durum eylemleri. */
export type BranchAction = 'close-temporarily' | 'reopen' | 'close'

export function fetchGalleryProfile(
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<AdminGallery> {
  return getJson<AdminGallery>(`${ROOT}/gallery`, locale, signal)
}

/** Kaydeder; yeni sürümü döner. Eski sürümle `409` (R16). */
export async function saveGalleryProfile(
  version: number,
  input: GalleryProfileInput,
  locale: AppLocale,
): Promise<number> {
  const result = await sendJson<{ readonly version: number }>(
    'PUT',
    `${ROOT}/gallery`,
    { version, ...input },
    locale,
  )
  return result?.version ?? version
}

/**
 * `0020` R8 — şubeler ekranının listesi. Araç formunun okuduğu listenin AYNISI (P5,
 * AC-68): bütün durumlar, panel dilindeki ad, durum kodu.
 */
export function fetchBranchList(
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<readonly BranchSummary[]> {
  return getJson<readonly BranchSummary[]>(`${ROOT}/branches?locale=${locale}`, locale, signal)
}

export function fetchAdminBranch(
  branchId: string,
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<AdminBranch> {
  return getJson<AdminBranch>(`${ROOT}/branches/${encodeURIComponent(branchId)}`, locale, signal)
}

/** Yeni şube açar; kimliğini döner (R9). */
export async function openBranch(input: BranchInput, locale: AppLocale): Promise<string> {
  const created = await sendJson<{ readonly id: string }>('POST', `${ROOT}/branches`, input, locale)
  return created?.id ?? ''
}

/** Kaydeder; yeni sürümü döner. Eski sürümle `409` (R16). */
export async function saveBranch(
  branchId: string,
  version: number,
  input: BranchInput,
  locale: AppLocale,
): Promise<number> {
  const result = await sendJson<{ readonly version: number }>(
    'PUT',
    `${ROOT}/branches/${encodeURIComponent(branchId)}`,
    { version, ...input },
    locale,
  )
  return result?.version ?? version
}

/** Durum eylemi; yeni sürümü döner (P4). */
export async function runBranchAction(
  branchId: string,
  version: number,
  action: BranchAction,
  locale: AppLocale,
): Promise<number> {
  const result = await sendJson<{ readonly version: number }>(
    'POST',
    `${ROOT}/branches/${encodeURIComponent(branchId)}/${action}`,
    { version },
    locale,
  )
  return result?.version ?? version
}

/** `0026` D9 — logo ataması; okunan sürümle, yeni sürümü döner. Eski sürümle `409`. */
export async function assignGalleryLogo(
  mediaAssetId: string,
  version: number,
  locale: AppLocale,
): Promise<number> {
  const result = await sendJson<{ readonly version: number }>(
    'PUT',
    `${ROOT}/gallery/logo`,
    { mediaAssetId, version },
    locale,
  )
  return result?.version ?? version
}

/** `0026` D9 — logoyu kaldırır (bağı siler, varlık durur); yeni sürümü döner. */
export async function removeGalleryLogo(version: number, locale: AppLocale): Promise<number> {
  const result = await sendJson<{ readonly version: number }>(
    'DELETE',
    `${ROOT}/gallery/logo?version=${version}`,
    undefined,
    locale,
  )
  return result?.version ?? version
}
