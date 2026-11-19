import { useEffect, useState } from 'react'
import { useLocale } from '../../../app/localeContext'
import {
  fetchFeaturedListings,
  fetchHomeBrands,
  fetchShowcaseTotal,
} from '../../../shared/api/catalog'
import type { ShowcaseBrand } from '../../../shared/api/types'
import { ErrorState } from '../../../shared/ui'
import { loadBodyTypeOptions, type BodyTypeOptions } from '../bodyTypeOptions'
import { EmptyShowcase } from '../states/EmptyShowcase'
import { FeaturedSection, type FeaturedLoad } from './FeaturedSection'
import { FormSkeleton } from './FormSkeleton'
import { NarrowingForm } from './NarrowingForm'

type FormLoad =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | {
      readonly kind: 'ready'
      readonly total: number
      readonly options: BodyTypeOptions
      /** `0021` R13 — formun markaları; okunamazsa boş (D22: alan görünmez). */
      readonly brands: readonly ShowcaseBrand[]
    }

interface Keyed<T> {
  readonly key: string
  readonly value: T
}

/**
 * 0014 — ana sayfanın `VehicleCatalog` bölümleri: daraltma formu ve öne çıkan araçlar.
 *
 * İki BAĞIMSIZ okuma var ve her biri kendi tekrar dene eylemini taşır (AC-55, AC-64):
 * formun okuması toplam sayı + kasa tipleri, öne çıkanınki öne çıkan ilanlar. Biri
 * düşerse diğeri görünmeye devam eder (R8). Boş vitrin kararı TOPLAMDAN verilir (D12):
 * toplam `0` ise iki bölümün yerine tek bir boş durum basılır (AC-50); toplam okunamadıysa
 * boş durum gösterilmez, çünkü bilinmeyen bir sayı sıfır değildir (AC-52).
 *
 * Yükleniyor hâli efektte setState ile kurulmaz, istek anahtarından türetilir (liste
 * ekranının deseni): senkron setState iskeleti bir kare geç gösterir.
 */
export function HomeCatalog() {
  const { locale } = useLocale()
  const [formToken, setFormToken] = useState(0)
  const [featuredToken, setFeaturedToken] = useState(0)
  const [formResult, setFormResult] = useState<Keyed<FormLoad> | null>(null)
  const [featuredResult, setFeaturedResult] = useState<Keyed<FeaturedLoad> | null>(null)

  const formKey = `${locale}|${formToken}`
  const featuredKey = `${locale}|${featuredToken}`
  const form: FormLoad = formResult?.key === formKey ? formResult.value : { kind: 'loading' }
  const featured: FeaturedLoad =
    featuredResult?.key === featuredKey ? featuredResult.value : { kind: 'loading' }

  useEffect(() => {
    const controller = new AbortController()

    // `0021` D22 — marka okuması PARALEL ve formu düşürmez: düşerse marka alanı görünmez ve
    // form kasa tipiyle çalışır (AC-51). İlk boyamanın önüne ek bir gidiş-dönüş koymaz.
    Promise.all([
      fetchShowcaseTotal(locale, controller.signal),
      loadBodyTypeOptions(locale, controller.signal),
      fetchHomeBrands(locale, controller.signal).catch((): readonly ShowcaseBrand[] => []),
    ])
      .then(([total, options, brands]) =>
        setFormResult({ key: formKey, value: { kind: 'ready', total, options, brands } }),
      )
      .catch((cause: unknown) => {
        if (!isAbort(cause)) {
          setFormResult({ key: formKey, value: { kind: 'error' } })
        }
      })

    return () => controller.abort()
  }, [locale, formKey])

  useEffect(() => {
    const controller = new AbortController()

    fetchFeaturedListings(locale, controller.signal)
      .then((items) => setFeaturedResult({ key: featuredKey, value: { kind: 'ready', items } }))
      .catch((cause: unknown) => {
        if (!isAbort(cause)) {
          setFeaturedResult({ key: featuredKey, value: { kind: 'error' } })
        }
      })

    return () => controller.abort()
  }, [locale, featuredKey])

  if (form.kind === 'ready' && form.total === 0) {
    // AC-50 — tek boş durum; "0 aracı gör" hiçbir yerde yazmaz (Bilinen tuzak 3).
    return (
      <div className="home-section">
        <EmptyShowcase />
      </div>
    )
  }

  return (
    <>
      <section className="home-section">
        <FormBlock load={form} onRetry={() => setFormToken((token) => token + 1)} />
      </section>
      <FeaturedSection load={featured} onRetry={() => setFeaturedToken((token) => token + 1)} />
    </>
  )
}

function FormBlock({ load, onRetry }: { readonly load: FormLoad; readonly onRetry: () => void }) {
  const { t } = useLocale()

  if (load.kind === 'loading') {
    return <FormSkeleton />
  }

  if (load.kind === 'error') {
    return (
      <ErrorState
        testId="narrowing-form-error"
        title={t('home.form.error.title')}
        description={t('home.form.error.body')}
        retryLabel={t('home.form.error.retry')}
        onRetry={onRetry}
      />
    )
  }

  return <NarrowingForm total={load.total} options={load.options} brands={load.brands} />
}

function isAbort(cause: unknown): boolean {
  return cause instanceof DOMException && cause.name === 'AbortError'
}
