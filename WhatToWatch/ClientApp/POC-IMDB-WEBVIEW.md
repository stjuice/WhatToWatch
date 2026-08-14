# IMDb Android WebView POC

Isolated Capacitor Android experiment to verify whether a user-device WebView can load an IMDb list page and read `__NEXT_DATA__` with `evaluateJavascript`.

This is **not** wired into the React UI, ASP.NET endpoints, or the current Playwright import flow.
No Android Studio required: everything below runs with the Gradle wrapper and Android platform-tools (adb).

## What was added

| Path | Purpose |
|------|---------|
| `WhatToWatch/ClientApp/capacitor.config.ts` | Capacitor app config |
| `WhatToWatch/ClientApp/android/` | Generated Capacitor Android project |
| `.../ImdbImportActivity.java` | WebView screen: loads IMDb, auto-extracts, copies, dumps to file |
| `.../ImdbImportPlugin.java` | Optional Capacitor bridge back to JavaScript |
| `.../activity_imdb_import.xml` | POC layout (status + Extract / Copy JSON / Done + WebView) |
| `.../AndroidManifest.xml` | `ImdbImportActivity` is the **temporary launcher activity** |
| `public/poc/imdb-webview.html` | Standalone HTML launcher (only needed for the JS-bridge variant) |
| `src/poc/imdbImportPlugin.ts` | Optional TS wrapper |

## Success checks

1. IMDb loads inside the WebView
2. You can complete login / human verification manually
3. Logcat shows `document.title`
4. Logcat shows `__NEXT_DATA__` with a non-zero length (inline when small, as a file path when large)

Log tag: **`ImdbWebViewPoc`**

## Prerequisites (one time, no Android Studio)

- **JDK 21** — Capacitor 7 compiles with `sourceCompatibility 21`, so JDK 8 or 17 will fail.

```powershell
winget install --id Microsoft.OpenJDK.21 -e
```

- **Android command-line tools + SDK packages** (`compileSdk` is 35):

```powershell
winget install --id Google.AndroidSDK.CommandLineTools -e
```

Then point the environment at the SDK and install the packages (adjust paths if your install differs):

```powershell
$env:JAVA_HOME = (Get-ChildItem "C:\Program Files\Microsoft\jdk-21*" | Select-Object -First 1).FullName
$env:ANDROID_HOME = "$env:LOCALAPPDATA\Android\Sdk"
& "$env:ANDROID_HOME\cmdline-tools\latest\bin\sdkmanager.bat" --sdk_root="$env:ANDROID_HOME" "platform-tools" "platforms;android-35" "build-tools;35.0.0"
$env:Path = "$env:JAVA_HOME\bin;$env:ANDROID_HOME\platform-tools;$env:Path"
```

Gradle also accepts an explicit SDK path instead of `ANDROID_HOME`:

```powershell
Set-Content -Path WhatToWatch\ClientApp\android\local.properties -Value "sdk.dir=$($env:LOCALAPPDATA -replace '\\','\\')\\Android\\Sdk"
```

- A physical Android device with **USB debugging** enabled and authorized (`adb devices` shows `device`).

## Download a prebuilt APK (no local SDK / ADB)

GitHub Actions builds the debug APK and publishes it as a workflow artifact:

1. Open **Actions → Build IMDb WebView POC APK** on GitHub (pushes to `poc-android-web` trigger it, or run **workflow_dispatch**).
2. Open the finished run → **Artifacts** → download **`imdb-webview-poc-debug`**.
3. Unzip and copy `app-debug.apk` to the phone, then open it and allow install from that source.

Workflow file: `.github/workflows/imdb-webview-poc.yml`

## Build the debug APK locally

From `WhatToWatch/ClientApp`:

```powershell
npm install
npm run build
npx cap sync android
cd android
.\gradlew.bat assembleDebug
```

Or the shortcut (does build + sync + assemble):

```powershell
npm run poc:apk
```

