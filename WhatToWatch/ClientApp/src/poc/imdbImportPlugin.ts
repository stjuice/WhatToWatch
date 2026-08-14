import { registerPlugin } from '@capacitor/core';

export type ImdbImportResult = {
  ok: boolean;
  title?: string | null;
  nextData?: string | null;
  nextDataLength?: number;
  error?: string | null;
};

export type ImdbImportPlugin = {
  openAndExtract(options?: { url?: string }): Promise<ImdbImportResult>;
};

/**
 * Capacitor plugin wrapper for the isolated Android IMDb WebView POC.
 * Not used by the existing React import flow.
 */
export const ImdbImport = registerPlugin<ImdbImportPlugin>('ImdbImport');
