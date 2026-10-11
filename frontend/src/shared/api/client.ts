import type { AppLocale } from '../lib/locale'

/**
 * Tüm ağ istekleri bu client'tan geçer; bileşenin içinde fetch çağrısı yasaktır
 * (docs/frontend.md § 2). Endpoint yolları yalnız bu klasörde yazılır.
 */

export class ApiError extends Error {
  readonly errorCode: string
  /**
   * Sunucunun gönderdiği yapısal parametre: hangi ALANIN hatalı olduğu. Metin değildir
   * ve ziyaretçiye gösterilmez — ekran alanı işaretler, metni kendi kataloğundan üretir
   * (docs/conventions.md § 5).
   */
  readonly field: string | null
  readonly traceId: string | null
  /**
   * HTTP durum kodu; ağ hatasında `null`. Yalnız api/ katmanı okur — "bulunamadı"yı
   * "hata"dan ayırmak için gerekir (AC-18 / AC-19). Ziyaretçiye ASLA gösterilmez
   * (docs/frontend.md § 4): ErrorState zaten durum kodu alan bir prop taşımıyor.
   */
  readonly status: number | null
  /**
   * 0012b **D2** · **D24** — sunucunun gönderdiği DİĞER yapısal parametreler, ham hâliyle.
   *
   * <p>`field` tek bir alanı işaret ediyordu ve yetmedi: yayın reddi **koşul başına bir
   * kod** taşıyor (`reasons`) ve zorunlu alan reddi **alan anahtarlarının listesini**
   * (`fields`). İkisi de virgüllü dize — `BuildingBlocks`'un `Error` tipi yapısal bir dizi
   * taşımıyor ve onu zenginleştirmek Yasak Listesi 11'e girerdi.</p>
   *
   * <p>İçeriği ASLA doğrudan basılmaz: her değer uçta kendi çeviri anahtarına çevrilir
   * (Yasak Listesi 23, `docs/frontend.md` § 3.1).</p>
   */
  readonly parameters: Readonly<Record<string, string>>

  constructor(
    errorCode: string,
    traceId: string | null,
    status: number | null = null,
    field: string | null = null,
    parameters: Readonly<Record<string, string>> = {},
  ) {
    // Mesaj yalnız geliştirici içindir; ziyaretçiye ASLA gösterilmez (docs/frontend.md § 4).
    super(errorCode)
    this.name = 'ApiError'
    this.errorCode = errorCode
    this.traceId = traceId
    this.status = status
    this.field = field
    this.parameters = parameters
  }
}

const DEFAULT_ERROR_CODE = 'api.unavailable'

/**
 * Yazma isteği. `Idempotency-Key` ZORUNLUDUR (docs/conventions.md § 3): ağ hatası isteğin
 * sunucuya ulaşmasından *sonra* olduysa, "tekrar dene" aynı anahtarı yeniden gönderir ve
 * ikinci bir kayıt oluşmaz. Anahtarı client üretmez — çağıran üretir ve formun ömrü
 * boyunca saklar; burada üretilseydi her deneme yeni bir anahtar alırdı ve koruma
 * tam da işe yarayacağı anda kaybolurdu.
 */
export async function postJson<TResponse>(
  path: string,
  body: unknown,
  locale: AppLocale,
  idempotencyKey: string,
  signal?: AbortSignal,
): Promise<TResponse> {
  let response: Response

  try {
    response = await fetch(path, {
      method: 'POST',
      signal,
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
        'Accept-Language': locale,
        'Idempotency-Key': idempotencyKey,
      },
      body: JSON.stringify(body),
    })
  } catch (cause) {
    if (cause instanceof DOMException && cause.name === 'AbortError') {
      throw cause
    }

    throw new ApiError(DEFAULT_ERROR_CODE, null)
  }

  if (!response.ok) {
    throw await toApiError(response)
  }

  return (await response.json()) as TResponse
}

