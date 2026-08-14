import { ukr, type UkrKey } from "./ukr";

export type TVars = Record<string, string | number>;

/**
 * Resolve a Ukrainian UI string. Optional `{name}` placeholders are replaced from `vars`.
 * Shape is intentionally close to i18next so a later swap stays small.
 */
export const text = (key: UkrKey, vars?: TVars): string => {
  let value: string = ukr[key];
  if (!vars) {
    return value;
  }

  for (const [name, replacement] of Object.entries(vars)) {
    value = value.replace(new RegExp(`\\{${name}\\}`, "g"), String(replacement));
  }
  return value;
};
