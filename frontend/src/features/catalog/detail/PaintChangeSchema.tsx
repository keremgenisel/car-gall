import type { ShowcasePaintChange } from '../../../shared/api/types'
import { PAINT_CHANGE_STATES, type PaintChangePart } from '../../../shared/lib/paintChange'

/** Parçanın çizimdeki dikdörtgeni (viewBox birimiyle). */
interface Shape {
  readonly x: number
  readonly y: number
  readonly width: number
  readonly height: number
  readonly radius: number
}

/** Çizimin genişliği; sağ taraf, sol tarafın bu eksende yansımasıdır. */
const WIDTH = 220
const HEIGHT = 410

function mirror(shape: Shape): Shape {
  return { ...shape, x: WIDTH - shape.x - shape.width }
}

const LEFT_FRONT_FENDER: Shape = { x: 22, y: 40, width: 30, height: 88, radius: 8 }
const LEFT_FRONT_DOOR: Shape = { x: 22, y: 132, width: 36, height: 88, radius: 4 }
const LEFT_REAR_DOOR: Shape = { x: 22, y: 224, width: 36, height: 84, radius: 4 }
const LEFT_REAR_FENDER: Shape = { x: 22, y: 312, width: 32, height: 60, radius: 8 }

/**
 * `0016b` D29 — 13 parçalı sade üstten görünüm. **Ön üsttedir** (R18'in notu) ve aracın
 * SOL tarafı çizimin SOLUNDADIR. Koordinatlar fiziksel bir görünümün koordinatlarıdır:
 * RTL'de aynalanmaz (R21, D10) ve C4'ün mantıksal yön kuralı bu tabloya uygulanmaz.
 */
const SHAPES: Readonly<Record<PaintChangePart, Shape>> = {
  front_bumper: { x: 45, y: 12, width: 130, height: 22, radius: 10 },
  hood: { x: 56, y: 38, width: 108, height: 82, radius: 8 },
  roof: { x: 62, y: 164, width: 96, height: 112, radius: 6 },
  trunk_lid: { x: 58, y: 312, width: 104, height: 58, radius: 6 },
  rear_bumper: { x: 45, y: 376, width: 130, height: 22, radius: 10 },
  left_front_fender: LEFT_FRONT_FENDER,
  left_front_door: LEFT_FRONT_DOOR,
  left_rear_door: LEFT_REAR_DOOR,
  left_rear_fender: LEFT_REAR_FENDER,
  right_front_fender: mirror(LEFT_FRONT_FENDER),
  right_front_door: mirror(LEFT_FRONT_DOOR),
  right_rear_door: mirror(LEFT_REAR_DOOR),
  right_rear_fender: mirror(LEFT_REAR_FENDER),
}

/** Parça olmayan çizim öğeleri: camlar, tekerlekler, aynalar. Durum taşımazlar. */
const GLASS: readonly Shape[] = [
  { x: 60, y: 124, width: 100, height: 36, radius: 6 },
  { x: 62, y: 280, width: 96, height: 28, radius: 6 },
]
const WHEELS: readonly Shape[] = [
  { x: 8, y: 62, width: 11, height: 42, radius: 4 },
  { x: 8, y: 318, width: 11, height: 42, radius: 4 },
  mirror({ x: 8, y: 62, width: 11, height: 42, radius: 4 }),
  mirror({ x: 8, y: 318, width: 11, height: 42, radius: 4 }),
]
const MIRRORS: readonly Shape[] = [
  { x: 8, y: 132, width: 14, height: 9, radius: 3 },
  mirror({ x: 8, y: 132, width: 14, height: 9, radius: 3 }),
]

/**
 * `0016b` C2/D13 — dört durum dört DESENLE ayrılır: boş, seyrek tarama, yoğun tarama,
 * çapraz tarama. Renk ek bir ipucudur; gri tonlamada da desen okunur (AC-26).
 * `gap` tarama çizgileri arasındaki uzaklık; `cross` ikinci yöndeki çizgi.
 */
