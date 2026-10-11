import { useRef, useState } from 'react'
import { useLocale } from '../../../app/localeContext'
import { ApiError } from '../../../shared/api/client'
import { uploadMediaAsset } from '../../../shared/api/mediaAdmin'
import { Button } from '../../../shared/ui'
import { mediaMessageKey } from './vehicleMediaState'

type Upload =
  | { readonly kind: 'idle' }
  | { readonly kind: 'uploading' }
  | { readonly kind: 'failed'; readonly messageKey: string }

/**
 * 0012a **D9** · AC-1 — panelden **tek** dosya yükleme kontrolü.
 *
 * <p>Ölçüldü ve karar bu yüzden verildi: bu dilimden önce `frontend/src` içinde
 * `input type="file"` hiç yoktu, yani R1'in <i>"yüklenmiş bir fotoğrafı bağlar"</i> ön
 * koşulu panelden hiçbir yoldan sağlanamıyordu.</p>
 *
 * <p><b>Çoklu seçim ve sürükle-bırak YOK</b> (X3): <code>multiple</code> niteliği bilerek
 * basılmıyor. Tek dosya yolu doğrulanmadan çoklu akış hata durumlarını katlar — bir
 * dosya 413, biri 415, üçüncüsü başarılı olduğunda ekranın söyleyeceği şey artık tek bir
 * mesaj değildir.</p>
 *
 * <p><code>413</code> ve <code>415</code> AYRI mesaj alıyor (`0011` AC-3/AC-6, `docs/
 * conventions.md` § 5): personelin yapacağı şey farklı — biri BAŞKA bir dosya seçmek,
 * diğeri AYNI dosyayı küçültmek.</p>
 */
export function MediaUploadField({
  onUploaded,
}: {
  readonly onUploaded: (mediaAssetId: string) => void
}) {
  const { locale, t } = useLocale()
  const input = useRef<HTMLInputElement>(null)
  const [file, setFile] = useState<File | null>(null)
  const [upload, setUpload] = useState<Upload>({ kind: 'idle' })

  async function send() {
    if (file === null) {
      return
    }

    setUpload({ kind: 'uploading' })

    try {
      const uploaded = await uploadMediaAsset(file, locale)

      setFile(null)

      // Kontrolün kendi değeri DOM'da duruyor; aynı dosyayı ikinci kez seçmek
      // `change` olayını tetiklemediği için elle temizlenmek zorunda.
      if (input.current !== null) {
        input.current.value = ''
      }

      setUpload({ kind: 'idle' })
      onUploaded(uploaded.id)
    } catch (cause) {
      const errorCode = cause instanceof ApiError ? cause.errorCode : 'api.unavailable'

      setUpload({ kind: 'failed', messageKey: mediaMessageKey(errorCode, locale) })
    }
  }

  const uploading = upload.kind === 'uploading'

  return (
    <div className="vehicle-media__upload" data-testid="vehicle-media-upload">
      <label className="admin-form__label" htmlFor="vehicle-media-file">
        {t('vehicle.media.upload.label')}
      </label>
      <p className="admin-form__hint" id="vehicle-media-file-hint">
        {t('vehicle.media.upload.hint')}
      </p>
      <input
        ref={input}
        id="vehicle-media-file"
        className="admin-form__control"
        type="file"
        accept="image/jpeg,image/png,image/webp"
        aria-describedby="vehicle-media-file-hint"
        disabled={uploading}
        onChange={(event) => {
          setFile(event.target.files?.[0] ?? null)
          setUpload({ kind: 'idle' })
        }}
      />

      <Button
        loading={uploading}
        disabled={file === null}
        onClick={() => void send()}
        data-testid="vehicle-media-upload-submit"
      >
        {uploading ? t('vehicle.media.upload.sending') : t('vehicle.media.upload.submit')}
      </Button>

      {upload.kind === 'failed' ? (
        <p
          className="admin-form__field-error"
          role="alert"
          data-testid="vehicle-media-upload-error"
        >
          {t(upload.messageKey)}
        </p>
      ) : null}
    </div>
  )
}
