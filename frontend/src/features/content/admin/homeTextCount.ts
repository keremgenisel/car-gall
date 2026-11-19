/**
 * `0022` **D15** — panelin sayacı sunucuyla AYNI kuralla sayar: Unicode kod noktası
 * (`ContentRules.Length`, `EnumerateRunes`). `String.length` UTF-16 birimi sayar ve bir
 * emojiyi iki karakter gösterirdi; dizinin yineleyicisi kod noktası başına bir öğe verir.
 */
export function codePointLength(value: string): number {
  return Array.from(value).length
}
