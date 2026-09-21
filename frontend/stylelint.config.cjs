/**
 * CSS'te fiziksel yön özellikleri YASAKTIR; logical properties zorunludur
 * (C6, Yasak Listesi 25, docs/frontend.md § 6). Lint bunu kırar — çünkü RTL kırılması
 * gözle fark edilene kadar sessizdir.
 */
module.exports = {
  extends: ['stylelint-config-standard'],
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
    'declaration-property-value-disallowed-list': [
      { 'text-align': ['left', 'right'] },
      { message: 'text-align: start | end kullan (docs/frontend.md § 6).' },
    ],
    'custom-property-pattern': null,
    'selector-class-pattern': null,
  },
}
