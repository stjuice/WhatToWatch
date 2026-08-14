import { text } from "../i18n/text";

const trimTrailingSlash = (value: string): string => value.replace(/\/+$/, "");

/** Absolute API origin from Vite, or empty for same-origin (dev proxy / Render web). */
export const apiBaseUrl = trimTrailingSlash(import.meta.env.VITE_API_BASE_URL ?? "");

export const apiUrl = (path: string): string => {
  const normalized = path.startsWith("/") ? path : `/${path}`;
  return `${apiBaseUrl}${normalized}`;
};

const log = (message: string, ...details: unknown[]): void => {
  console.log(`[api] ${message}`, ...details);
};

log(`base URL = ${apiBaseUrl || "(same-origin)"}`);

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
      throw new Error("Admin API key required");
    }
    headers["X-Admin-Key"] = key;
  }

  const { admin: _admin, ...fetchInit } = init ?? {};
  const method = (fetchInit.method ?? "GET").toUpperCase();
  const url = apiUrl(path);
  const startedAt = performance.now();
  log(`→ ${method} ${url}${init?.admin ? " (admin)" : ""}`);

  let response: Response;
  try {
    response = await fetch(url, {
      ...fetchInit,
      headers,
    });
  } catch (error) {
    // Network-level failure ("Failed to fetch"): CORS, DNS, offline, or wrong base URL.
    log(
      `✗ ${method} ${url} failed before a response (network/CORS). ` +
        `Check VITE_API_BASE_URL and server CORS.`,
      error
    );
    throw new Error(text("errors.apiNetwork", { base: apiBaseUrl || "same-origin" }));
  }

  const elapsedMs = Math.round(performance.now() - startedAt);
  log(`← ${method} ${url} ${response.status} (${elapsedMs}ms)`);

  if (!response.ok) {
    const message = await readErrorMessage(response);
    log(`✗ ${method} ${url} ${response.status}: ${message}`);
    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
};
