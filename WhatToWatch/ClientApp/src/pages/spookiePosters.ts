const posterModules = import.meta.glob<string>(
  "../assets/season/halloween/posters/*.{jpg,jpeg,png,webp}",
  { eager: true, import: "default" }
);

const keyFromPath = (path: string): string =>
  path.slice(path.lastIndexOf("/") + 1).replace(/\.[^.]+$/, "").toLowerCase();

const postersByKey = new Map(
  Object.entries(posterModules).map(([path, url]) => [keyFromPath(path), url])
);

export const getSpookiePoster = (key: string): string | undefined =>
  postersByKey.get(key.toLowerCase());
