import { describe, expect, it } from 'vitest'
import {
  addInterval,
  copyPrevious,
  MAX_INTERVALS,
  removeInterval,
  scheduleOf,
  schedulePayload,
} from './openingHoursForm'

describe('0025 — çalışma saatleri formunun saf mantığı (D5)', () => {
  it('0025/AC-2: programsız şube yedi gün kapalı başlar', () => {
    expect(Object.values(scheduleOf(null))).toEqual([[], [], [], [], [], [], []])
  })

  it('0025/AC-5: üçüncü aralıktan sonra ekleme yapılmaz', () => {
    let program = scheduleOf(null)
    for (let i = 0; i < MAX_INTERVALS + 2; i += 1) {
      program = addInterval(program, 'monday')
    }
    expect(program.monday).toHaveLength(3)
  })

  it('0025/AC-6: aralık kaldırılır; sonuncusu kaldırılınca gün kapalı', () => {
    const iki = scheduleOf({
      monday: [
        { start: '09:00', end: '13:00' },
        { start: '16:00', end: '21:00' },
      ],
    })
    const bir = removeInterval(iki, 'monday', 0)
    expect(bir.monday).toEqual([{ start: '16:00', end: '21:00' }])
    expect(removeInterval(bir, 'monday', 0).monday).toEqual([])
  })

  it('0025/AC-7: "Önceki günle aynı" Pazartesinin aralıklarını Salı\'ya kopyalar; Pazartesi değişmez', () => {
    const program = scheduleOf({ monday: [{ start: '09:00', end: '18:00' }] })
    const kopya = copyPrevious(program, 'tuesday')
    expect(kopya.tuesday).toEqual([{ start: '09:00', end: '18:00' }])
    expect(copyPrevious(program, 'monday')).toBe(program)
  })

  it('0025/AC-24 (P8): gövde her zaman yedi günü taşır; boş program yedi boş gündür', () => {
    expect(schedulePayload(scheduleOf(null))).toEqual({
      monday: [],
      tuesday: [],
      wednesday: [],
      thursday: [],
      friday: [],
      saturday: [],
      sunday: [],
    })
  })
})
