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
import java.net.URI;
import java.nio.charset.StandardCharsets;
import java.util.Locale;
import java.util.Set;

/**
 * Pure helpers for the IMDb WebView import flow. Kept free of Activity state so JUnit can cover them.
 */
final class ImdbImportSupport {
    static final String EXTRACT_SCRIPT_ASSET = "imdb/extractWatchlist.js";

    private ImdbImportSupport() {
    }

    /** Loads the generated extractor built from extractWatchlistFromNextData.ts. */
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

    static PageData parsePage(@Nullable String json) throws JSONException {
        if (json == null || json.isEmpty()) {
            throw new JSONException("Missing page payload");
        }

        JSONObject root = new JSONObject(json);
        JSONArray movies = root.optJSONArray("movies");
        if (movies == null) {
            throw new JSONException("Missing movies array");
        }

        return new PageData(
            root.optString("listId", ""),
            root.optString("title", ""),
            movies,
            root.optBoolean("hasNextPage", false),
            root.has("nextPageUrl") && !root.isNull("nextPageUrl")
                ? root.optString("nextPageUrl")
                : null
        );
    }

    static JSONObject createAggregate(PageData page) throws JSONException {
        JSONObject aggregate = new JSONObject();
        aggregate.put("listId", page.listId);
        aggregate.put("title", page.title);
        aggregate.put("movies", new JSONArray());
        return aggregate;
    }

    static int appendUniqueMovies(PageData page, JSONObject aggregate, Set<String> seenIds)
        throws JSONException {
        JSONArray target = aggregate.getJSONArray("movies");
        int added = 0;

        for (int index = 0; index < page.movies.length(); index++) {
            JSONObject movie = page.movies.getJSONObject(index);
            String imdbId = movie.optString("imdbId", "").trim();
            if (imdbId.isEmpty() || !seenIds.add(imdbId)) {
                continue;
            }

            target.put(movie);
            added++;
        }

        return added;
    }

    static boolean isAllowedNextPageUrl(@Nullable String url) {
        if (url == null || url.trim().isEmpty()) {
            return false;
        }

        try {
            URI uri = new URI(url.trim());
            String scheme = uri.getScheme();
            if (scheme == null || !"https".equalsIgnoreCase(scheme)) {
                return false;
            }

            String host = uri.getHost();
            if (host == null) {
                return false;
            }

            return host.equalsIgnoreCase("imdb.com") || host.endsWith(".imdb.com");
        } catch (Exception e) {
            return false;
        }
    }

    static final class PageData {
        final String listId;
        final String title;
        final JSONArray movies;
        final boolean hasNextPage;
        @Nullable final String nextPageUrl;

        PageData(
            String listId,
            String title,
            JSONArray movies,
            boolean hasNextPage,
            @Nullable String nextPageUrl
        ) {
            this.listId = listId;
            this.title = title;
            this.movies = movies;
            this.hasNextPage = hasNextPage;
            this.nextPageUrl = nextPageUrl;
        }
    }
}
