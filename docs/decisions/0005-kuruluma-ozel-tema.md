# 0005 — Kuruluma özel tema: yapılandırmadan gelen renk, kapıda duran kontrast

- **Durum:** Kabul edildi
- **Tarih:** 2026-10-04
- **Bağlam:** spec frontend-002 — Görsel Dil ve Kuruluma Özel Tema (D1, D3, D9, D10–D13)

## Bağlam

Ürün **white-label**: `AGENTS.md` kurulum başına tek galeri diyor. Arayüzün bugünkü
renkleri ise hiç kimsenin verdiği bir karar değildi — `frontend-001` token mekanizmasını
doğru kurdu ama **hangi değerler** olacağını hiçbir spec söylemedi. Vurgu rengi olarak
duran `#1f5fd0`, `docs/frontend.md` § 4.1'de *yasak örneği* olarak yazılı hex'in aynısıydı:
dokümandaki örnek, token dosyasına galeri kimliği kararı gibi yerleşmişti.

Asıl gereksinim bir palet seçmek değil, **rengin kurulum başına değişebilmesiydi.** Bu üç
soruyu açtı: renk nereden gelir, yanlış renk ne olur, ve tipografi de değişir mi?

## Karar

**Renk dağıtım yapılandırmasından gelir ve ya tam ya hiç verilir. Kontrast eşikleri
uygulama başlangıcında doğrulanır; karşılanmazsa uygulama ayağa kalkmaz. Tipografi V1'de
yapılandırılamaz. Tema, mevcut `index.html` enjeksiyon yolundan `<html>` etiketine
basılır.**

## Gerekçe

### Renk neden yapılandırmada, panelde değil (D1)

Bu, projenin zaten kurduğu desen: varsayılan dil *"kurulum yapılandırmasından"*, para
birimi *"dağıtım yapılandırmasında sabit ve panelden düzenlenemez"*. Renk de aynı sınıfa
girer — galeri başına bir kez verilen, seyrek değişen bir dağıtım kararı.

Panelden renk seçici, kontrast garantisini galeriye devreder ve erişilebilirlik tabanını
(`docs/frontend.md` § 7) bir renk seçicinin arkasına koyar.

*Elenen alternatif:* renkleri galeri profiline (veriye) koymak. O zaman kontrast kapısı
**çalışma anında**, galeri "kaydet"e bastığında devreye girmek zorunda kalır ve
"kaydedemiyorum" sorunu galeri çalışanının önüne düşer.

*Bilerek ödenen bedel:* galeri rengini kendi değiştiremez; değişiklik bir yapılandırma ve
bir yeniden dağıtım ister.

### Neden "ya tam ya hiç" (R6)

Kısmi temaya izin vermek, host'un karşılaştırma için **varsayılanları da bilmesini**
gerektirirdi — yani paletin iki yerde yaşamasını. Varsayılanlar `tokens.css`'te yaşıyor ve
bir kural yalnızca bir dosyada yaşar.

Sonuç dört ayrı durum ve yalnız birincisi sessiz: **hiç** tema verilmemesi varsayılana
düşer; **kısmi** tema, **tanınmayan anahtar veya biçime uymayan değer**, ve **kontrastı
yetersiz** tema uygulamayı durdurur. Bunları birleştirmek, yanlış kontrastlı bir kurulumun
sessizce açılmasına yol açardı.

### Neden ayağa kalkmıyor (D3)

Erişilebilirlik tabanının bir yapılandırma hatasıyla sessizce kırılması, kırıldığının fark
edilmemesi demektir — ve **renk hatası gözle yıllarca fark edilmez**. Açılan ama okunamayan
bir vitrin, açılmayan bir vitrinden kötüdür.

*Elenen alternatif:* uyarı loglayıp devam etmek — logu kimse okumaz.

**Dağıtım kaydı:** yapılandırma doğrulaması dağıtım kesilmeden önce koşar ve başarısız bir
örnek trafik almaz. Kademeli dağıtımda yeni örnek açılmaz, eski örnek çalışmaya devam
eder. Doğrudan değiştirilen bir dağıtımda ise aynı karar çalışan vitrini indirir; bu yüzden
kesme öncesi doğrulama kararın ayrılmaz parçasıdır.

