package com.whattowatch.app;

import android.annotation.SuppressLint;
import android.app.Activity;
import android.content.ClipData;
import android.content.ClipboardManager;
import android.content.Context;
import android.content.Intent;
import android.graphics.Bitmap;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.util.Log;
import android.webkit.CookieManager;
import android.webkit.WebChromeClient;
import android.webkit.WebResourceRequest;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.Button;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.Nullable;
import androidx.appcompat.app.AppCompatActivity;

import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.nio.charset.StandardCharsets;

/**
 * Isolated POC activity: loads an IMDb list in a WebView and extracts
 * document.title + __NEXT_DATA__ via evaluateJavascript.
 *
 * Not part of the production import flow. Safe to delete with the rest of the POC.
 */
public class ImdbImportActivity extends AppCompatActivity {
    public static final String TAG = "ImdbWebViewPoc";

    public static final String EXTRA_URL = "imdb_url";
    public static final String EXTRA_TITLE = "document_title";
    public static final String EXTRA_NEXT_DATA = "next_data";
    public static final String EXTRA_NEXT_DATA_LENGTH = "next_data_length";
    public static final String EXTRA_NEXT_DATA_FILE = "next_data_file";
    public static final String EXTRA_ERROR = "error";

    public static final String DEFAULT_LIST_URL = "https://www.imdb.com/list/ls4117371353/";

    /** A single Logcat message is truncated around 4 KB, so keep inline dumps below that. */
    private static final int LOGCAT_SAFE_LENGTH = 3500;

    /** Binder transaction limit is ~1 MB and it is shared with the rest of the Intent. */
    private static final int INTENT_EXTRA_LIMIT = 700_000;

    /** Next.js hydration finishes shortly after onPageFinished. */
    private static final long AUTO_EXTRACT_DELAY_MS = 1500L;

    private final Handler mainHandler = new Handler(Looper.getMainLooper());

    private WebView webView;
    private TextView statusView;
    private String lastTitle = "";
    private String lastNextData = "";
    private String lastNextDataFile = null;
    private Runnable pendingAutoExtract;

