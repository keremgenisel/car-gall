## Spec
<!-- specs/<no>-<ad>.md — spec'siz PR açılmaz (docs/git.md § 1) -->
specs/

## Ne değişti
<!-- 2-4 cümle, İŞ dilinde — teknik değil. Ne yapılabilir hâle geldi? -->


## Kabul kriteri → kanıt
<!-- Her kriter için test adı veya görsel kanıt yolu. Boş satır bırakılmaz. -->

| Kriter | Kanıt |
|---|---|
| AC-1 … | `` |
| AC-2 … | `` |

## Karşılanmayan kriterler
<!-- Yoksa "yok" yaz — boş bırakma. Sessizce atlanan kriter PR'ı reddeder. -->


## Mimari etkisi
- **Dokunulan modüller:**
- **Yeni entegrasyon olayı / contract değişikliği:** <!-- yoksa "yok" -->
- **Yeni migration:** <!-- yoksa "yok"; varsa hangi şema -->

## Açık konular / öneriler
<!-- Öneri kuralı: her açık konu kendi önerisi ve gerekçesiyle gelir (AGENTS.md Altın Kural 2).
     Karar Takım Yöneticisi'nindir; onaysız öneri uygulanmaz. -->


---

### Kontrol listesi
- [ ] Pipeline tamamen yeşil (backend build + xUnit, mimari testler, migration şema kontrolü, frontend lint + build, eksik çeviri anahtarı)
- [ ] Arayüz kriterlerinin görsel kanıtı eklendi — **boş ve hata durumları dâhil**; RTL kriteri varsa Arapça görüntü de var
- [ ] Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi
- [ ] Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi
- [ ] Commit mesajları formata uyuyor: `<tip>(<kapsam>): <özet> [plan <spec-no>/<adım>]`
