import { useId } from 'react'
import { useLocale } from '../../../app/localeContext'
import type { ShowcasePaintChange } from '../../../shared/api/types'
import { PAINT_CHANGE_PARTS, PAINT_CHANGE_STATES } from '../../../shared/lib/paintChange'
import { PaintChangeSchema } from './PaintChangeSchema'

/**
 * `0016b` R13–R19 — ilan detayının parça haritası bölümü: şema, lejant, okunabilir liste
 * ve iki not. Yalnız harita VARKEN basılır: alan kapalı, gizli ya da değersizken yanıtta
 * anahtar yoktur (D9) ve çağıran bölümü hiç kurmaz (R14, AC-32). Değerin olmaması ile
 * tamamının orijinal olması aynı gösterilmez (D8).
 */
export function PaintChangeMapSection({ map }: { readonly map: readonly ShowcasePaintChange[] }) {
  const { t } = useLocale()
  const headingId = useId()
  const listId = useId()
  const prefix = usePatternPrefix()

  const states = new Map(map.map((entry) => [entry.part, entry.state]))
  // C10 sırası, girilen sıra değil (AC-34).
  const changed = PAINT_CHANGE_PARTS.filter(
    (part) => (states.get(part) ?? 'original') !== 'original',
  )
  const originalCount = PAINT_CHANGE_PARTS.length - changed.length

  return (
    <section className="paint-map" aria-labelledby={headingId} data-testid="paint-map">
      <h2 className="paint-map__title" id={headingId}>
        {t('detail.paintMap.title')}
      </h2>

      <div className="paint-map__layout">
        <PaintChangeSchema
          map={map}
          label={t('detail.paintMap.schemaLabel')}
          describedBy={listId}
          patternPrefix={prefix}
        />

        <div className="paint-map__text">
          <ul
            className="paint-map__legend"
            aria-label={t('detail.paintMap.legend')}
            data-testid="paint-map-legend"
          >
            {PAINT_CHANGE_STATES.map((state) => (
              <li key={state} className="paint-map__legend-item" data-state={state}>
                <svg
                  className="paint-map__swatch"
                  viewBox="0 0 24 16"
                  aria-hidden="true"
                  focusable="false"
                >
                  {/* Lejant, şemanın desenlerinin AYNISINI kullanır; ikinci tanım yok. */}
                  <rect
                    className={`paint-map__part paint-map__part--${state}`}
                    x="1"
                    y="1"
                    width="22"
                    height="14"
                    rx="2"
                    fill={`url(#${prefix}-${state})`}
                  />
                </svg>
                <span>{t(`paintChange.state.${state}`)}</span>
              </li>
            ))}
          </ul>

          <div id={listId} className="paint-map__summary">
            {changed.length === 0 ? (
              <p className="paint-map__all-original" data-testid="paint-map-all-original">
                {t('detail.paintMap.allOriginal')}
              </p>
            ) : (
              <>
                <ul className="paint-map__list" data-testid="paint-map-list">
                  {changed.map((part) => (
                    <li key={part} data-part={part}>
                      {t('detail.paintMap.entry', {
                        part: t(`paintChange.part.${part}`),
                        state: t(`detail.paintMap.state.${states.get(part) ?? 'original'}`),
                      })}
                    </li>
                  ))}
                </ul>
                {/* D34 — hiçbir parça orijinal değilken "diğer 0 parça" cümlesi basılmaz. */}
                {originalCount > 0 ? (
                  <p className="paint-map__others" data-testid="paint-map-others">
                    {t('detail.paintMap.othersOriginal', { count: originalCount })}
                  </p>
                ) : null}
              </>
            )}
          </div>

          <ul className="paint-map__notes" data-testid="paint-map-notes">
            <li>{t('detail.paintMap.note.declaration')}</li>
            <li>{t('detail.paintMap.note.orientation')}</li>
          </ul>
        </div>
      </div>
    </section>
  )
}

/** `useId` çıktısı `url(#…)` içinde kaçış ister; desen kimliği yalnız harf ve rakam taşır. */
function usePatternPrefix(): string {
  return `paint-${useId().replace(/[^a-zA-Z0-9]/g, '')}`
}
