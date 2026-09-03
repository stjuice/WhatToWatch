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

import org.json.JSONException;
import org.json.JSONObject;

import java.io.IOException;
import java.util.ArrayDeque;
import java.util.HashSet;
import java.util.Set;

public class ImdbImportActivity extends AppCompatActivity {
    public static final String TAG = "ImdbImport";

    public static final String EXTRA_URL = "imdb_url";
    public static final String EXTRA_WATCHLIST_JSON = "watchlist_json";
    public static final String EXTRA_ERROR = "error";

    public static final String ERROR_CANCELLED = "cancelled";

    private static final long FIRST_ATTEMPT_DELAY_MS = 1200L;
    private static final long RETRY_DELAY_MS = 2000L;
    private static final int MAX_PAGES = 40;

    private final Handler mainHandler = new Handler(Looper.getMainLooper());

    private WebView webView;
    private TextView statusView;
    private TextView logView;
    private final ArrayDeque<String> visualLog = new ArrayDeque<>();
    private Runnable pendingExtract;
    private boolean isResultDelivered;
    private String extractScript;
    private int extractAttempt;
    private int pagesLoaded;
    private JSONObject aggregate;
    private final Set<String> seenMovieIds = new HashSet<>();

    @SuppressLint("SetJavaScriptEnabled")
    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_imdb_import);

        statusView = findViewById(R.id.imdb_import_status);
        logView = findViewById(R.id.imdb_import_log);
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
                    appendVisualLog("Заблоковано перехід у застосунок IMDb.");
                }
                return blocked;
            }

            @Override
            public void onPageStarted(WebView view, String pageUrl, Bitmap favicon) {
                setStatus("Завантажуємо IMDb…");
                appendVisualLog("IMDb завантажується…");
                Log.i(TAG, "onPageStarted: " + pageUrl);
            }

            @Override
            public void onPageFinished(WebView view, String pageUrl) {
                Log.i(TAG, "onPageFinished: " + pageUrl);
                appendVisualLog("Сторінку завантажено. Шукаємо дані списку…");
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
        if (webView == null || isResultDelivered || extractScript == null)
            return;

        extractAttempt++;
        appendVisualLog("Спроба читання №" + extractAttempt + "…");
        webView.evaluateJavascript(extractScript, value -> {
            String json = ImdbImportSupport.unwrapJsString(value);
            int movieCount = ImdbImportSupport.countMovies(json);

            if (movieCount <= 0) {
                Log.i(TAG, "List data not available yet; retrying in " + RETRY_DELAY_MS + "ms");
                setStatus("Очікуємо список. За потреби увійдіть в IMDb…");
                appendVisualLog("Дані ще недоступні. Очікуємо вхід або перевірку IMDb.");
                scheduleExtract(RETRY_DELAY_MS);

                return;
            }

            try {
                ImdbImportSupport.PageData page = ImdbImportSupport.parsePage(json);
                if (aggregate == null) {
                    aggregate = ImdbImportSupport.createAggregate(page);
                }

                int added = ImdbImportSupport.appendUniqueMovies(page, aggregate, seenMovieIds);
                pagesLoaded++;
                int total = ImdbImportSupport.countMovies(aggregate.toString());

                Log.i(
                    TAG,
                    "Extracted page " + pagesLoaded + ": +" + added + " movies, total=" + total
                );
                setStatus("Знайдено фільмів: " + total);
                appendVisualLog("Сторінка " + pagesLoaded + ": +" + added + " (усього " + total + ").");

                if (page.hasNextPage
                    && pagesLoaded < MAX_PAGES
                    && ImdbImportSupport.isAllowedNextPageUrl(page.nextPageUrl)) {
                    appendVisualLog("Завантажуємо наступну сторінку…");
                    webView.loadUrl(page.nextPageUrl);

                    return;
                }

                finishWithWatchlist(aggregate.toString());
            } catch (JSONException e) {
                Log.e(TAG, "Could not parse extracted page", e);
                finishWithError("Could not parse the imported watchlist");
            }
        });
    }

    private void finishWithWatchlist(String json) {
        if (isResultDelivered)
            return;

        isResultDelivered = true;
        mainHandler.removeCallbacksAndMessages(null);

        Intent result = new Intent();
        result.putExtra(EXTRA_WATCHLIST_JSON, json);
        setResult(Activity.RESULT_OK, result);
        finish();
    }

    private void finishWithError(String error) {
        if (isResultDelivered)
            return;

        isResultDelivered = true;
        mainHandler.removeCallbacksAndMessages(null);

        Log.i(TAG, "Import finished without data: " + error);
        Intent result = new Intent();

        result.putExtra(EXTRA_ERROR, error);
        setResult(Activity.RESULT_CANCELED, result);
        finish();
    }

    private void setStatus(String status) {
        if (statusView != null)
            statusView.setText(status);
    }

    private void appendVisualLog(String message) {
        if (logView == null)
            return;

        if (visualLog.size() == 5)
            visualLog.removeFirst();

        visualLog.addLast("• " + message);

        StringBuilder visibleText = new StringBuilder();
        for (String entry : visualLog) {
            if (visibleText.length() > 0) {
                visibleText.append('\n');
            }
            visibleText.append(entry);
        }
        logView.setText(visibleText.toString());
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
