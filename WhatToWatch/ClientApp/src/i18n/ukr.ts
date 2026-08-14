/**
 * Ukrainian copy for the public app UI.
 * Keep keys stable so a future i18next (or similar) swap stays straightforward.
 */
export const ukr = {
  "nav.homeAria": "На головну",

  "home.randomAll": "Випадковий фільм з усіх списків",
  "home.randomAllAria": "Випадковий фільм з усіх списків",
  "home.loading": "Завантаження…",
  "home.empty": "Поки немає жодного списку.",

  "list.loading": "Завантаження…",
  "list.notFound": "Список не знайдено",
  "list.loadFailed": "Не вдалося завантажити список",
  "list.movieCount": "{count} фільмів",
  "list.randomAria": "Випадковий фільм зі списку {name}",

  "movie.posterAlt": "Постер: {title}",
  "movie.posterAltFallback": "Постер фільму",
  "movie.director": "Режисер: {name}",
  "movie.rerollAria": "Інший випадковий фільм",
  "movie.randomAria": "Отримати випадковий фільм",

  "errors.watchlistsLoadFailed": "Не вдалося завантажити списки",
  "errors.pickFailed": "Не вдалося обрати фільм",
  "errors.apiNetwork":
    "Не вдалося з'єднатися з API ({base}). Перевірте підключення та адресу сервера.",
} as const;

export type UkrKey = keyof typeof ukr;
