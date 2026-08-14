import type { CapacitorConfig } from '@capacitor/cli';

/**
 * Capacitor config for the isolated IMDb WebView POC.
 * Not wired into the existing React import flow.
 */
const config: CapacitorConfig = {
  appId: 'com.whattowatch.app',
  appName: 'WhatToWatch',
  webDir: 'dist',
  server: {
    androidScheme: 'https',
  },
};

export default config;
