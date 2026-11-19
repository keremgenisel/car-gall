import { expect, type Page } from '@playwright/test'

/**
 * `0021` R8 — araç formunda önce marka, sonra o markanın modeli seçilir. Formu dolduran akışlar
 * markayı SIRAYLA değil, modeli olan İLK markayı seçerek doldurur: kurulumda modeli olmayan bir
 * marka bulunabilir (yönetici markayı ekleyip modelini sonra ekler; `evidence-0021` bunu kurar)
 * ve sıradaki ilk markayı seçmek model seçicisini boş bırakırdı (AC-28).
 */
export async function modeliOlanMarkayiSecAsync(page: Page): Promise<void> {
  const marka = page.getByLabel('Marka', { exact: true })
  await expect(marka).toBeVisible()

  const degerler = await marka
    .locator('option')
    .evaluateAll((secenekler) =>
      secenekler
        .map((secenek) => (secenek as HTMLOptionElement).value)
        .filter((deger) => deger !== ''),
    )

  for (const deger of degerler) {
    await marka.selectOption(deger)

    if ((await page.getByLabel('Model', { exact: true }).locator('option').count()) > 1) {
      return
    }
  }

  throw new Error('Modeli olan aktif bir marka bulunamadı.')
}
