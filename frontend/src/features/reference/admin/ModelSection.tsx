import { useState } from 'react'
import { useLocale } from '../../../app/localeContext'
import {
  createModel,
  runModelAction,
  saveModel,
  type AdminModel,
} from '../../../shared/api/referenceAdmin'
import { Badge, Button, EmptyState } from '../../../shared/ui'
import { DisplayNameFields } from './DisplayNameFields'
import type { BannerState } from './ReferenceBanner'
import { ReferenceField } from './ReferenceField'
import { displayNamesOf, displayNamesPayload, rejectionOf } from './referenceForm'

interface Props {
  readonly brandId: string
  readonly models: readonly AdminModel[]
  readonly locales: readonly string[]
  readonly onChanged: () => void
  readonly onRejected: (banner: BannerState) => void
}

/**
 * `0021` R2, R5, R6 — markanın modelleri: aktif ve pasif hepsi, durumlarıyla. Yeni model bu
 * markanın altına eklenir (AC-17). Düzenleme formunda marka seçimi YOKTUR: modelin markası
 * değiştirilemez (D14, AC-20) ve form bunu söyler.
 */
export function ModelSection({ brandId, models, locales, onChanged, onRejected }: Props) {
  const { locale, t } = useLocale()
  const [name, setName] = useState('')
  const [nameError, setNameError] = useState<string | undefined>()
  const [busy, setBusy] = useState(false)
  const [editing, setEditing] = useState<string | null>(null)

  async function add() {
    setBusy(true)
    setNameError(undefined)

    try {
      await createModel(brandId, { canonicalName: name }, locale)
      setName('')
      onChanged()
    } catch (cause) {
      const rejection = rejectionOf(cause, locale)
      if (rejection.kind === 'field') {
        setNameError(rejection.messageKey)
      } else {
        onRejected(rejection)
      }
    } finally {
      setBusy(false)
    }
  }

  async function toggle(model: AdminModel) {
    try {
      await runModelAction(
        model.id,
        model.version,
        model.isActive ? 'deactivate' : 'activate',
        locale,
      )
      onChanged()
    } catch (cause) {
      const rejection = rejectionOf(cause, locale)
      onRejected(
        rejection.kind === 'field'
          ? { kind: 'failed', messageKey: rejection.messageKey }
          : rejection,
      )
    }
  }

  return (
    <section className="reference-models" data-testid="models-section">
      <h2 className="admin-page__subtitle">{t('reference.models.title')}</h2>

      {models.length === 0 ? (
        <EmptyState
          testId="models-empty"
          title={t('reference.models.empty.title')}
          description={t('reference.models.empty.body')}
        />
      ) : (
        <ul className="reference-models__list">
          {models.map((model) =>
            editing === model.id ? (
              <li key={model.id} className="reference-models__row" data-testid="model-row">
                <ModelEditor
                  model={model}
                  locales={locales}
                  onCancel={() => setEditing(null)}
                  onSaved={() => {
                    setEditing(null)
                    onChanged()
                  }}
                  onRejected={onRejected}
                />
              </li>
            ) : (
              <li key={model.id} className="reference-models__row" data-testid="model-row">
                <span className="reference-models__name" dir="ltr">
                  {model.canonicalName}
                </span>
                <Badge>
                  {t(model.isActive ? 'reference.status.active' : 'reference.status.inactive')}
                </Badge>
                <Button
                  variant="text"
                  data-testid="model-edit"
                  onClick={() => setEditing(model.id)}
                >
                  {t('reference.model.edit')}
                </Button>
                <Button
                  variant="text"
                  data-testid="model-toggle-active"
                  onClick={() => void toggle(model)}
                >
                  {t(model.isActive ? 'reference.action.deactivate' : 'reference.action.activate')}
                </Button>
              </li>
            ),
          )}
        </ul>
      )}

      <form
        className="admin-form reference-new"
        noValidate
        data-testid="model-new-form"
        onSubmit={(event) => {
          event.preventDefault()
          void add()
        }}
      >
        <h3 className="admin-form__label">{t('reference.models.new.title')}</h3>
        <ReferenceField
          id="model-new-name"
          label={t('reference.field.canonicalName')}
          hint={t('reference.field.modelCanonicalNameHint')}
          messageKey={nameError}
          value={name}
          dir="ltr"
          onChange={setName}
        />
        <div className="admin-form__actions">
          <Button type="submit" loading={busy} data-testid="model-new-submit">
            {t('reference.models.new.submit')}
          </Button>
        </div>
      </form>
    </section>
  )
}

function ModelEditor({
  model,
  locales,
  onCancel,
  onSaved,
  onRejected,
}: {
  readonly model: AdminModel
  readonly locales: readonly string[]
  readonly onCancel: () => void
  readonly onSaved: () => void
  readonly onRejected: (banner: BannerState) => void
}) {
  const { locale, t } = useLocale()
  const [canonicalName, setCanonicalName] = useState(model.canonicalName)
  const [displayNames, setDisplayNames] = useState(displayNamesOf(model.displayNames, locales))
  const [errors, setErrors] = useState<Readonly<Record<string, string>>>({})
  const [busy, setBusy] = useState(false)

  async function save() {
    setBusy(true)
    setErrors({})

    try {
      await saveModel(
        model.id,
        model.version,
        { canonicalName, displayNames: displayNamesPayload(displayNames, locales) },
        locale,
      )
      onSaved()
    } catch (cause) {
      const rejection = rejectionOf(cause, locale)
      if (rejection.kind === 'field') {
        setErrors({ [rejection.field]: rejection.messageKey })
      } else {
        onRejected(rejection)
      }
    } finally {
      setBusy(false)
    }
  }

  return (
    <form
      className="admin-form"
      noValidate
      data-testid="model-form"
      onSubmit={(event) => {
        event.preventDefault()
        void save()
      }}
    >
      <p className="admin-form__hint" data-testid="model-brand-fixed">
        {t('reference.model.brandFixed')}
      </p>
      <ReferenceField
        id={`model-name-${model.id}`}
        label={t('reference.field.canonicalName')}
        hint={t('reference.field.modelCanonicalNameHint')}
        messageKey={errors['canonicalName']}
        value={canonicalName}
        dir="ltr"
        onChange={setCanonicalName}
      />
      <DisplayNameFields
        idPrefix={`model-display-name-${model.id}`}
        locales={locales}
        values={displayNames}
        errors={errors}
        onChange={setDisplayNames}
      />
      <div className="admin-form__actions">
        <Button type="submit" loading={busy} data-testid="model-save">
          {t('reference.model.save')}
        </Button>
        <Button variant="text" onClick={onCancel}>
          {t('reference.model.cancel')}
        </Button>
      </div>
    </form>
  )
}
