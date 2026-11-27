import ar from './locales/contact/ar.json'
import en from './locales/contact/en.json'
import tr from './locales/contact/tr.json'
import { registerCatalogs } from './i18n'

/**
 * `0028` **P8** — iletişim sayfasının kataloğu: DÖRDÜNCÜ küme (ADR `0013` eki, `frontend-003` D13'ün
 * yolu). Bu modülü YALNIZ sayfanın tembel parçası (`features/dealership/contactPage`) içe aktarır;
 * vitrinin statik import ağacından ulaşmak `check:locales`'te kırılır. Sayfanın `<h1>`'i ve üst
 * çubuğun bağlantısı ilk pakette basıldığı için `contact.title` vitrin kümesindedir.
 */
registerCatalogs({ tr, ar, en })
