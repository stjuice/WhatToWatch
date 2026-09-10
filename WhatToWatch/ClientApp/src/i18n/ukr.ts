/**
 * Ukrainian copy for the public app UI.
 * Keep keys stable so a future i18next (or similar) swap stays straightforward.
 */
export const ukr = {
  "nav.homeAria": "На головну",

  "home.watchlists": "Всі списки",
  "home.party": "Оберемо разом",

  "party.allMovies": "Всі фільми",
  "party.watchlistCarousel": "Вибір списку фільмів",
  "party.codeLabel": "Код гри",
  "party.create": "Створити гру",
  "party.join": "Приєднатися",
  "party.errors.suggestion": "Не вдалося отримати код гри",
  "party.errors.codeTaken": "Цей код уже зайнятий. Спробуйте новий код.",
  "party.errors.invalidCode": "Гру з таким кодом не знайдено.",
  "party.errors.action": "Не вдалося почати гру",
  "party.errors.full": "У цій грі вже є два гравці.",
  "party.errors.expired": "Ця гра вже завершилася або застаріла.",
  "party.errors.join": "Не вдалося приєднатися до гри.",
  "party.errors.load": "Не вдалося відновити гру.",
  "party.errors.vote": "Не вдалося зберегти вибір.",
  "party.joining": "Приєднуємося до гри…",
  "party.loading": "Відновлюємо гру…",
  "party.waitingForOpponent": "Очікуємо іншого гравця…",
  "party.voteNo": "Ні",
  "party.voteYes": "Так",
  "party.matched": "Є збіг!",
  "party.exhausted": "Ви переглянули всі фільми. Перевірте вибір іншого гравця.",
  "party.finished": "Спільного вибору цього разу не знайшлося.",
  "party.expired": "Час цієї гри минув.",
  "party.sessionMissing": "Не вдалося відновити цю гру на цьому пристрої.",
  "party.refresh": "Оновити",
  "party.back": "Обрати іншу гру",

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
