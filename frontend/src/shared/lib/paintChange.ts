/**
 * `0016a` C10 — 13 parçanın KODLARI ve SIRASI; tablonun tek evi `specs/0016a` C10'dur.
 * Sıra hem panel formunun hem vitrinin okunabilir listesinin sırasıdır. Kod çevrilmez;
 * görünen ad `paintChange.part.<kod>` anahtarından gelir (C5, C6).
 *
 * `shared/lib`'te: panel (`0016a`) ve vitrin (`0016b`) aynı tabloyu okur.
 */
export const PAINT_CHANGE_PARTS = [
  'front_bumper',
  'hood',
  'roof',
  'trunk_lid',
  'rear_bumper',
  'left_front_fender',
  'left_front_door',
  'left_rear_door',
  'left_rear_fender',
  'right_front_fender',
  'right_front_door',
  'right_rear_door',
  'right_rear_fender',
] as const

export type PaintChangePart = (typeof PAINT_CHANGE_PARTS)[number]

/** `0016a` R10 · D1 — dört durum; "belirtilmedi" diye bir durum YOKTUR. */
export const PAINT_CHANGE_STATES = ['original', 'local_painted', 'painted', 'replaced'] as const

export type PaintChangeState = (typeof PAINT_CHANGE_STATES)[number]

export interface PaintChangeEntry {
  readonly part: string
  readonly state: string
}
