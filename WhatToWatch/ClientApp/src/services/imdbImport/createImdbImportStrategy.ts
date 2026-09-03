import { Capacitor } from "@capacitor/core";
import type { IImdbImportStrategy } from "./ImdbImportStrategy";
import { NativeWebViewImportStrategy } from "./NativeWebViewImportStrategy";
import { ServerUrlImportStrategy } from "./ServerUrlImportStrategy";

let cachedStrategy: IImdbImportStrategy | null = null;

export const createImdbImportStrategy = (): IImdbImportStrategy => {
  if (cachedStrategy) {
    return cachedStrategy;
  }

  cachedStrategy = Capacitor.isNativePlatform()
    ? new NativeWebViewImportStrategy()
    : new ServerUrlImportStrategy();

  return cachedStrategy;
};

export const resetImdbImportStrategy = (): void => {
  cachedStrategy = null;
};
