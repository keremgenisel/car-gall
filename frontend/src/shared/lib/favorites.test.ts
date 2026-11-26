import { act, renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  FAVORITES_KEY,
  FAVORITES_LIMIT,
  favoritesAvailable,
  resetFavoritesForTests,
  setFavorite,
  useFavorites,
  type Favorite,
} from './favorites'

/**
 * `frontend-003` **D4** · **D5** · **D9** — depo katmanı, jsdom'un gerçek `localStorage`'ıyla. "Öbür
 * sekme" depoya doğrudan yazılarak kurulur: bu sekmenin bellekteki görüntüsü onu bilmez.
 */
const ilan = (slug: string, modelName = 'Toyota Corolla', modelYear = 2020) => ({
  slug,
  modelName,
  modelYear,
})

function kayit(): { v: number; items: Favorite[] } {
  return JSON.parse(localStorage.getItem(FAVORITES_KEY) ?? 'null') as {
    v: number
    items: Favorite[]
  }
}

function digerSekmeYazar(items: readonly Omit<Favorite, 'addedAt'>[]) {
  localStorage.setItem(
    FAVORITES_KEY,
    JSON.stringify({
      v: 1,
      items: items.map((item) => ({ ...item, addedAt: '2026-11-26T10:00:00.000Z' })),
    }),
  )
}

beforeEach(() => {
  localStorage.clear()
  resetFavoritesForTests()
})

afterEach(() => {
  vi.restoreAllMocks()
})

