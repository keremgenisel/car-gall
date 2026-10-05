import { useState } from 'react'
import { useLocale } from '../../app/localeContext'
import { ApiError } from '../../shared/api/client'
import { changeStaffRole, deactivateStaff, resetStaffPassword } from '../../shared/api/identity'
import { STAFF_ROLES, type StaffListItem, type StaffRole } from '../../shared/api/types'
import { Button, Field } from '../../shared/ui'
import { ConfirmDialog } from './ConfirmDialog'
import { passwordFormError, rejectionFor, type StaffAction } from './staffActionState'

/**
 * Bir satırın üç eylemi (0008 R1–R3).
 *
 * Eylem sonucu YUKARI bildiriliyor; liste tazeleme kararını bu bileşen vermiyor. Sebep
 * R6–R8: "tazele mi" sorusunun cevabı reddin TÜRÜNE bağlı ve o karar `staffActionState`'te
 * yaşıyor. Burada kalsaydı üç eylemde üç kez tekrarlanırdı.
 */
export interface ActionOutcome {
  readonly refresh: boolean
  readonly signedOut: boolean
  /** Başarı → `null`; red → gösterilecek çeviri anahtarı. */
  readonly messageKey: string | null
}

export function StaffRowActions({
  member,
  isSelf,
  onOutcome,
}: {
  readonly member: StaffListItem
  readonly isSelf: boolean
  readonly onOutcome: (outcome: ActionOutcome) => void
}) {
  const { locale, t } = useLocale()
  const [open, setOpen] = useState<StaffAction | null>(null)

  // R9/AC-26 — bekleyen durum SATIR BAŞINA. Tek bir global bayrak olsaydı bir satırdaki
  // işlem bütün tablonun düğmelerini kilitlerdi.
  const [pending, setPending] = useState<StaffAction | null>(null)

  const [role, setRole] = useState<StaffRole>(member.role)
  const [password, setPassword] = useState({ value: '', confirmation: '' })
  const [fieldError, setFieldError] = useState<string | null>(null)

  /**
   * Seçili rol diyalog AÇILIRKEN prop'tan tazelenir.
   *
   * Mount anında bir kez alıp bırakmak yetmiyor: satır anahtarı `staffId` olduğu için
   * tazeleme satırı yeniden MOUNT ETMİYOR, dolayısıyla state eski kalıyordu. Yönetici
   * rolü değiştirip diyaloğu ikinci kez açtığında ekranda "İçerik Editörü" görüp
   * diyalogda "Satış Danışmanı" seçili buluyordu — dokunmadan onaylamak, az önce
   * yaptığı değişikliği sessizce geri alırdı.
   *
   * Diyalog ekranın parçası ve R6 tazelemeden sonra ekranın tutarlı kalmasını istiyor.
   */
  function openRole() {
    setRole(member.role)
    setOpen('role')
  }

  function close() {
    setOpen(null)
    setFieldError(null)
    setPassword({ value: '', confirmation: '' })
  }

  async function run(action: StaffAction, call: () => Promise<unknown>) {
    if (pending !== null) {
      return
    }

    setPending(action)

    try {
      await call()

      // AC-13 — parola kaydettikten sonra ekranda TUTULMAZ.
      close()
      onOutcome({ refresh: true, signedOut: false, messageKey: null })
    } catch (cause) {
      const errorCode = cause instanceof ApiError ? cause.errorCode : 'api.unavailable'
      const status = cause instanceof ApiError ? cause.status : null
      const rejection = rejectionFor(action, errorCode, status, locale)

      close()
      onOutcome({
        refresh: rejection.refresh,
        signedOut: rejection.signedOut,
        messageKey: rejection.messageKey,
      })
    } finally {
      setPending(null)
    }
  }

  function submitPassword() {
    const error = passwordFormError(password.value, password.confirmation)

    // AC-12 — eşleşmiyorsa İSTEK GÖNDERİLMEZ.
    if (error !== null) {
      setFieldError(error)
      return
    }

    void run('password', () => resetStaffPassword(member.staffId, password.value, locale))
  }

  const busy = (action: StaffAction) => pending === action

  return (
    <>
      <div className="staff-row-actions">
        <Button
          variant="secondary"
          loading={busy('deactivate')}
          disabled={pending !== null}
          onClick={() => setOpen('deactivate')}
          data-testid={`deactivate-${member.staffId}`}
        >
          {busy('deactivate') ? t('staff.action.working') : t('staff.action.deactivate')}
        </Button>
        <Button
          variant="secondary"
          loading={busy('role')}
          disabled={pending !== null}
          onClick={openRole}
          data-testid={`role-${member.staffId}`}
        >
          {busy('role') ? t('staff.action.working') : t('staff.action.role')}
        </Button>
        <Button
          variant="secondary"
          loading={busy('password')}
          disabled={pending !== null}
          onClick={() => setOpen('password')}
          data-testid={`password-${member.staffId}`}
        >
          {busy('password') ? t('staff.action.working') : t('staff.action.password')}
        </Button>
      </div>

      {open === 'deactivate' ? (
        <ConfirmDialog
          titleKey="staff.confirm.deactivate.title"
          onCancel={close}
          testId="confirm-deactivate"
          footer={
            <Button
              loading={busy('deactivate')}
              onClick={() => void run('deactivate', () => deactivateStaff(member.staffId, locale))}
              data-testid="confirm-deactivate-submit"
            >
              {t('staff.confirm.deactivate.submit')}
            </Button>
          }
        >
          <p dir="auto">{t('staff.confirm.deactivate.body', { name: member.displayName })}</p>
          {/* AC-16 — kendi hesabını kapatan kişi geri açacak yetkiyi de kaybeder. */}
          {isSelf ? (
            <p className="admin-dialog__warning" data-testid="confirm-self-warning">
              {t('staff.confirm.deactivate.self')}
            </p>
          ) : null}
        </ConfirmDialog>
      ) : null}

      {open === 'role' ? (
        <ConfirmDialog
          titleKey="staff.confirm.role.title"
          onCancel={close}
          testId="confirm-role"
          footer={
            <Button
              loading={busy('role')}
              onClick={() => void run('role', () => changeStaffRole(member.staffId, role, locale))}
              data-testid="confirm-role-submit"
            >
              {t('staff.confirm.role.submit')}
            </Button>
          }
        >
          <p dir="auto">{t('staff.confirm.role.body', { name: member.displayName })}</p>

          <div className="admin-form__field">
            <label className="admin-form__label" htmlFor="confirm-role-select">
              {t('staff.add.field.role')}
            </label>
            <select
              id="confirm-role-select"
              className="admin-form__control"
              value={role}
              onChange={(event) => setRole(event.target.value as StaffRole)}
            >
              {STAFF_ROLES.map((value) => (
                <option key={value} value={value}>
                  {t(`staff.role.${value}`)}
                </option>
              ))}
            </select>
          </div>

          {/* AC-9 — kendi rolünü düşürmek de sonucu ağır bir işlemdir (R4). */}
          {isSelf && role !== 'manager' ? (
            <p className="admin-dialog__warning" data-testid="confirm-self-warning">
              {t('staff.confirm.role.self')}
            </p>
          ) : null}
        </ConfirmDialog>
      ) : null}

      {open === 'password' ? (
        <ConfirmDialog
          titleKey="staff.confirm.password.title"
          onCancel={close}
          testId="confirm-password"
          footer={
            <Button
              loading={busy('password')}
              onClick={submitPassword}
              data-testid="confirm-password-submit"
            >
              {t('staff.confirm.password.submit')}
            </Button>
          }
        >
          <p dir="auto">{t('staff.confirm.password.body', { name: member.displayName })}</p>

          <Field
            id="reset-password-new"
            label={t('staff.confirm.password.field.new')}
            type="password"
            autoComplete="new-password"
            value={password.value}
            errorKey={fieldError ?? undefined}
            onChange={(value) => setPassword((current) => ({ ...current, value }))}
          />

          <Field
            id="reset-password-confirm"
            label={t('staff.confirm.password.field.confirm')}
            type="password"
            autoComplete="new-password"
            value={password.confirmation}
            onChange={(confirmation) => setPassword((current) => ({ ...current, confirmation }))}
          />
        </ConfirmDialog>
      ) : null}
    </>
  )
}
