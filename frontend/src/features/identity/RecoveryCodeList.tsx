import { useLocale } from '../../app/localeContext'

/**
 * D31/AC-7 — kurtarma kodları **yalnız ekranda ve bir kez** gösterilir; e-posta ile
 * gönderilmez.
 *
 * *Gerekçe:* kurtarma kodunu e-postaya göndermek ikinci adımı **e-posta hesabının**
 * güvenliğine indirger ve o hesap bizim kontrolümüzde değil. *Bedel, açıkça:* personel
 * kodları kaydetmezse kaybeder — D24'ün yenilemesi bu yolu kısaltıyor.
 */
export function RecoveryCodeList({ codes }: { readonly codes: readonly string[] }) {
  const { t } = useLocale()

  return (
    <section className="two-factor__codes" data-testid="recovery-codes">
      <h2 className="admin-form__subtitle">{t('twoFactor.codes.title')}</h2>
      <p className="admin-form__intro">{t('twoFactor.codes.body')}</p>

      <ul className="two-factor__code-list">
        {codes.map((code) => (
          <li key={code} className="two-factor__code">
            <code>{code}</code>
          </li>
        ))}
      </ul>
    </section>
  )
}
