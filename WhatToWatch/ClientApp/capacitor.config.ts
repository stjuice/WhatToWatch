import type { CapacitorConfig } from '@capacitor/cli';

/** Capacitor config for the Android APK (native IMDb WebView import + Studio). */
const config: CapacitorConfig = {
  appId: 'com.whattowatch.app',
  appName: 'WhatToWatch',
  webDir: 'dist',
  server: {
    androidScheme: 'https',
  },
};

export default config;
