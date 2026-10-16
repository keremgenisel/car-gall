import { QRCodeSVG } from 'qrcode.react'

/**
 * D17/C15 — kare kodu **UÇ** çizer. Sunucuda üretilmiş bir görsel sırrı bir resme
 * gömer ve o resim önbellek, log ve proxy yüzeylerine taşınır (§ Bilinen tuzaklar 2);
 * burada çizilen kod ise sayfadan dışarı çıkmıyor.
 *
 * Çıktı **SVG**: ölçeklendiğinde bozulmuyor, yazdırmada keskin kalıyor ve testte
 * görünürlüğü ölçülebiliyor. Kare kodun yanında elle giriş metni **her zaman görünür**
 * duruyor (D17) — kamerayı aynı cihazda kullanan personelin tek yolu o.
 */
export function QrCode({ value, label }: { readonly value: string; readonly label: string }) {
  return (
    <div className="two-factor__qr" data-testid="two-factor-qr">
      <QRCodeSVG value={value} size={176} level="M" title={label} marginSize={1} />
    </div>
  )
}
