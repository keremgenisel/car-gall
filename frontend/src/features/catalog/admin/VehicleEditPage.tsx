import { useEffect, useState } from 'react'
import { useLocale } from '../../../app/localeContext'
import { ApiError } from '../../../shared/api/client'
import {
  fetchBranches,
  fetchReferenceOptions,
  fetchVehicle,
  fetchVehicleFields,
  updateVehicle,
  type ReferenceSetName,
} from '../../../shared/api/catalogAdmin'
import type {
  BranchSummary,
  ReferenceOption,
  VehicleAdminDetail,
  VehicleAdminField,
} from '../../../shared/api/types'
import { SUPPORTED_LOCALES } from '../../../shared/lib/locale'
import { adminPath, vehicleMediaPath } from '../../../shared/lib/route'
import { Button, Card, ErrorState, Field, Skeleton } from '../../../shared/ui'
import { ListingActionBar } from './ListingActionBar'
import { OptionalFieldInput } from './OptionalFieldInput'
import { VehicleSelect } from './VehicleSelect'
import {
  codeKey,
  missingLocalesOf,
  toEditValues,
  toUpdateRequest,
  vehicleMessageKey,
  type VehicleEditValues,
} from './vehicleFormState'

type Options = Record<ReferenceSetName, readonly ReferenceOption[]>

type Sources =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'missing' }
  | {
      readonly kind: 'ready'
      readonly detail: VehicleAdminDetail
      readonly options: Options
      readonly branches: readonly BranchSummary[]
      readonly fields: readonly VehicleAdminField[]
    }

type Submission =
  | { readonly kind: 'idle' }
  | { readonly kind: 'submitting' }
  | { readonly kind: 'saved' }
  | { readonly kind: 'failed'; readonly messageKey: string; readonly field: string | null }

/**
 * Araç düzenleme ekranı (0010b R1).
 *
 * <p>Dört durum da ele alınıyor (R17, `0010a` R22): kayıt yüklenirken iskelet, kaydetme
 * sırasında yükleniyor, başarıda bildirim, hatada alan işaretleme veya genel şerit +
 * tekrar dene. Hiçbir durumda HTTP kodu, `errorCode` veya `traceId` ekrana basılmaz
 * (AC-33).</p>
 *
 * <p><b>Şase SALT OKUNUR</b> (R4/D20): fiziksel aracın kimliği ve değişmesi "aynı kayıt,
 * başka araç" demek. Gövdede yine de taşınıyor ki sunucu bir değiştirme girişimini
 * reddedebilsin (D24).</p>
 */
