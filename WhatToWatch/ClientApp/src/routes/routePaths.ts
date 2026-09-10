export const routePaths = {
  home: "/",
  watchlists: "/lists",
  list: (id: string) => `/list/${encodeURIComponent(id)}`,
  listPattern: "/list/:id",
  movie: "/movie",
  party: "/party",
  partyJoinPattern: "/party/join/:code",
  partyJoin: (code: string) => `/party/join/${encodeURIComponent(code)}`,
  partyPlayPattern: "/party/:partyId",
  partyPlay: (partyId: string) => `/party/${encodeURIComponent(partyId)}`,
  studio: "/studio",
} as const;
