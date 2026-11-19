import ar from './locales/panel/ar.json'
import en from './locales/panel/en.json'
import tr from './locales/panel/tr.json'
import { registerCatalogs } from './i18n'

/**
 * Panel kataloğu (ADR `0013`). Yalnız panelin kullandığı anahtarlar burada yaşar ve bu
 * modülü YALNIZ panelin lazy paketi içe aktarır (`AdminApp`): ziyaretçi vitrini açtığında
 * panel metinleri hiç inmez. Vitrinin statik import ağacından bu modüle ulaşmak
 * `check:locales`'te kırılır.
 */
registerCatalogs({ tr, ar, en })
