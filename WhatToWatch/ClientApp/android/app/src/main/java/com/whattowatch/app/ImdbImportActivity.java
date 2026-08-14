package com.whattowatch.app;

import android.annotation.SuppressLint;
import android.app.Activity;
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

import androidx.annotation.Nullable;
import androidx.appcompat.app.AppCompatActivity;

import java.io.IOException;

/**
 * Internal IMDb import screen, opened only by {@link ImdbImporterPlugin}.
 *
 * Loads the list in a WebView so the user can log in or pass human verification, extracts and
 * normalizes the list from __NEXT_DATA__, then closes itself and hands the result to the plugin.
 */
public class ImdbImportActivity extends AppCompatActivity {
    public static final String TAG = "ImdbImport";

    public static final String EXTRA_URL = "imdb_url";
    public static final String EXTRA_WATCHLIST_JSON = "watchlist_json";
    public static final String EXTRA_ERROR = "error";

    public static final String ERROR_CANCELLED = "cancelled";

    /** Next.js needs a moment to hydrate after onPageFinished. */
    private static final long FIRST_ATTEMPT_DELAY_MS = 1200L;

    /** While the user logs in or solves a challenge the list data is not on the page yet. */
    private static final long RETRY_DELAY_MS = 2000L;

    private final Handler mainHandler = new Handler(Looper.getMainLooper());

    private WebView webView;
    private TextView statusView;
    private Runnable pendingExtract;
    private boolean isResultDelivered;
    private String extractScript;

    @SuppressLint("SetJavaScriptEnabled")
    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_imdb_import);

        statusView = findViewById(R.id.imdb_import_status);
        webView = findViewById(R.id.imdb_import_webview);
        Button cancelButton = findViewById(R.id.imdb_import_cancel);
        cancelButton.setOnClickListener(v -> finishWithError(ERROR_CANCELLED));

        try {
            extractScript = ImdbImportSupport.loadExtractScript(getAssets());
        } catch (IOException e) {
            Log.e(TAG, "Could not load extract script: " + e.getMessage());
            finishWithError("Could not load the IMDb extract script");
            return;
        }

        String url = getIntent().getStringExtra(EXTRA_URL);
        if (url == null || url.trim().isEmpty()) {
            Log.e(TAG, "No list url passed to ImdbImportActivity");
            finishWithError("No IMDb list url was provided");
            return;
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
                boolean blocked = ImdbImportSupport.shouldBlockNavigation(request.getUrl());
                if (blocked) {
                    Log.i(TAG, "Blocked app deep link: " + request.getUrl());
                }
                return blocked;
            }

            @Override
            public void onPageStarted(WebView view, String pageUrl, Bitmap favicon) {
                setStatus("Завантажуємо IMDb…");
                Log.i(TAG, "onPageStarted: " + pageUrl);
            }

            @Override
            public void onPageFinished(WebView view, String pageUrl) {
                Log.i(TAG, "onPageFinished: " + pageUrl);
                scheduleExtract(FIRST_ATTEMPT_DELAY_MS);
            }
        });

        Log.i(TAG, "Opening IMDb list: " + url);
        setStatus("Відкриваємо список…");
        webView.loadUrl(url.trim());
    }

    private void scheduleExtract(long delayMs) {
        if (isResultDelivered) {
            return;
        }

        if (pendingExtract != null) {
            mainHandler.removeCallbacks(pendingExtract);
        }

        pendingExtract = () -> {
            pendingExtract = null;
            extract();
        };
        mainHandler.postDelayed(pendingExtract, delayMs);
    }

    private void extract() {
        if (webView == null || isResultDelivered || extractScript == null) {
            return;
        }

        webView.evaluateJavascript(extractScript, value -> {
            String json = ImdbImportSupport.unwrapJsString(value);
            int movieCount = ImdbImportSupport.countMovies(json);

            if (movieCount <= 0) {
                // Most likely a login page or a bot challenge: keep polling while the user acts.
                Log.i(TAG, "List data not available yet; retrying in " + RETRY_DELAY_MS + "ms");
                setStatus("Очікуємо список. За потреби увійдіть в IMDb…");
                scheduleExtract(RETRY_DELAY_MS);
                return;
            }

            Log.i(TAG, "Extracted " + movieCount + " movies (" + json.length() + " chars)");
            setStatus("Знайдено фільмів: " + movieCount);
            finishWithWatchlist(json);
        });
    }

    private void finishWithWatchlist(String json) {
        if (isResultDelivered) {
            return;
        }

        isResultDelivered = true;
        mainHandler.removeCallbacksAndMessages(null);

        Intent result = new Intent();
        result.putExtra(EXTRA_WATCHLIST_JSON, json);
        setResult(Activity.RESULT_OK, result);
        finish();
    }

    private void finishWithError(String error) {
        if (isResultDelivered) {
            return;
        }

        isResultDelivered = true;
        mainHandler.removeCallbacksAndMessages(null);

        Log.i(TAG, "Import finished without data: " + error);
        Intent result = new Intent();
        result.putExtra(EXTRA_ERROR, error);
        setResult(Activity.RESULT_CANCELED, result);
        finish();
    }

    private void setStatus(String status) {
        if (statusView != null) {
            statusView.setText(status);
        }
    }

    @Override
    public void onBackPressed() {
        if (webView != null && webView.canGoBack()) {
            webView.goBack();
            return;
        }
        finishWithError(ERROR_CANCELLED);
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
