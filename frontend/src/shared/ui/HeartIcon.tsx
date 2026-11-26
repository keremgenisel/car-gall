/**
 * `frontend-003` **P4** — favori düğmesinin ve sayacın TEK simgesi. Dolu/boş hâl renkle değil
 * biçimle ayrılır: çizgili kalp, basılı düğmede dolgulu (CSS: `[aria-pressed='true'] .heart`).
 * Dekoratiftir; anlamı düğmenin adı ve `aria-pressed` taşır.
 */
export function HeartIcon() {
  return (
    <svg className="heart" viewBox="0 0 24 24" aria-hidden="true">
      <path d="M12 20s-7-4.4-8.8-8.6C1.8 8.2 3.8 5 7 5c2 0 3.4 1 5 2.8C13.6 6 15 5 17 5c3.2 0 5.2 3.2 3.8 6.4C19 15.6 12 20 12 20z" />
    </svg>
  )
}
