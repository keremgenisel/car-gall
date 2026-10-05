import { useEffect, useState } from 'react'
import { useLocale } from '../../../app/localeContext'
import { ApiError } from '../../../shared/api/client'
import {
  createVehicle,
  fetchBranches,
  fetchReferenceOptions,
  type ReferenceSetName,
} from '../../../shared/api/catalogAdmin'
import type { BranchSummary, ReferenceOption } from '../../../shared/api/types'
import { adminPath } from '../../../shared/lib/route'
import { Button, Card, ErrorState, Field, Skeleton } from '../../../shared/ui'
import { VehicleSelect } from './VehicleSelect'
import {
  codeKey,
  EMPTY_VEHICLE_FORM,
  firstMissingCoreField,
  vehicleMessageKey,
  type VehicleFormValues,
} from './vehicleFormState'

type Options = Record<ReferenceSetName, readonly ReferenceOption[]>

/** Seçenekler İKİ uçtan gelir: referans veri `VehicleReference`'tan, şubeler `Dealership`'ten. */
type Sources =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | {
      readonly kind: 'ready'
      readonly options: Options
      readonly branches: readonly BranchSummary[]
    }

type Submission =
  | { readonly kind: 'idle' }
  | { readonly kind: 'submitting' }
  | { readonly kind: 'saved' }
  /** `field === null` ise hata alana bağlanamıyor ve genel şeride düşer (AC-42, AC-43). */
  | { readonly kind: 'failed'; readonly messageKey: string; readonly field: string | null }

/**
 * Araç kaydı formu (R1, AC-1).
 *
 * Dört durum da ele alınıyor (R22, AC-40…AC-43): seçenekler yüklenirken iskelet, kayıt
 * sırasında yükleniyor hâli, başarıda bildirim, hatada alan işaretleme veya genel şerit +
 * tekrar dene. Hiçbir durumda HTTP kodu, `errorCode` veya `traceId` ekrana basılmaz
 * (AC-47) — `ApiError`'ın kodu yalnız çeviri anahtarı aramak için kullanılır.
 *
 * Renk YALNIZ personelin kendi locale'inde giriliyor: renk çok dilli serbest metindir ve
 * R10 çok dilli metni "yarıda bırakıp dönmek" muafiyetine dâhil ediyor; diğer dillerin
 * girişi düzenleme ekranının işi (`0010b`).
 */
