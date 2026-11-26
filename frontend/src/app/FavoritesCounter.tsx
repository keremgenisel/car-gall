import { useLocale } from './localeContext'
import {
  FAVORITES_LIMIT,
  favoritesAvailable,
  useFavoriteOutcome,
  useFavorites,
} from '../shared/lib/favorites'
import { favoritesPath } from '../shared/lib/route'
import { HeartIcon } from '../shared/ui'

/**
 * `frontend-003` **D8** · **P3** · **P5** — üst çubuktaki sayaç ve favori eylemlerinin TEK canlı
 * bölgesi. Sayaç favoriler ekranına götürür, sıfırda da görünür ve ilk boyamada depodan okunmuş
 * sayıyı gösterir (AC-29). Görünür hâli simge + sayı; erişilebilir adı ICU cümlesi (AC-30).
 * Depo kullanılamazsa ikisi de yoktur (D9).
 */
export function FavoritesCounter() {
  const { locale, t } = useLocale()
  const count = useFavorites().length
  const outcome = useFavoriteOutcome()

  if (!favoritesAvailable()) {
    return null
  }

  return (
    <>
      <a
        className="top-bar__favorites"
        href={favoritesPath(locale)}
        aria-label={t('favorites.counter', { count })}
      >
        <HeartIcon />
        {count}
      </a>
      <p className="visually-hidden" role="status">
        {outcome === null ? '' : t('favorites.outcome', { state: outcome, count: FAVORITES_LIMIT })}
      </p>
    </>
  )
}