export function VehicleEditPage({ vehicleId }: { readonly vehicleId: string }) {
  const { locale, t } = useLocale()
  const [loaded, setLoaded] = useState<{ readonly key: string; readonly value: Sources } | null>(
    null,
  )
  const [sourceToken, setSourceToken] = useState(0)
  const [values, setValues] = useState<VehicleEditValues | null>(null)
  const [submission, setSubmission] = useState<Submission>({ kind: 'idle' })

  const sourceKey = `${locale}|${vehicleId}|${sourceToken}`
  const sources: Sources =
    loaded !== null && loaded.key === sourceKey ? loaded.value : { kind: 'loading' }

  useEffect(() => {
    const controller = new AbortController()

    Promise.all([
      fetchVehicle(vehicleId, locale, controller.signal),
      fetchReferenceOptions(locale, controller.signal),
      fetchBranches(locale, controller.signal),
      fetchVehicleFields(locale, controller.signal),
    ])
      .then(([detail, options, branches, fields]) => {
        setValues(toEditValues(detail, SUPPORTED_LOCALES))
        setLoaded({ key: sourceKey, value: { kind: 'ready', detail, options, branches, fields } })
      })
      .catch((cause: unknown) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') {
          return
        }

        // AC-4'ün uç tarafı: bulunamayan araç bir HATA değil, olmayan bir kayıttır ve
        // "tekrar dene" onu getirmez.
        const missing = cause instanceof ApiError && cause.status === 404

        setLoaded({ key: sourceKey, value: { kind: missing ? 'missing' : 'error' } })
      })

    return () => controller.abort()
  }, [locale, vehicleId, sourceToken, sourceKey])

  async function submit() {
    if (sources.kind !== 'ready' || values === null) {
      return
    }

    setSubmission({ kind: 'submitting' })

    try {
      await updateVehicle(vehicleId, toUpdateRequest(values, sources.detail.color, locale), locale)
      setSubmission({ kind: 'saved' })
    } catch (cause) {
      const errorCode = cause instanceof ApiError ? cause.errorCode : 'api.unavailable'

      // AC-43'ün düzenlemedeki karşılığı: girilen bilgiler KAYBOLMAZ.
      setSubmission({
        kind: 'failed',
        messageKey: vehicleMessageKey(errorCode, locale),
        field: cause instanceof ApiError ? cause.field : null,
      })
    }
  }

  // `values === null` koşulu YALNIZ yüklenirken geçerli: hata ve "bulunamadı"
  // dallarında da değerler null'dır ve bu koşul onları iskelette dondururdu — ölçüldü,
  // 404 ekranı hiç basılmıyordu.
  if (sources.kind === 'loading') {
    return (
      <div className="admin-page admin-page--narrow" data-testid="vehicle-edit-skeleton">
        <h1 className="admin-page__title">{t('vehicle.edit.title')}</h1>
        <div className="admin-form">
          {[0, 1, 2, 3, 4, 5].map((row) => (
            <Skeleton key={row} shape="line" />
          ))}
        </div>
      </div>
    )
  }

  if (sources.kind === 'missing') {
    return (
      <div className="admin-page admin-page--narrow">
        <h1 className="admin-page__title">{t('vehicle.edit.title')}</h1>
        <div className="admin-form__notice" role="status" data-testid="vehicle-edit-missing">
          <p className="admin-form__notice-body">{t('vehicle.edit.missing')}</p>
          <div className="admin-form__banner-action">
            <a className="ui-button ui-button--secondary" href={adminPath('vehicles')}>
              {t('vehicle.form.saved.action')}
            </a>
          </div>
        </div>
      </div>
    )
  }

  if (sources.kind === 'error') {
    return (
      <div className="admin-page admin-page--narrow">
        <h1 className="admin-page__title">{t('vehicle.edit.title')}</h1>
        <ErrorState
          title={t('vehicle.form.sources.error.title')}
          description={t('vehicle.form.sources.error.body')}
          retryLabel={t('vehicle.form.sources.error.retry')}
          onRetry={() => setSourceToken((token) => token + 1)}
          testId="vehicle-edit-error"
        />
      </div>
    )
  }

  if (values === null) {
    return null
  }

  const submitting = submission.kind === 'submitting'
  const errorKeyFor = (field: string) =>
    submission.kind === 'failed' && submission.field === field ? submission.messageKey : undefined

  /**
   * Formun İŞARETLEYEBİLECEĞİ alanlar. Sunucu başka bir alana işaret ederse — ör. salt
   * okunur şase — mesaj hiçbir yere bağlanamaz ve sessizce KAYBOLURDU; ölçüldü. O hâlde
   * genel şeride düşer.
   */
  const markable = new Set([
    'branchId',
    'vehicleModelId',
    'bodyType',
    'fuelType',
    'transmissionType',
    'drivetrainType',
    'trimLevel',
    'modelYear',
    'mileageKilometers',
    'color',
    'price',
    'listingTitle',
    'listingDescription',
    ...sources.fields.map((field) => field.fieldKey),
  ])

  const banner =
    submission.kind === 'failed' && (submission.field === null || !markable.has(submission.field))

  const branchOptions: readonly ReferenceOption[] = sources.branches.map((branch) => ({
    value: branch.branchId,
    label:
      branch.status === 'Active'
        ? branch.name
        : t('vehicle.form.branch.withStatus', {
            name: branch.name,
            status: t(codeKey('branch.status', branch.status)),
          }),
  }))

  const missing = missingLocalesOf(values, SUPPORTED_LOCALES)

  return (
    <div className="admin-page admin-page--narrow">
      <div className="admin-page__header">
        <h1 className="admin-page__title">{t('vehicle.edit.title')}</h1>
        {/*
          0012a D17 — medya yüzeyi AYRI bir ekran ve oraya giden tek yol bu bağlantı.
          Sekme yapılmadı: iki kaydetme akışını birbirine bağlamak, hangi düğmenin neyi
          kaydettiğini belirsizleştirirdi.
        */}
        <a className="ui-button ui-button--text" href={vehicleMediaPath(vehicleId)}>
          {t('vehicle.media.open')}
        </a>
        <a className="ui-button ui-button--text" href={adminPath('vehicles')}>
          {t('vehicle.form.saved.action')}
        </a>
      </div>

      {submission.kind === 'saved' ? (
        <div className="admin-form__notice" role="status" data-testid="vehicle-edit-saved">
          <p className="admin-form__notice-body">{t('vehicle.edit.saved')}</p>
        </div>
      ) : null}

      {banner ? (
        <div className="admin-form__error" role="alert" data-testid="vehicle-edit-error-banner">
          <p className="admin-form__error-body">{t(submission.messageKey)}</p>
          <div className="admin-form__banner-action">
            <Button variant="secondary" onClick={() => void submit()}>
              {t('vehicle.form.retry')}
            </Button>
          </div>
        </div>
      ) : null}

      {/*
        0012b — yayın durumu eylemleri. Formun ÜSTÜNDE ve ayrı bir bölüm: durum geçişi
        bir kaydetme değil, kendi başına bir karardır ve formun "değişiklikleri kaydet"
        düğmesiyle karışmamalı.
      */}
      <ListingActionBar
        vehicleId={vehicleId}
        listingStatus={sources.detail.listingStatus}
        awaitingBranchAssignment={sources.detail.awaitingBranchAssignment}
        onApplied={() => setSourceToken((token) => token + 1)}
      />

      <Card as="div" testId="vehicle-edit-form">
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
            hint={t('vehicle.edit.field.color.hint', {
              current: sources.detail.color[locale] ?? t('vehicle.edit.field.color.empty'),
            })}
            value={values.color}
            errorKey={errorKeyFor('color')}
            onChange={(color) => setValues({ ...values, color })}
          />

          {/*
            D20/R4 — şase düzenlenemez. Alan yine de basılıyor: personel hangi aracı
            düzenlediğini görmeli ve salt okunur bir alan, olmayan bir alandan daha
            dürüst.
          */}
          <div className="admin-form__field">
            <label className="admin-form__label" htmlFor="vehicle-vin">
              {t('vehicle.form.field.vin')}
            </label>
            <p className="admin-form__hint" id="vehicle-vin-hint">
              {t('vehicle.edit.field.vin.hint')}
            </p>
            <input
              id="vehicle-vin"
              className="admin-form__control"
              dir="ltr"
              value={values.vin}
              readOnly
              aria-describedby="vehicle-vin-hint"
            />
          </div>

          <div className="admin-form__field">
            <label className="admin-form__check" htmlFor="vehicle-price-on-request">
              <input
                id="vehicle-price-on-request"
                type="checkbox"
                checked={values.priceOnRequest}
                onChange={(event) =>
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

          {/* R10 — ilan metni etkin HER dilde ayrı; bir dilde yazmamak geçerli (AC-19). */}
          <fieldset className="admin-form__group" data-testid="vehicle-listing-text">
            <legend className="admin-form__label">{t('vehicle.edit.listingText')}</legend>
            <p className="admin-form__hint">
              {missing.length === 0
                ? t('vehicle.edit.listingText.complete')
                : t('vehicle.edit.listingText.missing', {
                    locales: missing.map((code) => t(`app.locale.${code}`)).join(', '),
                  })}
            </p>

            {SUPPORTED_LOCALES.map((item) => (
              <div className="admin-form__group-item" key={item}>
                <Field
                  id={`vehicle-title-${item}`}
                  label={t('vehicle.edit.field.title', { locale: t(`app.locale.${item}`) })}
                  value={values.listingTitle[item] ?? ''}
                  errorKey={errorKeyFor('listingTitle')}
                  onChange={(text) =>
                    setValues({
                      ...values,
                      listingTitle: { ...values.listingTitle, [item]: text },
                    })
                  }
                />
                <Field
                  id={`vehicle-description-${item}`}
                  label={t('vehicle.edit.field.description', { locale: t(`app.locale.${item}`) })}
                  value={values.listingDescription[item] ?? ''}
                  errorKey={errorKeyFor('listingDescription')}
                  onChange={(text) =>
                    setValues({
                      ...values,
                      listingDescription: { ...values.listingDescription, [item]: text },
                    })
                  }
                />
              </div>
            ))}
          </fieldset>

          {/* R7 — yalnız AÇIK alanlar basılır; kapalı alan bu listede hiç gelmez. */}
          {sources.fields.length > 0 ? (
            <fieldset className="admin-form__group" data-testid="vehicle-optional-fields">
              <legend className="admin-form__label">{t('vehicle.edit.optionalFields')}</legend>

              {sources.fields.map((field) => (
                <OptionalFieldInput
                  key={field.fieldKey}
                  field={field}
                  value={values.optionalValues[field.fieldKey] ?? ''}
                  localizedValue={values.optionalLocalizedValues[field.fieldKey] ?? {}}
                  errorKey={errorKeyFor(field.fieldKey)}
                  onChange={(value) =>
                    setValues({
                      ...values,
                      optionalValues: { ...values.optionalValues, [field.fieldKey]: value },
                    })
                  }
                  onLocalizedChange={(item, value) =>
                    setValues({
                      ...values,
                      optionalLocalizedValues: {
                        ...values.optionalLocalizedValues,
                        [field.fieldKey]: {
                          ...(values.optionalLocalizedValues[field.fieldKey] ?? {}),
                          [item]: value,
                        },
                      },
                    })
                  }
                />
              ))}
            </fieldset>
          ) : null}

          <Button type="submit" loading={submitting} data-testid="vehicle-edit-submit">
            {submitting ? t('vehicle.edit.submitting') : t('vehicle.edit.submit')}
          </Button>
        </form>
      </Card>
    </div>
  )
}
