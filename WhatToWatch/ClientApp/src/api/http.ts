const trimTrailingSlash = (value: string): string => value.replace(/\/+$/, "");

/** Absolute API origin from Vite, or empty for same-origin (dev proxy / Render web). */
export const apiBaseUrl = trimTrailingSlash(import.meta.env.VITE_API_BASE_URL ?? "");

export const apiUrl = (path: string): string => {
  const normalized = path.startsWith("/") ? path : `/${path}`;
  return `${apiBaseUrl}${normalized}`;
};

export const ADMIN_KEY_STORAGE = "w2w.adminApiKey";

export const getAdminApiKey = (): string | null => {
  try {
    return sessionStorage.getItem(ADMIN_KEY_STORAGE);
  } catch {
    return null;
  }
};

export const setAdminApiKey = (key: string): void => {
  sessionStorage.setItem(ADMIN_KEY_STORAGE, key.trim());
};

export const clearAdminApiKey = (): void => {
  sessionStorage.removeItem(ADMIN_KEY_STORAGE);
};

const readErrorMessage = async (response: Response): Promise<string> => {
  try {
    const body: unknown = await response.json();
    if (
      body &&
      typeof body === "object" &&
      "error" in body &&
      typeof (body as { error: unknown }).error === "string"
    ) {
      return (body as { error: string }).error;
    }
  } catch {
    // Fall through to status text.
  }

  return response.statusText || `Request failed (${response.status})`;
};

export type RequestJsonOptions = RequestInit & {
  admin?: boolean;
};

export const requestJson = async <T>(
  path: string,
  init?: RequestJsonOptions
): Promise<T> => {
  const headers: Record<string, string> = {
    Accept: "application/json",
    ...(init?.body ? { "Content-Type": "application/json" } : {}),
    ...(init?.headers as Record<string, string> | undefined),
  };

  if (init?.admin) {
    const key = getAdminApiKey();
    if (!key) {
      throw new Error("Потрібен ключ адміністратора");
    }
    headers["X-Admin-Key"] = key;
  }

  const { admin: _admin, ...fetchInit } = init ?? {};
  const response = await fetch(apiUrl(path), {
    ...fetchInit,
    headers,
  });

  if (!response.ok) {
    throw new Error(await readErrorMessage(response));
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
};