APK output:

```text
WhatToWatch\ClientApp\android\app\build\outputs\apk\debug\app-debug.apk
```

## Install on the device

```powershell
adb install -r WhatToWatch\ClientApp\android\app\build\outputs\apk\debug\app-debug.apk
```

Or, from `WhatToWatch/ClientApp`:

```powershell
npm run poc:install
```

## Run the POC

`ImdbImportActivity` is the launcher activity for this POC, so just tap the app icon, or start it explicitly:

```powershell
adb shell am start -n com.whattowatch.app/.ImdbImportActivity
```

With a custom list URL:

```powershell
adb shell am start -n com.whattowatch.app/.ImdbImportActivity --es imdb_url "https://www.imdb.com/list/ls4117371353/"
```

Behavior:

- After **every** page load the activity waits ~1.5 s for Next.js hydration and automatically logs `document.title` and `__NEXT_DATA__`.
- **Extract** re-runs the probe manually (use it after finishing a login or bot challenge).
- **Copy JSON** puts the extracted `__NEXT_DATA__` on the device clipboard; if the payload is too large for the clipboard it is written to a file instead and the path is logged.
- **Done** closes the activity and returns the result to the Capacitor bridge (only relevant for the JS-bridge variant).

## Logcat

```powershell
adb logcat -c
adb logcat -s ImdbWebViewPoc:I
```

Expected output:

```text
ImdbWebViewPoc: Opening IMDb URL: https://www.imdb.com/list/ls4117371353/
ImdbWebViewPoc: onPageFinished: https://www.imdb.com/list/ls4117371353/
ImdbWebViewPoc: document.title => Some list - IMDb
ImdbWebViewPoc: __NEXT_DATA__ present=true length=284913
ImdbWebViewPoc: __NEXT_DATA__ preview: {"props":{"pageProps":...
ImdbWebViewPoc: __NEXT_DATA__ too large for Logcat; saved to: /storage/emulated/0/Android/data/com.whattowatch.app/files/next-data-1723620000000.json
ImdbWebViewPoc: Pull it with: adb pull /storage/emulated/0/Android/data/com.whattowatch.app/files/next-data-1723620000000.json
```

Payloads up to 3500 characters are logged inline as `__NEXT_DATA__ json: ...`; anything larger goes to a file because a single Logcat message is truncated near 4 KB.

Pull the dump to your machine:

```powershell
adb pull /storage/emulated/0/Android/data/com.whattowatch.app/files/next-data-<timestamp>.json
```

List available dumps:

```powershell
adb shell ls -l /storage/emulated/0/Android/data/com.whattowatch.app/files/
```

If `__NEXT_DATA__` is missing, finish the login/verification in the WebView and tap **Extract** again.

## Return value to JavaScript (optional bridge variant)

`ImdbImport.openAndExtract({ url })` resolves with:

```ts
{
  ok: true,
  title: string,
  nextData?: string,      // full __NEXT_DATA__ JSON when it fits in the Intent
  nextDataLength: number,
  nextDataFile?: string   // on-device path when the payload was dumped to a file
}
```

Reaching this path requires `MainActivity` to be the launcher again (see below).

## Removing the POC later

Delete / revert:

- `ClientApp/android/` (or at least the `ImdbImport*` Java/layout files, the manifest launcher swap, and the `MainActivity` plugin registration)
- `ClientApp/capacitor.config.ts`
- `ClientApp/public/poc/`
- `ClientApp/src/poc/`
- Capacitor packages and the `cap:sync` / `poc:apk` / `poc:install` scripts in `package.json`
- the `/poc/` entry in `vite.config.ts` `navigateFallbackDenylist`
- Capacitor ignore rules in the repo `.gitignore`

To restore normal app behavior without deleting the POC, move the `MAIN`/`LAUNCHER` intent-filter in `AndroidManifest.xml` from `ImdbImportActivity` back to `MainActivity`.
