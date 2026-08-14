# Android IMDb import (in-app WebView)

Imports an IMDb list through a WebView inside the Android app instead of scraping IMDb from the
server. IMDb is loaded on the user's device, so login and human verification work, and the list is
normalized on-device before it reaches React.

The backend endpoints and the existing Playwright import flow are untouched.

## Flow

```text
React (ImportFromImdbButton)
  → ImdbImportService.importFromImdb(url)
  → ImdbImporter.importList({ url })            Capacitor plugin
  → ImdbImportActivity                          internal WebView, no launcher icon
  → IMDb page (login / verification if needed)
  → evaluateJavascript reads __NEXT_DATA__
  → normalized to { listId, title, movies[] }
  → activity closes itself
  → React receives ImportedWatchlist
```

## Files

| Path | Role |
|------|------|
| `capacitor.config.ts` | Capacitor app config |
| `android/` | Capacitor Android project |
| `.../ImdbImportActivity.java` | Internal WebView screen, extraction + normalization |
| `.../ImdbImporterPlugin.java` | Capacitor plugin `ImdbImporter.importList` |
| `.../MainActivity.java` | Registers the plugin |
| `.../activity_imdb_import.xml` | Status text, Cancel button, WebView |
| `src/native/imdbImporter.ts` | Plugin typings (`ImportedWatchlist`) |
| `src/services/imdbImportService.ts` | `ImdbImportService.importFromImdb(url)` |
| `src/components/ImportFromImdbButton.tsx` | Native-only trigger in `HomePage` |

## API

```ts
const result = await ImdbImporter.importList({
  url: "https://www.imdb.com/list/ls4117371353/",
});

interface ImportedWatchlist {
  listId: string;
  title: string;
  movies: Array<{
    imdbId: string;
    title: string;
    year: number | null;
    imageUrl: string | null;
  }>;
}
```

Rejects with `cancelled` when the user backs out or taps **Скасувати**.

## Behavior notes

- `ImdbImportActivity` is `exported="false"` and has no `MAIN`/`LAUNCHER` filter, so it is only
  reachable through the plugin — the user never sees a separate IMDb app.
- After each page load the activity waits ~1.2 s for Next.js hydration, then runs the extraction
  script. If the page holds no list data (login screen, bot challenge) it retries every 2 s while
  the user works through it, and closes as soon as the list appears.
- `intent://`, `imdb://` and any other non-http(s) scheme is blocked in `shouldOverrideUrlLoading`,
  which fixes the `ERR_UNKNOWN_URL_SCHEME` error from IMDb's Branch.io app banner.
- Only the first page of a list is present in `__NEXT_DATA__` (~250 items). Pagination is not
  handled yet.
- The button renders only when `Capacitor.isNativePlatform()` is true, so the web build is unchanged.

## Build the debug APK

### Via GitHub Actions (no local SDK)

1. **Actions → Build IMDb WebView POC APK** (pushes to `poc-android-web` trigger it, or run
   **workflow_dispatch**).
2. Open the finished run → **Artifacts** → download **`imdb-webview-poc-debug`**.
3. Unzip, copy `app-debug.apk` to the phone, open it and allow install from that source.

### Locally (Gradle + adb, no Android Studio)

Prerequisites, one time:

- **JDK 21** — Capacitor 7 compiles with `sourceCompatibility 21`, so JDK 8 or 17 will fail.

```powershell
winget install --id Microsoft.OpenJDK.21 -e
winget install --id Google.AndroidSDK.CommandLineTools -e

$env:JAVA_HOME = (Get-ChildItem "C:\Program Files\Microsoft\jdk-21*" | Select-Object -First 1).FullName
$env:ANDROID_HOME = "$env:LOCALAPPDATA\Android\Sdk"
& "$env:ANDROID_HOME\cmdline-tools\latest\bin\sdkmanager.bat" --sdk_root="$env:ANDROID_HOME" "platform-tools" "platforms;android-35" "build-tools;35.0.0"
$env:Path = "$env:JAVA_HOME\bin;$env:ANDROID_HOME\platform-tools;$env:Path"
```

Build, from `WhatToWatch/ClientApp`:

```powershell
npm install
npm run build
npx cap sync android
cd android
.\gradlew.bat assembleDebug
```

Or `npm run poc:apk` (build + sync + assemble). APK:

```text
WhatToWatch\ClientApp\android\app\build\outputs\apk\debug\app-debug.apk
```

Install:

```powershell
adb install -r c:\Development\Personal\WhatToWatch\WhatToWatch\ClientApp\android\app\build\outputs\apk\debug\app-debug.apk
```

Or `npm run poc:install`.

## Testing the flow

1. Launch the app (normal launcher icon → React UI).
2. Paste an IMDb list URL in the existing input.
3. Tap **Імпорт з IMDb**.
4. IMDb opens in the in-app WebView; log in or pass verification if asked.
5. The screen closes on its own and React logs the watchlist.

Native log:

```powershell
adb logcat -s ImdbImport:I
```

```text
ImdbImport: Opening IMDb list: https://www.imdb.com/list/ls4117371353/
ImdbImport: onPageFinished: https://www.imdb.com/list/ls4117371353/
ImdbImport: List data not available yet; retrying in 2000ms
ImdbImport: Extracted 152 movies (18432 chars)
```

React log (`chromium` tag, or Chrome DevTools remote inspection):

```text
[ImdbImport] imported watchlist { listId: "ls4117371353", title: "...", movies: [...] }
```

## Next steps (not done yet)

- Feed `ImportedWatchlist` into `AppStateContext` / the backend instead of only logging it.
- Handle lists longer than one page.

## Removing the integration

Delete `android/`, `capacitor.config.ts`, `src/native/imdbImporter.ts`,
`src/services/imdbImportService.ts`, `src/components/ImportFromImdbButton.tsx`, its two lines in
`src/pages/HomePage.tsx`, the Capacitor packages and `cap:sync` / `poc:apk` / `poc:install` scripts
in `package.json`, the Capacitor rules in the repo `.gitignore`, and
`.github/workflows/imdb-webview-poc.yml`.
