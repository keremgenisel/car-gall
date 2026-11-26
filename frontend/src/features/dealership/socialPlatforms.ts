/**
 * `0024` D1 — platform listesi kapalı ve sıralıdır; sunucunun listesiyle aynı kodlar. Görünen ad
 * markanın kendi adıdır, ÇEVRİLMEZ ve dilden bağımsızdır (D6, AC-60): bu yüzden katalogda değil
 * burada yaşar. Sıra panelde ve altlıkta aynıdır.
 */
export const SOCIAL_PLATFORMS = [
  { code: 'instagram', name: 'Instagram' },
  { code: 'facebook', name: 'Facebook' },
  { code: 'youtube', name: 'YouTube' },
  { code: 'tiktok', name: 'TikTok' },
  { code: 'x', name: 'X' },
  { code: 'linkedin', name: 'LinkedIn' },
] as const

export type SocialPlatformCode = (typeof SOCIAL_PLATFORMS)[number]['code']

/** Bilinmeyen kod için `undefined`: sunucu D1 dışını zaten süzer (D3). */
export function socialPlatformName(code: string): string | undefined {
  return SOCIAL_PLATFORMS.find((platform) => platform.code === code)?.name
}
