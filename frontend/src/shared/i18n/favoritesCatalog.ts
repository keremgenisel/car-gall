import ar from './locales/favorites/ar.json'
import en from './locales/favorites/en.json'
import tr from './locales/favorites/tr.json'
import { registerCatalogs } from './i18n'

/**
 * `frontend-003` **D13** · **P8** — favoriler ve karşılaştırma ekranlarının kataloğu: ÜÇÜNCÜ küme
 * (ADR `0013` eki). Bu modülü YALNIZ ekranların tembel parçası (`features/catalog/favoritePages`)
 * içe aktarır; vitrinin statik import ağacından ulaşmak `check:locales`'te kırılır.
 */
registerCatalogs({ tr, ar, en })