    @SuppressLint("SetJavaScriptEnabled")
    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_imdb_import);

        statusView = findViewById(R.id.imdb_poc_status);
        webView = findViewById(R.id.imdb_poc_webview);
        Button extractButton = findViewById(R.id.imdb_poc_extract);
        Button copyButton = findViewById(R.id.imdb_poc_copy);
        Button doneButton = findViewById(R.id.imdb_poc_done);

        String url = getIntent().getStringExtra(EXTRA_URL);
        if (url == null || url.trim().isEmpty()) {
            url = DEFAULT_LIST_URL;
        }

        CookieManager cookieManager = CookieManager.getInstance();
        cookieManager.setAcceptCookie(true);
        cookieManager.setAcceptThirdPartyCookies(webView, true);

        WebSettings settings = webView.getSettings();
        settings.setJavaScriptEnabled(true);
        settings.setDomStorageEnabled(true);
        settings.setDatabaseEnabled(true);
        settings.setLoadWithOverviewMode(true);
        settings.setUseWideViewPort(true);
        settings.setBuiltInZoomControls(true);
        settings.setDisplayZoomControls(false);
        // Use a normal mobile Chrome UA so IMDb serves a real page, not a bot shell.
        settings.setUserAgentString(
            "Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 "
                + "(KHTML, like Gecko) Chrome/122.0.0.0 Mobile Safari/537.36"
        );

        webView.setWebChromeClient(new WebChromeClient());
        webView.setWebViewClient(new WebViewClient() {
            @Override
            public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
                return false;
            }

            @Override
            public void onPageStarted(WebView view, String pageUrl, Bitmap favicon) {
                setStatus("Loading…");
                Log.i(TAG, "onPageStarted: " + pageUrl);
            }

            @Override
            public void onPageFinished(WebView view, String pageUrl) {
                Log.i(TAG, "onPageFinished: " + pageUrl);
                scheduleAutoExtract();
            }
        });

        extractButton.setOnClickListener(v -> extractFromPage(false));
        copyButton.setOnClickListener(v -> copyNextDataToClipboard());
        doneButton.setOnClickListener(v -> finishWithResult());

        Log.i(TAG, "Opening IMDb URL: " + url);
        setStatus("Opening list…");
        webView.loadUrl(url);
    }

    /** Runs an extraction pass after every page load, debounced against SPA navigations. */
    private void scheduleAutoExtract() {
        if (pendingAutoExtract != null) {
            mainHandler.removeCallbacks(pendingAutoExtract);
        }

        setStatus("Loaded. Auto-extract in " + AUTO_EXTRACT_DELAY_MS + "ms…");
        pendingAutoExtract = () -> {
            pendingAutoExtract = null;
            extractFromPage(false);
        };
        mainHandler.postDelayed(pendingAutoExtract, AUTO_EXTRACT_DELAY_MS);
    }

    private void extractFromPage(boolean finishWhenDone) {
        if (webView == null) {
            return;
        }

        setStatus("Running evaluateJavascript…");
        Log.i(TAG, "evaluateJavascript: document.title");

        webView.evaluateJavascript("document.title", titleValue -> {
            lastTitle = unwrapJsString(titleValue);
            Log.i(TAG, "document.title => " + lastTitle);

            Log.i(TAG, "evaluateJavascript: __NEXT_DATA__ textContent");
            webView.evaluateJavascript(
                "(function(){ var el = document.getElementById('__NEXT_DATA__'); "
                    + "return el ? el.textContent : null; })();",
                nextDataValue -> {
                    lastNextData = unwrapJsString(nextDataValue);
                    lastNextDataFile = null;

                    int length = lastNextData == null ? 0 : lastNextData.length();
                    boolean present = length > 0 && !"null".equals(lastNextData);

                    Log.i(TAG, "__NEXT_DATA__ present=" + present + " length=" + length);

                    if (!present) {
                        Log.w(TAG, "__NEXT_DATA__ missing or empty. "
                            + "Complete login/verification in the WebView, then tap Extract.");
                        setStatus("No __NEXT_DATA__. Login/verify, then Extract.");
                    } else if (length <= LOGCAT_SAFE_LENGTH) {
                        Log.i(TAG, "__NEXT_DATA__ json: " + lastNextData);
                        setStatus("OK: title + __NEXT_DATA__ (" + length + " chars, logged inline)");
                    } else {
                        Log.i(TAG, "__NEXT_DATA__ preview: "
                            + lastNextData.substring(0, Math.min(240, length)));
                        File dumped = writeNextDataToFile(lastNextData);
                        if (dumped == null) {
                            setStatus("Extracted " + length + " chars, but file dump failed.");
                        } else {
                            lastNextDataFile = dumped.getAbsolutePath();
                            Log.i(TAG, "__NEXT_DATA__ too large for Logcat; saved to: " + lastNextDataFile);
                            Log.i(TAG, "Pull it with: adb pull " + lastNextDataFile);
                            setStatus("OK: " + length + " chars saved to " + dumped.getName());
                        }
                    }

                    if (finishWhenDone) {
                        finishWithResult();
                    }
                }
            );
        });
    }

    /**
     * Dumps the payload into the app-specific external directory, which
     * {@code adb pull} can read without root on a debug build.
     */
    @Nullable
    private File writeNextDataToFile(String json) {
        File dir = getExternalFilesDir(null);
        if (dir == null) {
            dir = getCacheDir();
        }

        File file = new File(dir, "next-data-" + System.currentTimeMillis() + ".json");
        try (FileOutputStream out = new FileOutputStream(file)) {
            out.write(json.getBytes(StandardCharsets.UTF_8));
            return file;
        } catch (IOException e) {
            Log.e(TAG, "Failed to write __NEXT_DATA__ dump: " + e.getMessage());
            return null;
        }
    }

    private void copyNextDataToClipboard() {
        if (!hasNextData()) {
            toastAndStatus("Nothing to copy yet — run Extract first.");
            return;
        }

        ClipboardManager clipboard = (ClipboardManager) getSystemService(Context.CLIPBOARD_SERVICE);
        if (clipboard == null) {
            toastAndStatus("Clipboard unavailable.");
            return;
        }

        try {
            clipboard.setPrimaryClip(ClipData.newPlainText("__NEXT_DATA__", lastNextData));
            Log.i(TAG, "Copied __NEXT_DATA__ to clipboard (" + lastNextData.length() + " chars)");
            toastAndStatus("Copied " + lastNextData.length() + " chars to clipboard.");
        } catch (RuntimeException e) {
            // Clipboard also travels over Binder, so very large payloads can be rejected.
            Log.w(TAG, "Clipboard copy failed: " + e.getMessage());
            File dumped = writeNextDataToFile(lastNextData);
            if (dumped == null) {
                toastAndStatus("Copy failed and file dump failed.");
            } else {
                lastNextDataFile = dumped.getAbsolutePath();
                Log.i(TAG, "Clipboard too small for payload; saved to: " + lastNextDataFile);
                Log.i(TAG, "Pull it with: adb pull " + lastNextDataFile);
                toastAndStatus("Too large for clipboard. Saved to " + dumped.getName());
            }
        }
    }

    private void finishWithResult() {
        Intent result = new Intent();
        result.putExtra(EXTRA_TITLE, lastTitle);

        if (hasNextData()) {
            result.putExtra(EXTRA_NEXT_DATA_LENGTH, lastNextData.length());
            if (lastNextData.length() <= INTENT_EXTRA_LIMIT) {
                result.putExtra(EXTRA_NEXT_DATA, lastNextData);
            } else {
                Log.w(TAG, "nextData length=" + lastNextData.length()
                    + " exceeds Intent budget; returning file path only.");
                result.putExtra(EXTRA_ERROR, "nextData too large for Intent bridge; see file dump");
            }
            if (lastNextDataFile != null) {
                result.putExtra(EXTRA_NEXT_DATA_FILE, lastNextDataFile);
            }
            setResult(Activity.RESULT_OK, result);
        } else {
            result.putExtra(EXTRA_NEXT_DATA_LENGTH, 0);
            result.putExtra(EXTRA_ERROR, "__NEXT_DATA__ not found");
            setResult(Activity.RESULT_CANCELED, result);
        }

        Log.i(TAG, "Finishing activity. titleLen="
            + (lastTitle == null ? 0 : lastTitle.length())
            + " nextDataLen="
            + (lastNextData == null ? 0 : lastNextData.length())
            + " nextDataFile=" + lastNextDataFile);
        finish();
    }

    private boolean hasNextData() {
        return lastNextData != null && !lastNextData.isEmpty() && !"null".equals(lastNextData);
    }

    private void toastAndStatus(String message) {
        Toast.makeText(this, message, Toast.LENGTH_SHORT).show();
        setStatus(message);
    }

    private void setStatus(String status) {
        if (statusView != null) {
            statusView.setText(status);
        }
        Log.i(TAG, "status: " + status);
    }

    /**
     * evaluateJavascript returns a JSON-encoded value (e.g. "\"Home\"" or "null").
     */
    static String unwrapJsString(@Nullable String jsValue) {
        if (jsValue == null || "null".equals(jsValue)) {
            return null;
        }

        try {
            // org.json handles escaped quotes / unicode from the WebView bridge.
            Object parsed = new org.json.JSONTokener(jsValue).nextValue();
            return parsed == null || parsed == org.json.JSONObject.NULL
                ? null
                : String.valueOf(parsed);
        } catch (Exception e) {
            Log.w(TAG, "Failed to unwrap JS value, using raw: " + e.getMessage());
            return jsValue;
        }
    }

    @Override
    public void onBackPressed() {
        if (webView != null && webView.canGoBack()) {
            webView.goBack();
            return;
        }
        super.onBackPressed();
    }

    @Override
    protected void onDestroy() {
        mainHandler.removeCallbacksAndMessages(null);
        if (webView != null) {
            webView.stopLoading();
            webView.destroy();
            webView = null;
        }
        super.onDestroy();
    }
}
