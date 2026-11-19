import { useLocale } from '../../../app/localeContext'
import type { ModelOption, ReferenceOption, VehicleAdminModel } from '../../../shared/api/types'
import { VehicleSelect } from './VehicleSelect'
import { brandModelOptions } from './vehicleFormState'

interface Props {
  readonly brands: readonly ReferenceOption[]
  readonly models: readonly ModelOption[]
  readonly brandId: string
  readonly vehicleModelId: string
  /** Düzenleme formu: aracın kayıtlı (belki pasif) marka ve modeli (D24, AC-26). */
  readonly current?: (VehicleAdminModel & { readonly vehicleModelId: string }) | null
  readonly brandErrorKey?: string
  readonly modelErrorKey?: string
  readonly onChange: (next: { readonly brandId: string; readonly vehicleModelId: string }) => void
}

/**
 * `0021` R8, R9 · D22 — araç formunda önce marka, sonra o markanın modeli. Model seçicisi
 * marka seçilene kadar devre dışıdır (AC-23); marka değişince seçili model temizlenir (AC-25);
 * aktif modeli olmayan bir marka seçilince seçici bunu söyler ve model boş kaldığı için kayıt
 * yapılamaz (AC-28).
 */
export function BrandModelFields({
  brands,
  models,
  brandId,
  vehicleModelId,
  current,
  brandErrorKey,
  modelErrorKey,
  onChange,
}: Props) {
  const { t } = useLocale()
  const options = brandModelOptions(brands, models, brandId, current)
  const noModels = brandId !== '' && options.models.length === 0

  return (
    <>
      <VehicleSelect
        id="vehicle-brand"
        label={t('vehicle.form.field.brand')}
        options={options.brands}
        value={brandId}
        errorKey={brandErrorKey}
        onChange={(next) =>
          onChange({ brandId: next, vehicleModelId: next === brandId ? vehicleModelId : '' })
        }
      />

      <VehicleSelect
        id="vehicle-model"
        label={t('vehicle.form.field.model')}
        options={options.models}
        value={vehicleModelId}
        disabled={brandId === '' || noModels}
        hintKey={
          brandId === ''
            ? 'vehicle.form.model.chooseBrandFirst'
            : noModels
              ? 'vehicle.form.model.noneForBrand'
              : undefined
        }
        errorKey={modelErrorKey}
        onChange={(next) => onChange({ brandId, vehicleModelId: next })}
      />
    </>
  )
}
