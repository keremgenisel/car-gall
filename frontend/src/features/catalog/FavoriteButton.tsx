import { useLocale } from '../../app/localeContext'
import { favoritesAvailable, setFavorite, useFavorites } from '../../shared/lib/favorites'
import { HeartIcon } from '../../shared/ui'

interface Props {
  readonly listing: {
    readonly slug: string
    readonly modelName: string
    readonly modelYear: number
  }
  /** Detayda görünür metinli düğme ("Favorilere ekle" / "Favorilerde"); kartta yalnız simge. */
  readonly labelled?: boolean
}

/**
 * `frontend-003` **D7** — favori düğmesi: kart, detay, karşılaştırma sütunu. Durum `aria-pressed`
 * ile bildirilir; kart düğmesinin adı aracın model adını ve yılını taşır (aynı sayfadaki kartlar
 * ayrışır, tuzak 9). Basınca ekranda görünen hâlin eylemi depoya yazılır (D4); ağ isteği yoktur.
 * Depo kullanılamazsa düğme hiç basılmaz (D9).
 */
export function FavoriteButton({ listing, labelled = false }: Props) {
  const { t } = useLocale()
  const on = useFavorites().some((item) => item.slug === listing.slug)

  if (!favoritesAvailable()) {
    return null
  }

  return (
    <button
      type="button"
      className="favorite-button"
      aria-pressed={on}
      aria-label={
        labelled
          ? undefined
          : // Yıl hazır dizge: ICU sayı biçimi (`::group-off`) üç dilde tekrar etmesin (bütçe).
            t('favorites.card', { model: listing.modelName, year: String(listing.modelYear) })
      }
      onClick={() => setFavorite(!on, listing)}
    >
      <HeartIcon />
      {/* Görünür metin canlı bölgenin `select` anahtarından (düşürme adımı 5, D18). */}
      {labelled ? t('favorites.outcome', { state: on ? 'on' : 'off' }) : null}
    </button>
  )
}
