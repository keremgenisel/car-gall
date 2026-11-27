import { useEffect, useState } from 'react'
import { useLocale } from '../../../app/localeContext'
import { ApiError } from '../../../shared/api/client'
import {
  fetchAdminBranch,
  openBranch,
  runBranchAction,
  saveBranch,
  type AdminBranch,
  type BranchAction,
  type BranchInput,
} from '../../../shared/api/dealershipAdmin'
import { enabledLocales } from '../../../shared/lib/locale'
import { adminPath } from '../../../shared/lib/route'
import { Badge, Button, Dialog, EmptyState, ErrorState, Skeleton } from '../../../shared/ui'
import { DealershipField } from './DealershipField'
import { OpeningHoursFieldset } from './OpeningHoursFieldset'
import { scheduleOf, schedulePayload, type Schedule } from './openingHoursForm'
import { SpecialHoursFieldset } from './SpecialHoursFieldset'
import { entriesOf, specialPayload, type SpecialEntry } from './specialHoursForm'
import {
  actionErrorKey,
  fieldErrorsOf,
  isConflict,
  localizedPayload,
  parseCoordinate,
  statusKey,
  timeZoneOptions,
} from './dealershipForm'

type Load =
  | { readonly kind: 'loading' }
  | { readonly kind: 'missing' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly branch: AdminBranch | null }

type Banner =
  | { readonly kind: 'none' }
  | { readonly kind: 'saved' }
  | { readonly kind: 'conflict' }
  | { readonly kind: 'failed'; readonly messageKey: string }

interface Values {
  readonly names: Readonly<Record<string, string>>
  readonly addresses: Readonly<Record<string, string>>
  readonly phone: string
  readonly whatsApp: string
  readonly email: string
  readonly timeZone: string
  readonly slug: string
  readonly latitude: string
  readonly longitude: string
  readonly openingHours: Schedule
  readonly specialHours: readonly SpecialEntry[]
}

function valuesOf(branch: AdminBranch | null, locales: readonly string[]): Values {
  return {
    names: Object.fromEntries(locales.map((code) => [code, branch?.names[code] ?? ''])),
    addresses: Object.fromEntries(locales.map((code) => [code, branch?.addresses[code] ?? ''])),
    phone: branch?.phone ?? '',
    whatsApp: branch?.whatsApp ?? '',
    email: branch?.email ?? '',
    timeZone: branch?.timeZone ?? '',
    slug: branch?.slug ?? '',
    latitude: branch?.latitude === null || branch === null ? '' : String(branch.latitude),
    longitude: branch?.longitude === null || branch === null ? '' : String(branch.longitude),
    // `0025` AC-2 — programsız şube yedi gün "Kapalı".
    openingHours: scheduleOf(branch?.openingHours ?? null),
    // `0027` AC-9 — `from` sırasıyla; panel okuması güncel ve gelecek bütün kayıtları taşır.
    specialHours: entriesOf(branch?.specialHours),
  }
}

function sameValues(left: Values, right: Values): boolean {
  return JSON.stringify(left) === JSON.stringify(right)
}

/**
 * `0020` R9–R13, R16, R18 — bir şubenin açılışı (<c>branchId</c> yok) ya da düzenlemesi ve
 * durum eylemleri.
 *
 * - Zorunlular `0003` R2'ninkiler; opsiyoneller boşaltılabilir (D9). Saat dilimi bir listedir
 *   (D11); kısaltma elle girilir (D12). Telefon, e-posta, kısaltma ve koordinat Arapça panelde
 *   de soldan sağa yazılır (AC-73).
 * - Kapatılmış şube SALT OKUNURDUR: alanlar devre dışı, eylem yok (AC-42, AC-54).
 * - Kalıcı kapatma iki sonucu söyleyen bir onay ister (D18, AC-47).
 * - Eski sürüm reddinde değerler ekranda kalır ve güncel hâli yükleme yolu sunulur (AC-63);
 *   sunucu hatasında da değerler kaybolmaz (AC-71).
 * - Form kaydedilmemiş değişiklik taşırken durum eylemleri devre dışıdır: eylemin ardından
 *   gelen yeniden yükleme yazılanı ezerdi (`0017` triyaj BULGU-1'in dersi).
 */