### Neden iki kontrast kapısı (R7, D11)

R6 host'un varsayılanları bilmemesini doğru nedenle şart koşuyor — ama sonucu, tema
verilmediğinde çalışan paletin runtime kapısına **hiç girmemesidir**. Tek kapı varsayılanı
kapsamaz.

İkinci kapı CI'da koşan bir testtir ve `tokens.css`'i okur. R6 ihlal edilmiyor çünkü
okuyan şey **uygulama değil test**; bir testin spec'i bilmesi zaten testin işidir. Eşik
tablosu yine tek yerde (C12): iki kapı da aynı çift listesini okuyor.

**Çiftler CSS'ten türetildi, spec'ten değil.** İlk taslak beş çift sayıyordu ve üçü
eksikti; biri — yardımcı metin rengi — hem on yerde kullanılıyor hem yapılandırılabilirdi.
Bir kurulum onu açık griye ayarlasa uygulama açılır, kapı yeşil kalır ve bütün yardımcı
metinler okunmaz olurdu.

Bunun mekanik sonucu **D13**: kontrast çiftine giren her token yapılandırılabilir olmak
zorunda, çünkü kapı bir çifti ancak **iki ucunu da biliyorsa** denetleyebilir.

### Neden tipografi yapılandırılamıyor (D9)

Yapılandırma bir aile **adı** verebilir ama **dosyasını** veremez — yazı tipleri kendi
kaynağımızdan servis ediliyor (D8). Dosyası olmayan bir aile adında tarayıcı sessizce başka
fonta düşer ve bozulma yalnız Arapça sayfada, gözle görülür. Tipografiyi yapılandırmaya
açmak font dosyası dağıtımını da açmayı gerektirir.

### Neden `<html style>` (D10)

C4 ikinci bir mekanizma yasaklıyor ve `IndexHtmlRenderer` `<html lang="" dir="">` yer
tutucusunu zaten değiştiriyor; `style` özniteliği **aynı** noktadır.

İkinci neden ölçüldü: Vite, CSS `<link>`'ini build sırasında `<head>`'e kendisi ekliyor,
yani `<html>` etiketinden **sonra**. Oraya konan bir `<style>:root{…}` bloğu link'ten önce
düşer ve `tokens.css` temayı ezer — aynı özgüllükte son bildirim kazanır ve bu **sessizce**
olur. Inline `style` özniteliği `:root` kuralını her zaman yener, sıraya hiç bakmaz.

Kaçış sorunu C11'in biçim kısıtıyla kapanıyor: altı haneli hex kalıbından geçmeyen bir
değer enjekte edilmiyor, yani tırnak veya açılı parantez taşıyan bir değer oraya gelemiyor.

## Bedel

- **Tema veren bir dağıtım on bir renk anahtarı vermek zorunda** (D13). R6 "ya tam ya hiç"
  derken bu bedeli zaten seçmişti; D13 onu büyüttü, türünü değiştirmedi.
- **Yanlış renk verilen bir kurulum açılmaz.** Bilinçli.
- **Beyaz etiketlemenin tipografi yarısı bir sonraki spec'e kalıyor.**
- **`--color-border` kontrast kapısının dışında.** Token hem dekoratif kart kenarlığı hem
  form denetimi sınırı olarak kullanılıyor ve kapı ikisini ayırt edemez; WCAG 1.4.11 3:1'i
  yalnız denetimi tanımlayan sınır için istiyor. Ölçüldü: bugünkü değer beyaza karşı
  1.36:1, yani blanket bir 3:1 kuralı mevcut tasarımı anında reddederdi.

## Alternatifler

- **Kurulum başına ayrı derleme çıktısı.** Elendi: tema çalışma anında enjekte ediliyor;
  kurulum başına build, dağıtım sayısını ve CI süresini çarpar.
- **Koyu mod / tema değiştirici.** Kapsam dışı. Token'lar anlamsal adlarla kurulduğu için
  sonradan ucuzlar.
- **Eşik tablosunu frontend tarafında (TS) tutmak.** Elendi: başlangıç kapısı host'ta
  yaşamak zorunda (C4) ve iki kapı iki dilde yazılırsa eşik iki yerde yaşar (C12).
