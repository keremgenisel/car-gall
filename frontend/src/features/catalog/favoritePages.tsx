/**
 * `frontend-003` **D13** · **P8** — favoriler ve karşılaştırma ekranlarının TEMBEL parçası. Ekranlar,
 * üçüncü katalog kümesi ve stilleri yalnız buradan iner; vitrinin ilk paketine girmez (`App`'ten tek
 * `import()`). Küme kaydı yan etkili içe aktarmadır ve ekranlardan ÖNCE değerlendirilir.
 */
import '../../shared/i18n/favoritesCatalog'
import './favoritePages.css'

export { ComparePage } from './ComparePage'
export { FavoritesPage } from './FavoritesPage'
