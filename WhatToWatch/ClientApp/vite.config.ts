import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { VitePWA } from "vite-plugin-pwa";

export default defineConfig(() => {
  const isNativeBuild = process.env.npm_lifecycle_event === "build:native";

  return {
    plugins: [
    react(),
    VitePWA({
      registerType: "autoUpdate",
      // APK updates keep WebView storage. Replace any previously installed
      // worker and clear its cached bundle while retaining PWA caching on web.
      selfDestroying: isNativeBuild,
      includeAssets: ["icons/apple-touch-icon.png"],
      manifest: {
        name: "WHAT 2 WATCH",
        short_name: "What2Watch",
        description: "Random movie from your IMDb watchlist",
        lang: "uk",
        start_url: "/",
        display: "standalone",
        background_color: "#242221",
        theme_color: "#242221",
        icons: [
          {
            src: "icons/pwa-192x192.png",
            sizes: "192x192",
            type: "image/png",
          },
          {
            src: "icons/pwa-512x512.png",
            sizes: "512x512",
            type: "image/png",
          },
          {
            src: "icons/pwa-512x512.png",
            sizes: "512x512",
            type: "image/png",
            purpose: "maskable",
          },
        ],
      },
      showMaximumFileSizeToCacheInBytesWarning: true,
      workbox: {
        globPatterns: ["**/*.{js,css,html,ico,png,svg,woff2,webmanifest}"],
        globIgnores: ["**/node_modules/**/*", "**/*list-frame-long*"],
        maximumFileSizeToCacheInBytes: 10 * 1024 * 1024,
        navigateFallbackDenylist: [/^\/api/],
        runtimeCaching: [
          {
            urlPattern: ({ url }) => url.pathname.startsWith("/api/"),
            handler: "NetworkOnly",
            method: "GET",
          },
          {
            urlPattern: ({ url }) => url.pathname.startsWith("/api/"),
            handler: "NetworkOnly",
            method: "PUT",
          },
          {
            urlPattern: ({ url }) => url.pathname.startsWith("/api/"),
            handler: "NetworkOnly",
            method: "DELETE",
          },
        ],
      },
      devOptions: {
        enabled: false,
      },
    }),
  ],
  css: {
    preprocessorOptions: {
      scss: {
        api: "modern-compiler",
      },
    },
  },
  server: {
    port: 5173,
    proxy: {
      "/api": "http://localhost:5264",
    },
  },
  };
});
