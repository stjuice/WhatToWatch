/**
 * Ukrainian copy for the public app UI.
 * Keep keys stable so a future i18next (or similar) swap stays straightforward.
 */
export const ukr = {
  "nav.homeAria": "На головну",

  "home.watchlists": "Всі списки",
  "home.party": "Оберемо разом",

  "watchlists.randomAll": "Випадковий фільм з усіх списків",
  "watchlists.randomAllAria": "Випадковий фільм з усіх списків",
  "watchlists.loading": "Завантаження…",
  "watchlists.empty": "Поки немає жодного списку.",
  "watchlists.openList": "Відкрити список {name}",

  "list.loading": "Завантаження…",
  "list.notFound": "Список не знайдено",
  "list.loadFailed": "Не вдалося завантажити список",
  "list.movieCount": "{count} фільмів",
  "list.expandAria": "Показати список фільмів",
  "list.collapseAria": "Згорнути список фільмів",
  "list.randomAria": "Випадковий фільм зі списку {name}",

  "movie.posterAlt": "Постер: {title}",
  "movie.posterAltFallback": "Постер фільму",
  "movie.openOnImdb": "Відкрити на IMDb: {title}",
  "movie.director": "Режисер: {name}",
  "movie.rerollAria": "Інший випадковий фільм",
  "movie.randomAria": "Отримати випадковий фільм",

  "errors.watchlistsLoadFailed": "Не вдалося завантажити списки",
  "errors.pickFailed": "Не вдалося обрати фільм",
  "errors.apiNetwork":
    "Не вдалося з'єднатися з API ({base}). Перевірте підключення та адресу сервера.",
} as const;

export type UkrKey = keyof typeof ukr;
