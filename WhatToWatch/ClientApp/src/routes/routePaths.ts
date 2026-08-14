export const routePaths = {
  home: "/",
  list: (id: string) => `/list/${encodeURIComponent(id)}`,
  listPattern: "/list/:id",
  movie: "/movie",
  studio: "/studio",
} as const;
