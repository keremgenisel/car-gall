/**
 * `0024` D1 — platform listesi kapalı ve sıralıdır; sunucunun listesiyle aynı kodlar, aynı sıra
 * (nesnenin anahtar sırası). Görünen ad markanın kendi adıdır, ÇEVRİLMEZ ve dilden bağımsızdır
 * (D6, AC-60): bu yüzden katalogda değil burada yaşar. Panel ve altlık aynı listeyi kullanır.
 * Dizi değil harita: giriş paketinde daha az bayt (QA 2 BULGU-1).
 */
export const SOCIAL_PLATFORM_NAMES: Readonly<Record<string, string>> = {
  instagram: 'Instagram',
  facebook: 'Facebook',
  youtube: 'YouTube',
  tiktok: 'TikTok',
  x: 'X',
  linkedin: 'LinkedIn',
}