const PATTERNS: Readonly<
  Record<string, { readonly gap: number; readonly lines: number; readonly cross: boolean }>
> = {
  original: { gap: 8, lines: 0, cross: false },
  local_painted: { gap: 9, lines: 1, cross: false },
  painted: { gap: 4, lines: 1, cross: false },
  replaced: { gap: 6, lines: 1, cross: true },
}

interface Props {
  readonly map: readonly ShowcasePaintChange[]
  /** Erişilebilir ad: okunabilir listeye işaret eden özet (R16, AC-29). */
  readonly label: string
  /** Okunabilir listenin kimliği; şema ona `aria-describedby` ile bağlanır. */
  readonly describedBy: string
  /** Lejantın aynı desenleri kullanabilmesi için desen kimliklerinin öneki. */
  readonly patternPrefix: string
}

/**
 * `0016b` R13, R16, R19 — aracın üstten şeması. **Etkileşimsizdir** (D11): `role="img"`,
 * içinde odaklanabilir öğe yok; bilginin metin karşılığı okunabilir listededir (R17).
 * Renkler token katmanından CSS ile gelir (C3); işaretlemede renk değeri yoktur.
 */
export function PaintChangeSchema({ map, label, describedBy, patternPrefix }: Props) {
  const states = new Map(map.map((entry) => [entry.part, entry.state]))

  return (
    <svg
      className="paint-map__schema"
      viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
      role="img"
      aria-label={label}
      aria-describedby={describedBy}
      focusable="false"
      data-testid="paint-map-schema"
    >
      <PaintPatterns prefix={patternPrefix} />

      {WHEELS.map((shape, index) => (
        <Rect key={`wheel-${index}`} shape={shape} className="paint-map__wheel" />
      ))}
      {MIRRORS.map((shape, index) => (
        <Rect key={`mirror-${index}`} shape={shape} className="paint-map__trim" />
      ))}
      {GLASS.map((shape, index) => (
        <Rect key={`glass-${index}`} shape={shape} className="paint-map__glass" />
      ))}

      {(Object.keys(SHAPES) as PaintChangePart[]).map((part) => {
        const state = states.get(part) ?? 'original'

        return (
          <Rect
            key={part}
            shape={SHAPES[part]}
            className={`paint-map__part paint-map__part--${state}`}
            fill={`url(#${patternPrefix}-${state})`}
            data={{ part, state }}
          />
        )
      })}
    </svg>
  )
}

/** Desen tanımları; lejant aynı kimliklerle başvurur (tek tanım, iki kullanım). */
function PaintPatterns({ prefix }: { readonly prefix: string }) {
  return (
    <defs>
      {PAINT_CHANGE_STATES.map((state) => {
        const pattern = PATTERNS[state]!

        return (
          <pattern
            key={state}
            id={`${prefix}-${state}`}
            data-state={state}
            data-gap={pattern.gap}
            patternUnits="userSpaceOnUse"
            width={pattern.gap}
            height={pattern.gap}
            patternTransform="rotate(45)"
          >
            <rect className="paint-map__ground" width={pattern.gap} height={pattern.gap} />
            {pattern.lines > 0 ? (
              <line
                className={`paint-map__hatch paint-map__hatch--${state}`}
                x1="0"
                y1="0"
                x2="0"
                y2={pattern.gap}
              />
            ) : null}
            {pattern.cross ? (
              <line
                className={`paint-map__hatch paint-map__hatch--${state}`}
                x1="0"
                y1="0"
                x2={pattern.gap}
                y2="0"
              />
            ) : null}
          </pattern>
        )
      })}
    </defs>
  )
}

function Rect({
  shape,
  className,
  fill,
  data,
}: {
  readonly shape: Shape
  readonly className: string
  readonly fill?: string
  readonly data?: { readonly part: string; readonly state: string }
}) {
  return (
    <rect
      className={className}
      x={shape.x}
      y={shape.y}
      width={shape.width}
      height={shape.height}
      rx={shape.radius}
      fill={fill}
      data-part={data?.part}
      data-state={data?.state}
    />
  )
}
