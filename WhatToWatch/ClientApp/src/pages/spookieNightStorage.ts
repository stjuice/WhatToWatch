export const SPOOKIE_OPENED_KEY = "spookieNight.opened";

export const getOpenedSpookieTickets = (): Set<string> => {
  try {
    const raw = localStorage.getItem(SPOOKIE_OPENED_KEY);
    const parsed: unknown = raw ? JSON.parse(raw) : [];
    return new Set(
      Array.isArray(parsed)
        ? parsed.filter((key): key is string => typeof key === "string")
        : []
    );
  } catch {
    return new Set();
  }
};

export const markSpookieTicketOpened = (key: string): void => {
  const opened = getOpenedSpookieTickets();
  if (opened.has(key)) {
    return;
  }

  opened.add(key);
  try {
    localStorage.setItem(SPOOKIE_OPENED_KEY, JSON.stringify([...opened]));
  } catch {
    // Storage can be unavailable (private mode); the ticket still opens.
  }
};
