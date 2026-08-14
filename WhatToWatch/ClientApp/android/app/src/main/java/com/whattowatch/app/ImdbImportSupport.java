package com.whattowatch.app;

import android.content.res.AssetManager;
import android.net.Uri;

import androidx.annotation.Nullable;
import androidx.annotation.VisibleForTesting;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.Locale;

/**
 * Pure helpers for the IMDb WebView import flow. Kept free of Activity state so JUnit can cover them.
 */
final class ImdbImportSupport {
    static final String EXTRACT_SCRIPT_ASSET = "imdb/extractWatchlist.js";

    private ImdbImportSupport() {
    }

    /**
     * Loads the extract IIFE from assets. Must stay in sync with
     * {@code src/native/extractWatchlistFromNextData.ts}.
     */
    static String loadExtractScript(AssetManager assets) throws IOException {
        try (InputStream in = assets.open(EXTRACT_SCRIPT_ASSET);
             BufferedReader reader = new BufferedReader(new InputStreamReader(in, StandardCharsets.UTF_8))) {
            StringBuilder builder = new StringBuilder();
            String line;
            while ((line = reader.readLine()) != null) {
                builder.append(line).append('\n');
            }
            return builder.toString().trim();
        }
    }

    /**
     * IMDb's Branch.io interstitial redirects to {@code intent://} / {@code imdb://} deep links,
     * which a WebView cannot load. Only http(s) navigations are allowed.
     */
    static boolean shouldBlockNavigation(@Nullable Uri uri) {
        if (uri == null) {
            return false;
        }
        return shouldBlockScheme(uri.getScheme());
    }

    @VisibleForTesting
    static boolean shouldBlockScheme(@Nullable String scheme) {
        if (scheme == null) {
            return false;
        }

        String normalized = scheme.toLowerCase(Locale.US);
        return !"http".equals(normalized) && !"https".equals(normalized);
    }

    /** Returns the number of movies in the normalized payload, or -1 when it is unusable. */
    static int countMovies(@Nullable String json) {
        if (json == null || json.isEmpty()) {
            return -1;
        }

        try {
            JSONArray movies = new JSONObject(json).optJSONArray("movies");
            return movies == null ? -1 : movies.length();
        } catch (JSONException e) {
            return -1;
        }
    }

    /**
     * evaluateJavascript returns a JSON-encoded value (e.g. "\"{...}\"" or "null").
     */
    @Nullable
    @VisibleForTesting
    static String unwrapJsString(@Nullable String jsValue) {
        if (jsValue == null || "null".equals(jsValue)) {
            return null;
        }

        try {
            Object parsed = new org.json.JSONTokener(jsValue).nextValue();
            return parsed == null || parsed == JSONObject.NULL ? null : String.valueOf(parsed);
        } catch (Exception e) {
            return null;
        }
    }
}