describe('favoriler deposu', () => {
  it('frontend-003/AC-12: İki sekme — B bayat görüntüyle Y ekler, X ve Y kalır; B görünen "ekle"yle X basınca X tek kalır; A bayat görüntüyle X çıkarınca yalnız Y kalır', () => {
    const b = renderHook(() => useFavorites())
    expect(b.result.current).toHaveLength(0)

    // A'da X eklendi (B yenilenmedi).
    digerSekmeYazar([ilan('x-2020-aaaaaa')])

    act(() => {
      setFavorite(true, ilan('y-2021-bbbbbb'))
    })
    expect(kayit().items.map((item) => item.slug)).toEqual(['x-2020-aaaaaa', 'y-2021-bbbbbb'])
    expect(b.result.current).toHaveLength(2)

    // B'de X basılı görünmüyordu; basılır → no-op birleşme, X tek.
    act(() => {
      setFavorite(true, ilan('x-2020-aaaaaa'))
    })
    expect(kayit().items.filter((item) => item.slug === 'x-2020-aaaaaa')).toHaveLength(1)
    expect(b.result.current.map((item) => item.slug)).toContain('x-2020-aaaaaa')
    expect(b.result.current).toHaveLength(2)

    // A yenilenmeden X'i çıkarır.
    act(() => {
      setFavorite(false, ilan('x-2020-aaaaaa'))
    })
    expect(kayit().items.map((item) => item.slug)).toEqual(['y-2021-bbbbbb'])
  })

  it('frontend-003/AC-14: Bozuk JSON, tanınmayan sürüm ve yanlış biçimli öğe sayfayı düşürmez; geçerli öğeler korunur', () => {
    localStorage.setItem(FAVORITES_KEY, '{bozuk')
    expect(renderHook(() => useFavorites()).result.current).toEqual([])

    resetFavoritesForTests()
    localStorage.setItem(FAVORITES_KEY, JSON.stringify({ v: 2, items: [] }))
    expect(renderHook(() => useFavorites()).result.current).toEqual([])

    resetFavoritesForTests()
    localStorage.setItem(
      FAVORITES_KEY,
      JSON.stringify({
        v: 1,
        items: [
          { slug: 'gecerli-2020-aaaaaa', addedAt: 'x', modelName: 'A', modelYear: 2020 },
          { slug: 'Buyuk-Harf', addedAt: 'x', modelName: 'B', modelYear: 2020 },
          { slug: 'yil-yok', addedAt: 'x', modelName: 'C' },
          null,
          'dizge',
          { slug: 'gecerli-2020-aaaaaa', addedAt: 'y', modelName: 'A', modelYear: 2020 },
        ],
      }),
    )
    expect(renderHook(() => useFavorites()).result.current.map((item) => item.slug)).toEqual([
      'gecerli-2020-aaaaaa',
    ])
  })

  it.each([
    [
      'erişici atar',
      () =>
        vi.spyOn(window, 'localStorage', 'get').mockImplementation(() => {
          throw new DOMException('kapalı', 'SecurityError')
        }),
    ],
    [
      'okuma çalışır, yazma kota hatası atar',
      () =>
        vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
          throw new DOMException('dolu', 'QuotaExceededError')
        }),
    ],
    [
      'ikisi de atar',
      () => {
        vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
          throw new DOMException('kapalı', 'SecurityError')
        })
        vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
          throw new DOMException('kapalı', 'SecurityError')
        })
      },
    ],
  ])(
    'frontend-003/AC-15 (birim): depo kullanılamazlığı — %s → favori arayüzü yok, okuma boş, sayfa düşmez',
    (_ad, kur) => {
      kur()
      expect(favoritesAvailable()).toBe(false)
      expect(renderHook(() => useFavorites()).result.current).toEqual([])
    },
  )

  it('frontend-003/AC-17: Sayfa açıkken yazma kota hatası verirse durum değişmez ve sonuç "yazılamadı"dır', () => {
    const view = renderHook(() => useFavorites())
    expect(favoritesAvailable()).toBe(true)
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('dolu', 'QuotaExceededError')
    })

    let sonuc = ''
    act(() => {
      sonuc = setFavorite(true, ilan('x-2020-aaaaaa'))
    })

    expect(sonuc).toBe('failed')
    expect(view.result.current).toEqual([])
    expect(localStorage.getItem(FAVORITES_KEY)).toBeNull()
  })

  it('frontend-003/AC-18: 24 favoriyle 25. ekleme yapılmaz; sonuç "sınır", kayıt 24 kalır ve en eski silinmez', () => {
    digerSekmeYazar(
      Array.from({ length: FAVORITES_LIMIT }, (_, index) =>
        ilan(`a-2020-${String(index).padStart(6, '0')}`),
      ),
    )

    expect(setFavorite(true, ilan('yeni-2024-ffffff'))).toBe('limit')
    expect(kayit().items).toHaveLength(24)
    expect(kayit().items[0]!.slug).toBe('a-2020-000000')
    expect(kayit().items.some((item) => item.slug === 'yeni-2024-ffffff')).toBe(false)
  })

  it("frontend-003/AC-19: Depo anahtarı `/token|session|auth/i`'ye uymaz", () => {
    expect(FAVORITES_KEY).not.toMatch(/token|session|auth/i)
  })

  it('frontend-003/AC-20: Eklenen kayıt yalnız slug, eklenme anı, model adı ve yılı taşır; fiyat ve rezerve hâli yok', () => {
    vi.useFakeTimers({ now: new Date('2026-11-26T12:34:56.000Z') })
    setFavorite(true, {
      ...ilan('x-2020-aaaaaa', 'Honda Civic', 2021),
      price: 1,
      isReserved: true,
    } as never)
    vi.useRealTimers()

    expect(kayit()).toEqual({
      v: 1,
      items: [
        {
          slug: 'x-2020-aaaaaa',
          addedAt: '2026-11-26T12:34:56.000Z',
          modelName: 'Honda Civic',
          modelYear: 2021,
        },
      ],
    })
  })
  it("frontend-003 QA 2 BULGU-4: Depoya elle 30 geçerli öğe yazılsa da okuma ilk 24'tür", () => {
    digerSekmeYazar(
      Array.from({ length: 30 }, (_, index) => ilan(`a-2020-${String(index).padStart(6, '0')}`)),
    )

    const okunan = renderHook(() => useFavorites()).result.current
    expect(okunan).toHaveLength(24)
    expect(okunan[0]!.slug).toBe('a-2020-000000')
  })
})
