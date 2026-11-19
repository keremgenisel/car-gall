import { expect, type APIRequestContext, type Page } from '@playwright/test'

/**
 * `0018` e2e yardımcıları. Teyit iletisi GERÇEKTEN gönderilir ve Mailpit'ten okunur: CI'da
 * `mailpit` servisi `8025`'te (ci.yml), yerelde `E2E_MAILPIT_URL`.
 */
export const MAILPIT = process.env['E2E_MAILPIT_URL'] ?? 'http://localhost:8025'

export function benzersizAdres(): string {
  return `bulten-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@ornek.com`
}

interface MailpitArama {
  readonly messages: readonly { readonly ID: string }[]
}

/** Adrese giden ilk iletinin düz metin gövdesi; kuyruk servisi 5 sn'de bir döner. */
export async function iletiGovdesi(api: APIRequestContext, adres: string): Promise<string> {
  let kimlik: string | undefined

  await expect
    .poll(
      async () => {
        const yanit = await api.get(
          `${MAILPIT}/api/v1/search?query=${encodeURIComponent(`to:"${adres}"`)}`,
        )
        kimlik = ((await yanit.json()) as MailpitArama).messages[0]?.ID
        return kimlik
      },
      { timeout: 30_000, intervals: [1_000] },
    )
    .toBeTruthy()

  const ileti = await api.get(`${MAILPIT}/api/v1/message/${kimlik}`)
  return ((await ileti.json()) as { readonly Text: string }).Text
}

/** Gövdedeki teyit bağlantısının YOLU (kök kurulumun `PublicBaseUrl`'i). */
export function teyitYolu(govde: string): string {
  const eslesme = /https?:\/\/[^/\s]+(\/[a-z]{2}\/newsletter\/confirm\?token=[A-Za-z0-9_%-]+)/.exec(
    govde,
  )
  expect(eslesme, govde).not.toBeNull()
  return eslesme![1]!
}

/** Arapça sayfada metnin hizası Range ile: blok kutu satırı kaplar ve hizayı göstermez. */
export async function sagaHizali(page: Page, secici: string): Promise<void> {
  const olcum = await page
    .locator(secici)
    .first()
    .evaluate((element) => {
      const range = document.createRange()
      range.selectNodeContents(element)
      return {
        yon: getComputedStyle(element).direction,
        metinSag: range.getBoundingClientRect().right,
        kutuSag: element.getBoundingClientRect().right,
        kutuSol: element.getBoundingClientRect().left,
        metinSol: range.getBoundingClientRect().left,
      }
    })

  expect(olcum.yon).toBe('rtl')
  expect(Math.abs(olcum.metinSag - olcum.kutuSag), JSON.stringify(olcum)).toBeLessThan(2)
}