export function VehicleFormPage() {
  const { locale, t } = useLocale()
  const [loaded, setLoaded] = useState<{ readonly key: string; readonly value: Sources } | null>(
    null,
  )
  const [sourceToken, setSourceToken] = useState(0)
  const [values, setValues] = useState<VehicleFormValues>(EMPTY_VEHICLE_FORM)
  const [submission, setSubmission] = useState<Submission>({ kind: 'idle' })

  /**
   * Sonuç isteğin anahtarıyla tutuluyor, "yükleniyor" ondan türetiliyor: efektin
   * gövdesinde durumu sıfırlamak basamaklı render üretiyor
   * (`react-hooks/set-state-in-effect`) ve dil değiştiğinde eski etiketler zaten geçersiz.
   */
  const sourceKey = `${locale}|${sourceToken}`
  const sources: Sources =
    loaded !== null && loaded.key === sourceKey ? loaded.value : { kind: 'loading' }

  useEffect(() => {
    const controller = new AbortController()

    Promise.all([
      fetchReferenceOptions(locale, controller.signal),
      fetchBranches(locale, controller.signal),
    ])
      .then(([options, branches]) =>
        setLoaded({ key: sourceKey, value: { kind: 'ready', options, branches } }),
      )
      .catch((cause: unknown) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') {
          return
        }

        setLoaded({ key: sourceKey, value: { kind: 'error' } })
      })

    return () => controller.abort()
  }, [locale, sourceToken, sourceKey])

  async function submit() {
    const missing = firstMissingCoreField(values)

    if (missing !== null) {
      setSubmission({
        kind: 'failed',
        messageKey: 'vehicle.form.error.core_field_missing',
        field: missing,
      })
      return
    }

    setSubmission({ kind: 'submitting' })

    try {
      await createVehicle(
        {
          branchId: values.branchId,
          vehicleModelId: values.vehicleModelId,
          bodyType: values.bodyType,
          fuelType: values.fuelType,
          transmissionType: values.transmissionType,
          drivetrainType: values.drivetrainType,
          trimLevel: values.trimLevel,
          color: values.color.trim() === '' ? {} : { [locale]: values.color.trim() },
          modelYear: Number(values.modelYear),
          mileageKilometers: Number(values.mileageKilometers),
          // P4 — "fiyat sorunuz" ile tutar birlikte gönderilemez; sunucu ikisini birden
          // alırsa isteği reddediyor ve haklı: hangisinin kazandığını sessizce seçmek
          // veri bozulmasının en sessiz biçimi olurdu.
          priceAmount: values.priceOnRequest ? null : Number(values.priceAmount),
          priceOnRequest: values.priceOnRequest,
          vin: values.vin.trim(),
        },
        locale,
      )

      // AC-43'ün tersi: BAŞARIDA form temizlenir, çünkü personel sıradaki aracı giriyor.
      setValues(EMPTY_VEHICLE_FORM)
      setSubmission({ kind: 'saved' })
    } catch (cause) {
      const errorCode = cause instanceof ApiError ? cause.errorCode : 'api.unavailable'

      // AC-43 — girilen bilgiler KAYBOLMAZ: `values` burada hiç ellenmiyor.
      setSubmission({
        kind: 'failed',
        messageKey: vehicleMessageKey(errorCode, locale),
        field: cause instanceof ApiError ? cause.field : null,
      })
    }
  }

  if (sources.kind === 'loading') {
    return (
      <div className="admin-page admin-page--narrow" data-testid="vehicle-form-skeleton">
        <h1 className="admin-page__title">{t('vehicle.form.title')}</h1>
        <div className="admin-form">
          {[0, 1, 2, 3, 4, 5].map((row) => (
            <Skeleton key={row} shape="line" />
          ))}
        </div>
      </div>
    )
  }

  if (sources.kind === 'error') {
    return (
      <div className="admin-page admin-page--narrow">
        <h1 className="admin-page__title">{t('vehicle.form.title')}</h1>
        <ErrorState
          title={t('vehicle.form.sources.error.title')}
          description={t('vehicle.form.sources.error.body')}
          retryLabel={t('vehicle.form.sources.error.retry')}
          onRetry={() => setSourceToken((token) => token + 1)}
          testId="vehicle-form-sources-error"
        />
      </div>
    )
  }

  const submitting = submission.kind === 'submitting'
  const errorKeyFor = (field: string) =>
    submission.kind === 'failed' && submission.field === field ? submission.messageKey : undefined

  const branchOptions: readonly ReferenceOption[] = sources.branches.map((branch) => ({
    value: branch.branchId,
    // Kapatılmış şube listede KALIR (uç onu süzmüyor) ama durumu etiketinde görünür:
    // personel kapalı bir şubeyi bilmeden seçmesin. Süzmek, olmayan bir kural uydurmak
    // olurdu — sunucu kapalı şubeye kaydı reddetmiyor.
    label:
      branch.status === 'Active'
        ? branch.name
        : t('vehicle.form.branch.withStatus', {
            name: branch.name,
            status: t(codeKey('branch.status', branch.status)),
          }),
  }))

  return (
    <div className="admin-page admin-page--narrow">
      <h1 className="admin-page__title">{t('vehicle.form.title')}</h1>

      {submission.kind === 'saved' ? (
        <div className="admin-form__notice" role="status" data-testid="vehicle-saved">
          <p className="admin-form__notice-body">{t('vehicle.form.saved')}</p>
          <div className="admin-form__banner-action">
            <a className="ui-button ui-button--secondary" href={adminPath('vehicles')}>
              {t('vehicle.form.saved.action')}
            </a>
          </div>
        </div>
      ) : null}

      {submission.kind === 'failed' && submission.field === null ? (
        <div className="admin-form__error" role="alert" data-testid="vehicle-form-error">
          <p className="admin-form__error-body">{t(submission.messageKey)}</p>
          {/* AC-43 — tekrar dene AYNI değerleri gönderir; form hiç temizlenmiyor. */}
          <div className="admin-form__banner-action">
            <Button variant="secondary" onClick={() => void submit()}>
              {t('vehicle.form.retry')}
            </Button>
          </div>
        </div>
      ) : null}

      <Card as="div" testId="vehicle-form">
        <form
          className="admin-form"
          noValidate
          onSubmit={(event) => {
            event.preventDefault()
            void submit()
          }}
        >
          <VehicleSelect
            id="vehicle-branch"
            label={t('vehicle.form.field.branch')}
            options={branchOptions}
            value={values.branchId}
            errorKey={errorKeyFor('branchId')}
            onChange={(branchId) => setValues({ ...values, branchId })}
          />

          <VehicleSelect
            id="vehicle-model"
            label={t('vehicle.form.field.model')}
            options={sources.options.models}
            value={values.vehicleModelId}
            errorKey={errorKeyFor('vehicleModelId')}
            onChange={(vehicleModelId) => setValues({ ...values, vehicleModelId })}
          />

          <VehicleSelect
            id="vehicle-body-type"
            label={t('vehicle.form.field.bodyType')}
            options={sources.options.bodyTypes}
            value={values.bodyType}
            errorKey={errorKeyFor('bodyType')}
            onChange={(bodyType) => setValues({ ...values, bodyType })}
          />

          <VehicleSelect
            id="vehicle-trim-level"
            label={t('vehicle.form.field.trimLevel')}
            options={sources.options.trimLevels}
            value={values.trimLevel}
            errorKey={errorKeyFor('trimLevel')}
            onChange={(trimLevel) => setValues({ ...values, trimLevel })}
          />

          <VehicleSelect
            id="vehicle-fuel-type"
            label={t('vehicle.form.field.fuelType')}
            options={sources.options.fuelTypes}
            value={values.fuelType}
            errorKey={errorKeyFor('fuelType')}
            onChange={(fuelType) => setValues({ ...values, fuelType })}
          />

          <VehicleSelect
            id="vehicle-transmission-type"
            label={t('vehicle.form.field.transmissionType')}
            options={sources.options.transmissions}
            value={values.transmissionType}
            errorKey={errorKeyFor('transmissionType')}
            onChange={(transmissionType) => setValues({ ...values, transmissionType })}
          />

          <VehicleSelect
            id="vehicle-drivetrain-type"
            label={t('vehicle.form.field.drivetrainType')}
            options={sources.options.drivetrains}
            value={values.drivetrainType}
            errorKey={errorKeyFor('drivetrainType')}
            onChange={(drivetrainType) => setValues({ ...values, drivetrainType })}
          />

          <Field
            id="vehicle-model-year"
            label={t('vehicle.form.field.modelYear')}
            type="number"
            value={values.modelYear}
            errorKey={errorKeyFor('modelYear')}
            onChange={(modelYear) => setValues({ ...values, modelYear })}
          />

          <Field
            id="vehicle-mileage"
            label={t('vehicle.form.field.mileage')}
            type="number"
            value={values.mileageKilometers}
            errorKey={errorKeyFor('mileageKilometers')}
            onChange={(mileageKilometers) => setValues({ ...values, mileageKilometers })}
          />

          <Field
            id="vehicle-color"
            label={t('vehicle.form.field.color')}
            hint={t('vehicle.form.field.color.hint')}
            value={values.color}
            errorKey={errorKeyFor('color')}
            onChange={(color) => setValues({ ...values, color })}
          />

          <Field
            id="vehicle-vin"
            label={t('vehicle.form.field.vin')}
            hint={t('vehicle.form.field.vin.hint')}
            value={values.vin}
            errorKey={errorKeyFor('vin')}
            onChange={(vin) => setValues({ ...values, vin })}
          />

          <div className="admin-form__field">
            <label className="admin-form__check" htmlFor="vehicle-price-on-request">
              <input
                id="vehicle-price-on-request"
                type="checkbox"
                checked={values.priceOnRequest}
                onChange={(event) =>
                  // Tutar da TEMİZLENİR: ekranda kalan bir tutar, "fiyat sorunuz"
                  // işaretliyken gönderilmeyeceği hâlde girilmiş gibi görünürdü.
                  setValues({
                    ...values,
                    priceOnRequest: event.target.checked,
                    priceAmount: event.target.checked ? '' : values.priceAmount,
                  })
                }
              />
              {t('vehicle.form.field.priceOnRequest')}
            </label>
          </div>

          {values.priceOnRequest ? null : (
            <Field
              id="vehicle-price"
              label={t('vehicle.form.field.price')}
              type="number"
              value={values.priceAmount}
              errorKey={errorKeyFor('price')}
              onChange={(priceAmount) => setValues({ ...values, priceAmount })}
            />
          )}

          {/* AC-40 — kaydet düğmesi yükleniyor hâline geçer ve form İKİ KEZ gönderilemez:
              `Button` yükleniyor hâlinde kendini devre dışı bırakıyor. */}
          <Button type="submit" loading={submitting} data-testid="vehicle-submit">
            {submitting ? t('vehicle.form.submitting') : t('vehicle.form.submit')}
          </Button>
        </form>
      </Card>
    </div>
  )
}