export function BranchEditPage({ branchId }: { readonly branchId?: string }) {
  const { locale, t } = useLocale()
  const locales = enabledLocales()
  const creating = branchId === undefined
  const [load, setLoad] = useState<Load>(
    creating ? { kind: 'ready', branch: null } : { kind: 'loading' },
  )
  const [values, setValues] = useState<Values>(() => valuesOf(null, locales))
  const [errors, setErrors] = useState<Readonly<Record<string, string>>>({})
  const [banner, setBanner] = useState<Banner>({ kind: 'none' })
  const [busy, setBusy] = useState(false)
  const [confirming, setConfirming] = useState(false)
  const [reload, setReload] = useState(0)

  useEffect(() => {
    if (branchId === undefined) {
      return undefined
    }

    const controller = new AbortController()

    fetchAdminBranch(branchId, locale, controller.signal)
      .then((branch) => {
        setLoad({ kind: 'ready', branch })
        setValues(valuesOf(branch, enabledLocales()))
      })
      .catch((cause: unknown) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') {
          return
        }

        setLoad(
          cause instanceof ApiError && cause.status === 404
            ? { kind: 'missing' }
            : { kind: 'error' },
        )
      })

    return () => controller.abort()
  }, [branchId, locale, reload])

  const title = t(creating ? 'dealership.branch.newTitle' : 'dealership.branch.editTitle')
  const back = (
    <a className="ui-button ui-button--text" href={adminPath('branches')}>
      {t('dealership.branch.back')}
    </a>
  )

  if (load.kind === 'loading') {
    return (
      <div className="admin-page admin-page--narrow" data-testid="branch-loading" role="status">
        <h1 className="admin-page__title">{title}</h1>
        <span className="visually-hidden">{t('dealership.loading')}</span>
        <Skeleton shape="line" />
        <Skeleton shape="line" />
        <Skeleton shape="line" />
      </div>
    )
  }

  if (load.kind === 'missing') {
    return (
      <div className="admin-page admin-page--narrow">
        <h1 className="admin-page__title">{title}</h1>
        <EmptyState
          testId="branch-missing"
          title={t('dealership.branch.missing.title')}
          description={t('dealership.branch.missing.body')}
          action={back}
        />
      </div>
    )
  }

  if (load.kind === 'error') {
    return (
      <div className="admin-page admin-page--narrow">
        <h1 className="admin-page__title">{title}</h1>
        <ErrorState
          testId="branch-error"
          title={t('dealership.error.load.title')}
          description={t('dealership.error.load.body')}
          retryLabel={t('dealership.error.load.retry')}
          onRetry={() => {
            setLoad({ kind: 'loading' })
            setReload((value) => value + 1)
          }}
        />
      </div>
    )
  }

  const { branch } = load
  const closed = branch?.status === 'Closed'
  const dirty = branch !== null && !sameValues(values, valuesOf(branch, locales))
  const zones = timeZoneOptions(values.timeZone)

  function payload(): BranchInput | null {
    const latitude = parseCoordinate(values.latitude)
    const longitude = parseCoordinate(values.longitude)

    if (latitude === undefined || longitude === undefined) {
      setErrors({ coordinate: 'coordinate.out_of_range' })
      return null
    }

    return {
      names: localizedPayload(values.names),
      addresses: localizedPayload(values.addresses),
      phone: values.phone,
      whatsApp: values.whatsApp,
      email: values.email,
      timeZone: values.timeZone,
      slug: values.slug,
      latitude,
      longitude,
      openingHours: schedulePayload(values.openingHours),
      specialHours: specialPayload(values.specialHours),
    }
  }

  async function save() {
    setErrors({})
    setBanner({ kind: 'none' })
    const input = payload()

    if (input === null) {
      return
    }

    setBusy(true)

    try {
      if (branch === null) {
        await openBranch(input, locale)
        // AC-21 — yeni şube listede AKTİF olarak görünür.
        window.location.assign(adminPath('branches'))
        return
      }

      await saveBranch(branch.branchId, branch.version, input, locale)
      setBanner({ kind: 'saved' })
      setReload((value) => value + 1)
    } catch (cause) {
      fail(cause)
    } finally {
      setBusy(false)
    }
  }

  async function act(action: BranchAction) {
    if (branch === null) {
      return
    }

    setBusy(true)
    setErrors({})
    setBanner({ kind: 'none' })

    try {
      await runBranchAction(branch.branchId, branch.version, action, locale)
      setBanner({ kind: 'saved' })
      setReload((value) => value + 1)
    } catch (cause) {
      fail(cause)
    } finally {
      setBusy(false)
    }
  }

  function fail(cause: unknown) {
    if (isConflict(cause)) {
      // R16 — uyarı + güncel hâli yükleme yolu; değerler ekranda kalır (AC-63).
      setBanner({ kind: 'conflict' })
      return
    }

    const fields = fieldErrorsOf(cause)
    setErrors(fields)

    if (Object.keys(fields).length === 0) {
      // AC-50 — son aktif şubenin reddi NEDENİYLE söylenir; sunucu hatası genel mesajla.
      setBanner({ kind: 'failed', messageKey: actionErrorKey(cause, locale) })
    }
  }

  function text(field: 'names' | 'addresses', code: string, multiline: boolean) {
    const errorField = `${field === 'names' ? 'name' : 'address'}.${code}`
    const id = `branch-${field === 'names' ? 'name' : 'address'}-${code}`

    return (
      <DealershipField key={id} id={id} label={t(`app.locale.${code}`)} error={errors[errorField]}>
        {(props) =>
          multiline ? (
            <textarea
              {...props}
              className="admin-form__control"
              dir="auto"
              lang={code}
              rows={2}
              value={values[field][code] ?? ''}
              onChange={(event) =>
                setValues({ ...values, [field]: { ...values[field], [code]: event.target.value } })
              }
            />
          ) : (
            <input
              {...props}
              className="admin-form__control"
              dir="auto"
              lang={code}
              value={values[field][code] ?? ''}
              onChange={(event) =>
                setValues({ ...values, [field]: { ...values[field], [code]: event.target.value } })
              }
            />
          )
        }
      </DealershipField>
    )
  }

  function ltrInput(
    key: 'phone' | 'whatsApp' | 'email' | 'slug' | 'latitude' | 'longitude',
    id: string,
    label: string,
    error: string | undefined,
    hint?: string,
  ) {
    return (
      <DealershipField id={id} label={label} hint={hint} error={error}>
        {(props) => (
          <input
            {...props}
            className="admin-form__control"
            dir="ltr"
            type={
              key === 'email' ? 'email' : key === 'phone' || key === 'whatsApp' ? 'tel' : 'text'
            }
            inputMode={key === 'latitude' || key === 'longitude' ? 'decimal' : undefined}
            value={values[key]}
            onChange={(event) => setValues({ ...values, [key]: event.target.value })}
          />
        )}
      </DealershipField>
    )
  }

  return (
    <div className="admin-page admin-page--narrow" data-testid="branch-edit">
      <div className="admin-page__header">
        <h1 className="admin-page__title">{title}</h1>
        {back}
      </div>

      {branch === null ? null : (
        <div className="dealership-status" data-testid="branch-status">
          <span>{t('dealership.branch.status')}</span>
          <Badge>{t(statusKey(branch.status))}</Badge>
        </div>
      )}

      {closed ? (
        <p className="admin-form__hint" data-testid="branch-closed-note">
          {t('dealership.branch.closedNote')}
        </p>
      ) : null}

      <BranchBanner
        banner={banner}
        onReload={() => {
          setBanner({ kind: 'none' })
          setErrors({})
          setReload((value) => value + 1)
        }}
      />

      <form
        className="admin-form"
        noValidate
        onSubmit={(event) => {
          event.preventDefault()
          void save()
        }}
      >
        <fieldset className="dealership-fieldset" disabled={closed} data-testid="branch-fields">
          <fieldset className="admin-form__group">
            <legend className="admin-form__label">{t('dealership.branch.field.name')}</legend>
            <p className="admin-form__hint">{t('dealership.branch.field.localizedHint')}</p>
            {locales.map((code) => text('names', code, false))}
          </fieldset>

          <fieldset className="admin-form__group">
            <legend className="admin-form__label">{t('dealership.branch.field.address')}</legend>
            {locales.map((code) => text('addresses', code, true))}
          </fieldset>

          {ltrInput(
            'phone',
            'branch-phone',
            t('dealership.branch.field.phone'),
            errors['phone'],
            t('dealership.branch.field.phoneHint'),
          )}
          {ltrInput(
            'whatsApp',
            'branch-whatsapp',
            t('dealership.branch.field.whatsApp'),
            errors['whatsApp'],
            t('dealership.branch.field.phoneHint'),
          )}
          {ltrInput('email', 'branch-email', t('dealership.branch.field.email'), errors['email'])}

          <DealershipField
            id="branch-time-zone"
            label={t('dealership.branch.field.timeZone')}
            error={errors['timeZone']}
          >
            {(props) => (
              <select
                {...props}
                className="admin-form__control"
                dir="ltr"
                value={values.timeZone}
                onChange={(event) => setValues({ ...values, timeZone: event.target.value })}
              >
                <option value="">{t('dealership.branch.field.timeZoneChoose')}</option>
                {zones.map((zone) => (
                  <option key={zone} value={zone}>
                    {zone}
                  </option>
                ))}
              </select>
            )}
          </DealershipField>

          {ltrInput(
            'slug',
            'branch-slug',
            t('dealership.branch.field.slug'),
            errors['slug'],
            t('dealership.branch.field.slugHint'),
          )}

          <fieldset className="admin-form__group">
            <legend className="admin-form__label">{t('dealership.branch.field.coordinate')}</legend>
            {ltrInput(
              'latitude',
              'branch-latitude',
              t('dealership.branch.field.latitude'),
              errors['coordinate'],
            )}
            {ltrInput(
              'longitude',
              'branch-longitude',
              t('dealership.branch.field.longitude'),
              undefined,
            )}
          </fieldset>
        </fieldset>

        <OpeningHoursFieldset
          schedule={values.openingHours}
          errors={errors}
          disabled={closed}
          onChange={(openingHours) => setValues({ ...values, openingHours })}
        />

        <SpecialHoursFieldset
          entries={values.specialHours}
          errors={errors}
          disabled={closed}
          onChange={(specialHours) => setValues({ ...values, specialHours })}
        />

        {closed ? null : (
          <div className="admin-form__actions">
            <Button type="submit" loading={busy} data-testid="branch-save">
              {t(creating ? 'dealership.branch.open' : 'dealership.action.save')}
            </Button>
          </div>
        )}
      </form>

      {branch === null || closed ? null : (
        <section
          className="dealership-actions"
          aria-labelledby="branch-actions-title"
          data-testid="branch-actions"
        >
          <h2 className="dealership-actions__title" id="branch-actions-title">
            {t('dealership.branch.actions.title')}
          </h2>
          {dirty ? (
            <p className="admin-form__hint" id="branch-save-first" data-testid="branch-save-first">
              {t('dealership.branch.actions.saveFirst')}
            </p>
          ) : null}
          <div className="admin-form__actions">
            {branch.status === 'Active' ? (
              <Button
                variant="secondary"
                disabled={busy || dirty}
                aria-describedby={dirty ? 'branch-save-first' : undefined}
                data-testid="branch-close-temporarily"
                onClick={() => void act('close-temporarily')}
              >
                {t('dealership.branch.actions.closeTemporarily')}
              </Button>
            ) : null}
            {branch.status === 'TemporarilyClosed' ? (
              <Button
                variant="secondary"
                disabled={busy || dirty}
                aria-describedby={dirty ? 'branch-save-first' : undefined}
                data-testid="branch-reopen"
                onClick={() => void act('reopen')}
              >
                {t('dealership.branch.actions.reopen')}
              </Button>
            ) : null}
            <Button
              variant="text"
              disabled={busy || dirty}
              aria-describedby={dirty ? 'branch-save-first' : undefined}
              data-testid="branch-close"
              onClick={() => setConfirming(true)}
            >
              {t('dealership.branch.actions.close')}
            </Button>
          </div>
        </section>
      )}

      {/* D8 · D18 — kalıcı kapatma iki sonucu söyleyen bir onayla; vazgeçmek hiçbir şey yapmaz (AC-47). */}
      <Dialog
        open={confirming}
        onClose={() => setConfirming(false)}
        label={t('dealership.branch.close.title')}
        testId="branch-close-dialog"
      >
        <div className="content-dialog">
          <h2 className="content-dialog__title">{t('dealership.branch.close.title')}</h2>
          <p data-testid="branch-close-body">{t('dealership.branch.close.body')}</p>
          <div className="admin-form__actions">
            <Button
              variant="secondary"
              data-testid="branch-close-cancel"
              onClick={() => setConfirming(false)}
            >
              {t('dealership.branch.close.cancel')}
            </Button>
            <Button
              data-testid="branch-close-confirm"
              onClick={() => {
                setConfirming(false)
                void act('close')
              }}
            >
              {t('dealership.branch.close.confirm')}
            </Button>
          </div>
        </div>
      </Dialog>
    </div>
  )
}

function BranchBanner({
  banner,
  onReload,
}: {
  readonly banner: Banner
  readonly onReload: () => void
}) {
  const { t } = useLocale()

  if (banner.kind === 'conflict') {
    return (
      <div className="admin-form__error" role="alert" data-testid="dealership-conflict">
        <p className="admin-form__error-body">{t('dealership.conflict.body')}</p>
        <div className="admin-form__banner-action">
          <Button variant="secondary" data-testid="dealership-conflict-reload" onClick={onReload}>
            {t('dealership.conflict.reload')}
          </Button>
        </div>
      </div>
    )
  }

  if (banner.kind === 'failed') {
    return (
      <div className="admin-form__error" role="alert" data-testid="dealership-save-failed">
        <p className="admin-form__error-body">{t(banner.messageKey)}</p>
      </div>
    )
  }

  if (banner.kind === 'saved') {
    return (
      <div className="admin-form__notice" role="status" data-testid="dealership-saved">
        <p className="admin-form__notice-body">{t('dealership.saved')}</p>
      </div>
    )
  }

  return null
}
