import type { Translator } from '../../shared/i18n/i18n'

/**
 * Nitelik anahtarının ziyaretçinin dilindeki ADI. Anahtarlar sunucunun yazabildiği kümedir
 * (`ShowcaseListingDetailQueries.BuildAttributesAsync`): beş referans alanı ve renk. Opsiyonel
 * alanlardan bugün hiçbiri buraya inmez — projektör yalnız `LocalizedText` taşır ve öyle bir
 * alan tanımlı değil; biri tanımlanırsa `VitrindeAdiOlanAlanlarTests` kırılır ve adı buraya
 * eklenir. Bilinmeyen anahtar `null` döner ve satır sunucunun etiketiyle basılır.
 *
 * `frontend-003` **D11** — eşleme TEK yerdedir: ilan detayı ve karşılaştırma tablosu bu dosyayı
 * içe aktarır; ikinci bir kopya yazılmaz (`0002`'nin "Hatchback | Hatchback" kaçan hatası).
 * `VitrindeAdiOlanAlanlarTests` bu dosyayı okur ve `case 'body_type':`'ın `src` altında tek
 * dosyada geçtiğini ölçer.
 */
export function attributeName(key: string, t: Translator): string | null {
  switch (key) {
    case 'body_type':
      return t('detail.field.bodyType')
    case 'fuel_type':
      return t('detail.field.fuelType')
    case 'transmission_type':
      return t('detail.field.transmissionType')
    case 'drivetrain_type':
      return t('detail.field.drivetrainType')
    case 'trim_level':
      return t('detail.field.trimLevel')
    case 'color':
      return t('detail.field.color')
    default:
      return null
  }
}
