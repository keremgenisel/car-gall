/**
 * `frontend-003` **P4** — favori düğmesinin ve sayacın TEK simgesi. Dolu/boş hâl renkle değil
 * biçimle ayrılır: çizgili kalp, basılı düğmede dolgulu (CSS: `[aria-pressed='true'] .heart`).
 * Dekoratiftir; anlamı düğmenin adı ve `aria-pressed` taşır.
 */
export function HeartIcon() {
  return (
    <svg className="heart" viewBox="0 0 24 24" aria-hidden="true">
      <path d="M12 20 3.5 11.5a4.8 4.8 0 0 1 8.5-5 4.8 4.8 0 0 1 8.5 5z" />
    </svg>
  )
}
