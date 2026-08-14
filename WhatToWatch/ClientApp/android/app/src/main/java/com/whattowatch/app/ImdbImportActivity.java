package com.whattowatch.app;

import android.annotation.SuppressLint;
import android.app.Activity;
import android.content.Intent;
import android.graphics.Bitmap;
import android.net.Uri;
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

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.util.Locale;

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

    /**
     * Reads __NEXT_DATA__ and returns only the fields the app needs, or null when the page does
     * not (yet) hold list data. Item shapes differ between IMDb list renderers, hence the fallbacks.
     */
    private static final String EXTRACT_SCRIPT =
        "(function () {"
            + "  var el = document.getElementById('__NEXT_DATA__');"
            + "  if (!el) return null;"
            + "  var data;"
            + "  try { data = JSON.parse(el.textContent); } catch (e) { return null; }"
            + "  var pageProps = data && data.props && data.props.pageProps;"
            + "  var list = pageProps && pageProps.mainColumnData && pageProps.mainColumnData.list;"
            + "  if (!list) return null;"
            + "  var entries = list.items"
            + "    || (list.titleListItemSearch && list.titleListItemSearch.edges)"
            + "    || [];"
            + "  var movies = [];"
            + "  for (var i = 0; i < entries.length; i++) {"
            + "    var entry = entries[i] || {};"
            + "    var node = entry.node || entry;"
            + "    var title = node.title || node.listItem || node;"
            + "    if (!title || !title.id) continue;"
            + "    movies.push({"
            + "      imdbId: title.id,"
            + "      title: title.titleText && title.titleText.text ? title.titleText.text : null,"
            + "      year: title.releaseYear && title.releaseYear.year != null"
            + "        ? title.releaseYear.year : null,"
            + "      imageUrl: title.primaryImage && title.primaryImage.url"
            + "        ? title.primaryImage.url : null"
            + "    });"
            + "  }"
            + "  if (!movies.length) return null;"
            + "  var name = list.title"
            + "    || (list.name && (list.name.originalText || list.name.text))"
            + "    || document.title;"
            + "  return JSON.stringify({"
            + "    listId: list.id || null,"
            + "    title: name,"
            + "    movies: movies"
            + "  });"
            + "})();";

    private final Handler mainHandler = new Handler(Looper.getMainLooper());

    private WebView webView;
    private TextView statusView;
    private Runnable pendingExtract;
    private boolean isResultDelivered;

    @SuppressLint("SetJavaScriptEnabled")
    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_imdb_import);

        statusView = findViewById(R.id.imdb_import_status);
        webView = findViewById(R.id.imdb_import_webview);
        Button cancelButton = findViewById(R.id.imdb_import_cancel);
        cancelButton.setOnClickListener(v -> finishWithError(ERROR_CANCELLED));

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
                return shouldBlockNavigation(request.getUrl());
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

    /**
     * IMDb's Branch.io interstitial redirects to {@code intent://} / {@code imdb://} deep links,
     * which a WebView cannot load (ERR_UNKNOWN_URL_SCHEME). Stay on the web version instead.
     */
    private boolean shouldBlockNavigation(@Nullable Uri uri) {
        if (uri == null) {
            return false;
        }

        String scheme = uri.getScheme();
        if (scheme == null) {
            return false;
        }

        scheme = scheme.toLowerCase(Locale.US);
        if ("http".equals(scheme) || "https".equals(scheme)) {
            return false;
        }

        Log.i(TAG, "Blocked app deep link (" + scheme + "): " + uri);
        return true;
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
        if (webView == null || isResultDelivered) {
            return;
        }

        webView.evaluateJavascript(EXTRACT_SCRIPT, value -> {
            String json = unwrapJsString(value);
            int movieCount = countMovies(json);

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

    /** Returns the number of movies in the normalized payload, or -1 when it is unusable. */
    private int countMovies(@Nullable String json) {
        if (json == null || json.isEmpty()) {
            return -1;
        }

        try {
            JSONArray movies = new JSONObject(json).optJSONArray("movies");
            return movies == null ? -1 : movies.length();
        } catch (JSONException e) {
            Log.w(TAG, "Extracted payload is not valid JSON: " + e.getMessage());
            return -1;
        }
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

    /**
     * evaluateJavascript returns a JSON-encoded value (e.g. "\"{...}\"" or "null").
     */
    @Nullable
    static String unwrapJsString(@Nullable String jsValue) {
        if (jsValue == null || "null".equals(jsValue)) {
            return null;
        }

        try {
            // org.json handles escaped quotes / unicode from the WebView bridge.
            Object parsed = new org.json.JSONTokener(jsValue).nextValue();
            return parsed == null || parsed == JSONObject.NULL ? null : String.valueOf(parsed);
        } catch (Exception e) {
            Log.w(TAG, "Failed to unwrap JS value: " + e.getMessage());
            return null;
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
