import { useSyncExternalStore } from 'react'

/**
 * `frontend-003` **D4** · **D5** · **D9** · **P1** · **P2** — ziyaretçinin favorileri, YALNIZ bu
 * tarayıcıda. Sunucuya hiçbir şey gitmez (S3): bu modülün ağla işi yoktur.
 *
 * - Kayıt dilden bağımsız tek anahtardadır; adı `check:session`'ın yasak sözcüklerini taşımaz.
 * - Depo açılışta bir deneme yazması + silmesiyle sınanır. Erişici atarsa, yazma atarsa ya da
 *   ikisi de atarsa depo yoktur ve favori arayüzü hiç basılmaz (D9).
 * - Her ekleme ve çıkarma o anki depoyu YENİDEN OKUR ve ekranda görünen hâlin eylemini uygular
 *   (D4 · B8): başka bir sekmenin yazdığı kayıt silinmez, aynı slug iki kez girmez.
 * - Okunamayan kayıt boş sayılır, geçersiz öğe atlanır; sayfa düşmez (AC-14).
 */
export const FAVORITES_KEY = 'cargallery.favorites'

export const FAVORITES_LIMIT = 24

export interface Favorite {
  readonly slug: string
  readonly addedAt: string
  readonly modelName: string
  readonly modelYear: number
}

/** Canlı bölgenin söylediği: eklendi · çıkarıldı · sınır · yazılamadı (tek ICU `select`, P3). */
export type FavoriteOutcome = 'added' | 'removed' | 'limit' | 'failed'

const SLUG = /^[a-z0-9-]{1,160}$/

let store: Storage | null | undefined
let items: readonly Favorite[] | undefined
let outcome: FavoriteOutcome | null = null
const listeners = new Set<() => void>()

function storage(): Storage | null {
  if (store === undefined) {
    try {
      const candidate = window.localStorage
      candidate.setItem(`${FAVORITES_KEY}.probe`, '1')
      candidate.removeItem(`${FAVORITES_KEY}.probe`)
      store = candidate
    } catch {
      store = null
    }
  }

  return store
}

function valid(item: unknown): item is Favorite {
  // Düşürme adımı 4 (D18): yalnız slug biçimi ve alan tipleri.
  const value = item as Favorite | null
  return (
    typeof value?.slug === 'string' &&
    SLUG.test(value.slug) &&
    typeof value.addedAt === 'string' &&
    typeof value.modelName === 'string' &&
    typeof value.modelYear === 'number'
  )
}

/** Depodaki kayıt; okunamıyorsa boş. */
function read(): readonly Favorite[] {
  try {
    const record = JSON.parse(storage()?.getItem(FAVORITES_KEY) ?? 'null') as {
      readonly v?: unknown
      readonly items?: unknown
    } | null

    if (record?.v !== 1 || !Array.isArray(record.items)) {
      return []
    }

    const seen = new Set<string>()
    return record.items.filter((item) => valid(item) && !seen.has(item.slug) && seen.add(item.slug))
  } catch {
    return []
  }
}

function emit(): void {
  for (const listener of listeners) {
    listener()
  }
}

function subscribe(listener: () => void): () => void {
  listeners.add(listener)
  return () => listeners.delete(listener)
}

function snapshot(): readonly Favorite[] {
  items ??= read()
  return items
}

/** Depo kullanılabilir mi (D9). `false` ise düğme ve sayaç basılmaz. */
export function favoritesAvailable(): boolean {
  return storage() !== null
}

/** Favoriler, eklenme sırasıyla (eski önce). İlk render depodan eşzamanlı okur: sayaç ilk karede doğru. */
export function useFavorites(): readonly Favorite[] {
  return useSyncExternalStore(subscribe, snapshot)
}

/**
 * Son eylemin sonucu; canlı bölge söyler. Aynı sonuç art arda gelirse metin değişmez ve ikinci kez
 * duyurulmaz (ör. sınırda iki kez basmak) — ilk paket bütçesi için kabul edilen bedel (D18).
 */
export function useFavoriteOutcome(): FavoriteOutcome | null {
  return useSyncExternalStore(subscribe, () => outcome)
}

/**
 * Ekranda görünen hâlin eylemi (D4 · B8). `add` görünüyorsa ekler — kayıtta zaten varsa bir şey
 * yapmaz; `remove` görünüyorsa çıkarır — yoksa bir şey yapmaz. Kayıt o anki depodan okunur.
 */
export function setFavorite(add: boolean, listing: Omit<Favorite, 'addedAt'>): FavoriteOutcome {
  return write(add ? 'added' : 'removed', (current) => {
    if (!add) {
      return current.filter((item) => item.slug !== listing.slug)
    }

    if (current.some((item) => item.slug === listing.slug)) {
      return current
    }

    return current.length >= FAVORITES_LIMIT
      ? null
      : [
          ...current,
          {
            slug: listing.slug,
            addedAt: new Date().toISOString(),
            modelName: listing.modelName,
            modelYear: listing.modelYear,
          },
        ]
  })
}

/** Birden çok favoriyi çıkarır ("hepsini kaldır", D10). */
export function removeFavorites(slugs: readonly string[]): FavoriteOutcome {
  return write('removed', (current) => current.filter((item) => !slugs.includes(item.slug)))
}

function write(
  success: FavoriteOutcome,
  change: (current: readonly Favorite[]) => readonly Favorite[] | null,
): FavoriteOutcome {
  const current = read()
  const next = change(current)
  let kind = next === null ? 'limit' : success

  if (next !== null && next !== current) {
    try {
      storage()?.setItem(FAVORITES_KEY, JSON.stringify({ v: 1, items: next }))
    } catch {
      // Kota dolu ya da depo kapandı: durum DEĞİŞMEZ, neden söylenir (AC-17).
      kind = 'failed'
    }
  }

  items = kind === 'failed' ? current : (next ?? current)
  outcome = kind
  emit()
  return kind
}

/** Testler için: depo tespitini ve bellekteki görüntüyü sıfırlar. */
export function resetFavoritesForTests(): void {
  store = undefined
  items = undefined
  outcome = null
}
