/**
 * CSS'te fiziksel yön özellikleri YASAKTIR; logical properties zorunludur
 * (C6, Yasak Listesi 25, docs/frontend.md § 6). Lint bunu kırar — çünkü RTL kırılması
 * gözle fark edilene kadar sessizdir.
 */
module.exports = {
  extends: ['stylelint-config-standard'],
  overrides: [
    {
      // Token'ların tanımlandığı dosya: ham değer buraya AİTTİR.
      files: ['src/shared/design/tokens.css'],
      rules: { 'declaration-property-value-disallowed-list': null },
    },
  ],
  rules: {
    'property-disallowed-list': [
      [
        'left',
        'right',
        'margin-left',
        'margin-right',
        'padding-left',
        'padding-right',
        'border-left',
        'border-right',
        'border-left-width',
        'border-right-width',
        'border-left-color',
        'border-right-color',
        'float',
        'clear',
      ],
      { message: 'Fiziksel yön özelliği yasak; logical property kullan (docs/frontend.md § 6).' },
    ],
    // docs/frontend.md § 4.1 — gömülü sabit değer yasak; değer token'dan gelir.
    // tokens.css muaf: token'ların TANIMLANDIĞI tek yer orası.
    'declaration-property-value-disallowed-list': [
      {
        '/^(?!--).*$/': ['/#[0-9a-fA-F]{3,8}\\b/', '/(?<![\\w-])[0-9]+px(?![\\w-])/'],
      },
      { message: 'Gömülü hex veya ham piksel yasak; token kullan (docs/frontend.md § 4.1).' },
    ],
    // frontend-002 C11 — renk biçimi TEK TİPTİR: altı haneli hex (alfa taşıyanlar sekiz).
    // stylelint-config-standard varsayılanı KISA biçim istiyor ve bu, C11'in tersi.
    // Kısaltmaya izin vermek, `index.html`'e enjekte edilen değerin doğrulamasını
    // ayrıştırma varsayımına bağlardı; kontrast hesabı da öyle.
    'color-hex-length': 'long',
    'custom-property-pattern': null,
    'selector-class-pattern': null,
  },
}
