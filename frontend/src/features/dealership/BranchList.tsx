import { lazy, Suspense, type CSSProperties } from 'react'
import { useLocale } from '../../app/localeContext'
import type { PublicBranch } from '../../shared/api/types'
import { Skeleton } from '../../shared/ui'
import type { GalleryLoad } from './useGallery'

// `0027` D15 — saat bloğu tembel parçada; iki bileşen aynı modülden, tek istekle gelir.
const clock = () => import('./branchClock')
const NowLine = lazy(() => clock().then((module) => ({ default: module.NowLine })))
const BranchHours = lazy(() => clock().then((module) => ({ default: module.BranchHours })))

/**
 * `0027` D15 evre B — saat alanının yüksekliği veriden: her aralık ve kapalı gün bir satır;
 * özel gün varsa başlık satırı ve kayıtların satırları (`styles.css`, `--hours-*`).
 */
function hoursLines(branch: PublicBranch): CSSProperties {
  const lines = (intervals: readonly unknown[]) => Math.max(1, intervals.length)
  const weekly = Object.values(branch.openingHours ?? {}).reduce((sum, day) => sum + lines(day), 0)
  const special = branch.specialHours.reduce((sum, day) => sum + lines(day.intervals), 0)
  return {
    '--hours-lines': weekly,
    '--special-on': branch.specialHours.length === 0 ? 0 : 1,
    '--special-lines': special,
  } as CSSProperties
}

/**
 * 0014 R7 · D8, D13 — altlığın şube bloğu: aktif şubelerin adı, adresi ve telefonu.
 * Süzme ve sıra sunucudadır (AC-48, AC-66). Aktif şube yoksa blok HİÇ basılmaz — boş
 * durum değil, yokluk (AC-65). Galeri okuması düşerse yer boş kalır ve hata metni
 * basılmaz (AC-54): altlık bir hata ekranının yeri değildir.
 */
export function BranchList({ load }: { readonly load: GalleryLoad }) {
  const { t } = useLocale()

  if (load.kind === 'loading') {
    return (
      <div
        className="home-footer__branches"
        role="status"
        aria-live="polite"
        data-testid="branch-skeleton"
      >
        <span className="visually-hidden">{t('home.footer.loading')}</span>
        <Skeleton shape="line" />
        <Skeleton shape="line" />
      </div>
    )
  }

  if (load.kind === 'error' || load.gallery.branches.length === 0) {
    return null
  }

  const { gallery, receivedAt } = load

  return (
    <section
      className="home-footer__branches"
      data-testid="branch-list"
      aria-labelledby="branches-title"
    >
      <h2 className="home-footer__heading" id="branches-title">
        {t('home.footer.contact')}
      </h2>
      <ul className="home-footer__branch-items">
        {gallery.branches.map((branch) => (
          <li
            key={`${branch.name}|${branch.phone}`}
            className="home-footer__branch"
            data-testid="branch"
          >
            <p className="home-footer__branch-name" dir="auto">
              {branch.name}
            </p>
            {/* `0027` D11 — iki satırlık sabit kutu; metin parçayla gelir, kutu hep yerinde. */}
            {branch.openingHours === null ? null : (
              <p className="home-footer__now" data-testid="branch-now">
                <Suspense fallback={null}>
                  <NowLine gallery={gallery} branch={branch} receivedAt={receivedAt} />
                </Suspense>
              </p>
            )}
            <p className="home-footer__branch-address" dir="auto">
              {branch.address}
            </p>
            {/* AC-45 — E.164 numara `tel:` hedefidir. Numara her dilde soldan sağa okunur. */}
            <a className="home-footer__phone" href={`tel:${branch.phone}`} dir="ltr">
              {branch.phone}
            </a>
            {branch.openingHours === null ? null : (
              <Suspense
                fallback={
                  <div
                    className="home-footer__hours-skeleton"
                    data-testid="branch-hours-skeleton"
                    aria-hidden="true"
                    style={hoursLines(branch)}
                  />
                }
              >
                <BranchHours gallery={gallery} branch={branch} receivedAt={receivedAt} />
              </Suspense>
            )}
          </li>
        ))}
      </ul>
    </section>
  )
}