export async function getJson<TResponse>(
  path: string,
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<TResponse> {
  let response: Response

  try {
    response = await fetch(path, {
      signal,
      // Oturum çerezi HttpOnly'dir ve JS onu OKUYAMAZ (0006 C17); tarayıcı aynı kökene
      // giden isteklerde kendisi taşır. Açıkça yazılması, varsayılanın değişmesi
      // hâlinde oturumun sessizce kaybolmasını engeller.
      credentials: 'same-origin',
      headers: { Accept: 'application/json', 'Accept-Language': locale },
    })
  } catch (cause) {
    if (cause instanceof DOMException && cause.name === 'AbortError') {
      throw cause
    }

    throw new ApiError(DEFAULT_ERROR_CODE, null)
  }

  if (!response.ok) {
    throw await toApiError(response)
  }

  return (await response.json()) as TResponse
}

async function toApiError(response: Response): Promise<ApiError> {
  try {
    const problem = (await response.json()) as Record<string, unknown> & {
      errorCode?: string
      traceId?: string
      field?: string
    }

    // RFC 7807'nin kendi alanları ayıklanıyor; kalanı sunucunun yapısal parametreleridir.
    // Ayıklama olmadan `status` ve `type` de "parametre" sayılır ve ekrana teknik detay
    // sızdırma riski doğar.
    const bilinen = new Set([
      'type',
      'title',
      'detail',
      'instance',
      'status',
      'errorCode',
      'traceId',
    ])
    const parameters: Record<string, string> = {}

    for (const [key, value] of Object.entries(problem)) {
      if (!bilinen.has(key) && typeof value === 'string') {
        parameters[key] = value
      }
    }

    return new ApiError(
      problem.errorCode ?? DEFAULT_ERROR_CODE,
      problem.traceId ?? null,
      response.status,
      problem.field ?? null,
      parameters,
    )
  } catch {
    // Gövde okunamadıysa da kullanıcıya teknik detay sızmaz; genel kodla devam edilir.
    // 404 gövdesiz gelir (E7): durum kodu burada korunmazsa "bulunamadı" kaybolurdu.
    return new ApiError(DEFAULT_ERROR_CODE, null, response.status)
  }
}

/**
 * Durum değiştiren istek. Oturum çerezi tarayıcı tarafından taşınır; uç onu ne okur ne
 * yazar (0006 C17, AC-29).
 */
export async function sendJson<TResponse>(
  method: 'POST' | 'PUT' | 'DELETE',
  path: string,
  body: unknown,
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<TResponse | null> {
  let response: Response

  try {
    response = await fetch(path, {
      method,
      signal,
      credentials: 'same-origin',
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
        'Accept-Language': locale,
      },
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch (cause) {
    if (cause instanceof DOMException && cause.name === 'AbortError') {
      throw cause
    }

    throw new ApiError(DEFAULT_ERROR_CODE, null)
  }

  if (!response.ok) {
    throw await toApiError(response)
  }

  return jsonGovdesiVar(response) ? ((await response.json()) as TResponse) : null
}

/**
 * Yanıtta ayrıştırılacak bir JSON gövdesi var mı?
 *
 * <b>Bu fonksiyon bir kusurun bedeliydi.</b> Önceki hâl yalnız `204`'ü gövdesiz
 * sayıyordu; tekrar deneme ucu ise `202 Accepted` + **boş gövde** dönüyor
 * (`MediaAdminEndpoints.RetryAsync` — "iş kabul edildi, bitmedi"). `response.json()`
 * boş gövdede `SyntaxError: Unexpected end of JSON input` atıyor, çağıran onu
 * `api.unavailable`'a çeviriyor ve **başarılı** bir tekrar deneme personele genel hata
 * olarak görünüyordu — sunucu işi kabul etmiş olmasına rağmen.
 *
 * Kontrol durum koduna değil, yanıtın KENDİSİNE bakıyor: bir sonraki gövdesiz `2xx`
 * aynı duvara çarpmasın. `Content-Length: 0` ya da JSON olmayan/eksik `Content-Type`
 * gövdesizlik demektir.
 */
function jsonGovdesiVar(response: Response): boolean {
  // `205 Reset Content` da tanım gereği gövdesizdir; `204` ile aynı aileden.
  if (response.status === 204 || response.status === 205) {
    return false
  }

  if (response.headers.get('Content-Length') === '0') {
    return false
  }

  const tip = response.headers.get('Content-Type')

  return tip !== null && tip.includes('json')
}

/**
 * 0012a **D9** — tek dosya yüklemesi. `multipart/form-data`; gövde `FormData` olduğu için
 * `Content-Type` AÇIKÇA yazılmaz — tarayıcı `boundary` ile birlikte kendisi üretir ve elle
 * yazılan bir `Content-Type` o sınır çizgisini kaybettirir, sunucu da gövdeyi
 * ayrıştıramaz.
 *
 * `Idempotency-Key` TAŞIMAZ ve bu bilinçli: aynı dosyayı iki kez yüklemek iki ayrı medya
 * varlığı demektir ve ikisi de geçerlidir — personel aynı fotoğrafı bilerek iki kez
 * ekleyebilir. İdempotentlik, kaydın TEKİL olması gereken yerde gerekir (0005'in talep
 * ucu); burada kayıt tekil değil.
 */
export async function sendFile<TResponse>(
  path: string,
  file: File,
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<TResponse> {
  const body = new FormData()
  body.append('file', file)

  let response: Response

  try {
    response = await fetch(path, {
      method: 'POST',
      signal,
      credentials: 'same-origin',
      headers: {
        Accept: 'application/json',
        'Accept-Language': locale,
      },
      body,
    })
  } catch (cause) {
    if (cause instanceof DOMException && cause.name === 'AbortError') {
      throw cause
    }

    throw new ApiError(DEFAULT_ERROR_CODE, null)
  }

  if (!response.ok) {
    throw await toApiError(response)
  }

  return (await response.json()) as TResponse
}
