import { expect, test } from '@playwright/test'

test.describe('Paylaşılabilirlik ve geri tuşu', () => {
  test('AC-18: Daraltılmış ve ikinci sayfadaki bir listenin bağlantısı yeni bir oturumda açıldığında aynı daraltma ve aynı sayfa görünür', async ({
    browser,
    request,
  }) => {
    // Kriter "ikinci sayfadaki liste" diyor; veri seti buna yetmiyorsa test sessizce
    // yanlış şeyi doğrulamasın diye önkoşul açıkça kontrol edilir. Beklenen veri seti
    // scripts/dev-up.sh varsayılanıdır (200 ilan) ve CI de onunla koşar.
    const yanit = await request.get('/api/catalog/showcase-listings?bodyType=suv&locale=tr')
    const sayfaSayisi = (await yanit.json()).totalPages as number

    expect(
      sayfaSayisi,
      'AC-18 en az iki sayfalık daraltılmış küme gerektirir — Host yeterli veriyle seed edilmeli (scripts/dev-up.sh 200)',
    ).toBeGreaterThanOrEqual(2)

    const context = await browser.newContext()
    const page = await context.newPage()

    await page.goto('/tr/?bodyType=suv&page=2')
    await expect(page.getByTestId('result-count')).toBeVisible()

    const seciliKutu = page.getByRole('checkbox', { name: 'SUV' })
    await expect(seciliKutu).toBeChecked()
    await expect(page.getByTestId('page-indicator')).toContainText('2')

    await context.close()
  })

  test('AC-19: İkinci sayfadayken bir kasa tipi seçildikten sonra geri tuşu TEK hareketle daraltmasız ikinci sayfa hâline döner', async ({
    page,
  }) => {
    await page.goto('/tr/?page=2')
    await expect(page.getByTestId('page-indicator')).toContainText('2')

    await page.getByRole('checkbox', { name: 'SUV' }).check()

    // AC-8: daraltma değişince liste ilk sayfadan başlar — kanonik biçimde page yazılmaz.
    await expect.poll(() => new URL(page.url()).searchParams.get('page')).toBeNull()
    expect(new URL(page.url()).searchParams.getAll('bodyType')).toEqual(['suv'])

    await page.goBack()

    // Ara bir geçmiş kaydına uğranmaz: daraltma değişikliği TEK kayıt eklemiştir (R5).
    const url = new URL(page.url())
    expect(url.searchParams.getAll('bodyType')).toEqual([])
    expect(url.searchParams.get('page')).toBe('2')
  })

  test('AC-14: Son sayfanın ötesinde bir sayfa istendiğinde ilk sayfa gösterilir ve adres kanonik biçime düzeltilir', async ({
    page,
  }) => {
    await page.goto('/tr/?page=999')
    await expect(page.getByTestId('result-count')).toBeVisible()

    // Düzeltme replaceState ile yapılır; geçmişe yeni kayıt EKLEMEZ.
    await expect.poll(() => new URL(page.url()).searchParams.get('page')).toBeNull()
    await expect(page.getByRole('alert')).toHaveCount(0)
  })

  test('C13: page=1 ve boş bodyType kanonik adreste yazılmaz', async ({ page }) => {
    await page.goto('/tr/?page=1&bodyType=')
    await expect(page.getByTestId('result-count')).toBeVisible()
    await expect.poll(() => new URL(page.url()).search).toBe('')
  })
})
