import { TextKeys } from "./textKeys";

export const ukr = {
  [TextKeys.Navigation_ToHome]: "На головну",

  [TextKeys.Home_Watchlists]: "Всі списки",
  [TextKeys.Home_Party]: "Оберемо разом",

  [TextKeys.Party_AllMovies]: "Всі фільми",
  [TextKeys.Party_WatchlistCarousel]: "Вибір списку фільмів",
  [TextKeys.Party_CodeLabel]: "Код гри",
  [TextKeys.Party_Create]: "Створити гру",
  [TextKeys.Party_Join]: "Приєднатися",
  [TextKeys.Party_Errors_Suggestion]: "Не вдалося отримати код гри",
  [TextKeys.Party_Errors_CodeTaken]: "Цей код уже зайнятий. Спробуйте новий код.",
  [TextKeys.Party_Errors_InvalidCode]: "Гру з таким кодом не знайдено.",
  [TextKeys.Party_Errors_Action]: "Не вдалося почати гру",
  [TextKeys.Party_Errors_Full]: "У цій грі вже є два гравці.",
  [TextKeys.Party_Errors_Expired]: "Ця гра вже завершилася або застаріла.",
  [TextKeys.Party_Errors_Finished]: "Ця гра вже завершилася.",
  [TextKeys.Party_Errors_InvalidPlayerToken]: "Не вдалося підтвердити гравця.",
  [TextKeys.Party_Errors_NotYourCurrentMovie]: "Цей фільм уже не є поточним.",
  [TextKeys.Party_Errors_NoMovies]: "Для цієї гри немає доступних фільмів.",
  [TextKeys.Party_Errors_NoCodesAvailable]: "Наразі немає вільних кодів гри.",
  [TextKeys.Party_Errors_Join]: "Не вдалося приєднатися до гри.",
  [TextKeys.Party_Errors_Load]: "Не вдалося відновити гру.",
  [TextKeys.Party_Errors_Vote]: "Не вдалося зберегти вибір.",
  [TextKeys.Party_Joining]: "Приєднуємося до гри…",
  [TextKeys.Party_Loading]: "Відновлюємо гру…",
  [TextKeys.Party_PlayCode]: "Код гри: {code}",
  [TextKeys.Party_WaitingForOpponent]: "Очікуємо іншого гравця…",
  [TextKeys.Party_VoteNo]: "Ні",
  [TextKeys.Party_VoteYes]: "Так",
  [TextKeys.Party_Matched]: "Є збіг!",
  [TextKeys.Party_Exhausted]: "Ви переглянули всі фільми. Перевірте вибір іншого гравця.",
  [TextKeys.Party_Finished]: "Спільного вибору цього разу не знайшлося.",
  [TextKeys.Party_Expired]: "Час цієї гри минув.",
  [TextKeys.Party_SessionMissing]: "Не вдалося відновити цю гру на цьому пристрої.",
  [TextKeys.Party_Refresh]: "Оновити",
  [TextKeys.Party_Back]: "Обрати іншу гру",

  [TextKeys.Watchlists_RandomAll]: "Випадковий фільм з усіх списків",
  [TextKeys.Watchlists_RandomAllAria]: "Випадковий фільм з усіх списків",
  [TextKeys.Watchlists_Loading]: "Завантаження…",
  [TextKeys.Watchlists_Empty]: "Поки немає жодного списку.",
  [TextKeys.Watchlists_OpenList]: "Відкрити список {name}",

  [TextKeys.List_Loading]: "Завантаження…",
  [TextKeys.List_NotFound]: "Список не знайдено",
  [TextKeys.List_LoadFailed]: "Не вдалося завантажити список",
  [TextKeys.List_MovieCount]: "{count} фільмів",
  [TextKeys.List_ExpandAria]: "Показати список фільмів",
  [TextKeys.List_CollapseAria]: "Згорнути список фільмів",
  [TextKeys.List_RandomAria]: "Випадковий фільм зі списку {name}",
  
  [TextKeys.Movie_PosterAlt]: "Постер: {title}",
  [TextKeys.Movie_PosterAltFallback]: "Постер фільму",
  [TextKeys.Movie_OpenOnImdb]: "Відкрити на IMDb: {title}",
  [TextKeys.Movie_Director]: "Режисер: {name}",
  [TextKeys.Movie_RerollAria]: "Інший випадковий фільм",
  [TextKeys.Movie_RandomAria]: "Отримати випадковий фільм",

  [TextKeys.Errors_WatchlistsLoadFailed]: "Не вдалося завантажити списки",
  [TextKeys.Errors_PickFailed]: "Не вдалося обрати фільм",
  [TextKeys.Errors_ApiNetwork]:
    "Не вдалося з'єднатися з API ({base}). Перевірте підключення та адресу сервера.",
} as const;

export type UkrKey = keyof typeof ukr;
