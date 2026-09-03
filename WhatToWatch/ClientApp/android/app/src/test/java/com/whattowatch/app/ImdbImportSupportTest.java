package com.whattowatch.app;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertFalse;
import static org.junit.Assert.assertNull;
import static org.junit.Assert.assertTrue;

import org.junit.Test;

/**
 * JVM-side coverage for the IMDb import helpers used by ImdbImportActivity.
 */
public class ImdbImportSupportTest {

    @Test
    public void unwrapJsString_returnsNull_forNullAndJsNull() {
        assertNull(ImdbImportSupport.unwrapJsString(null));
        assertNull(ImdbImportSupport.unwrapJsString("null"));
    }

    @Test
    public void unwrapJsString_decodesJsonEncodedString() {
        String encoded = "\"{\\\"listId\\\":\\\"ls1\\\",\\\"movies\\\":[]}\"";

        assertEquals(
            "{\"listId\":\"ls1\",\"movies\":[]}",
            ImdbImportSupport.unwrapJsString(encoded)
        );
    }

    @Test
    public void countMovies_readsMoviesArrayLength() {
        String json =
            "{\"listId\":\"ls1\",\"title\":\"X\",\"movies\":[{\"imdbId\":\"tt1\"},{\"imdbId\":\"tt2\"}]}";

        assertEquals(2, ImdbImportSupport.countMovies(json));
    }

    @Test
    public void countMovies_returnsNegative_whenPayloadIsMissingOrInvalid() {
        assertEquals(-1, ImdbImportSupport.countMovies(null));
        assertEquals(-1, ImdbImportSupport.countMovies(""));
        assertEquals(-1, ImdbImportSupport.countMovies("{not-json"));
        assertEquals(-1, ImdbImportSupport.countMovies("{\"listId\":\"ls1\"}"));
    }

    @Test
    public void shouldBlockScheme_allowsHttpAndHttps() {
        assertFalse(ImdbImportSupport.shouldBlockScheme("https"));
        assertFalse(ImdbImportSupport.shouldBlockScheme("http"));
        assertFalse(ImdbImportSupport.shouldBlockScheme("HTTPS"));
    }

    @Test
    public void shouldBlockScheme_blocksImdbDeepLinks() {
        assertTrue(ImdbImportSupport.shouldBlockScheme("intent"));
        assertTrue(ImdbImportSupport.shouldBlockScheme("imdb"));
        assertTrue(ImdbImportSupport.shouldBlockScheme("market"));
    }

    @Test
    public void shouldBlockScheme_allowsNull() {
        assertFalse(ImdbImportSupport.shouldBlockScheme(null));
    }

    @Test
    public void shouldBlockNavigation_allowsNullUri() {
        assertFalse(ImdbImportSupport.shouldBlockNavigation(null));
    }

    @Test
    public void parsePage_readsPaginationFields() throws Exception {
        String json =
            "{\"listId\":\"ls1\",\"title\":\"Paged\",\"movies\":[{\"imdbId\":\"tt1\"}],"
                + "\"hasNextPage\":true,\"nextPageUrl\":\"https://www.imdb.com/list/ls1/?page=2\"}";

        ImdbImportSupport.PageData page = ImdbImportSupport.parsePage(json);

        assertTrue(page.hasNextPage);
        assertEquals("https://www.imdb.com/list/ls1/?page=2", page.nextPageUrl);
    }

    @Test
    public void appendUniqueMovies_mergesAcrossPages() throws Exception {
        ImdbImportSupport.PageData first = ImdbImportSupport.parsePage(
            "{\"listId\":\"ls1\",\"title\":\"X\",\"movies\":[{\"imdbId\":\"tt1\"}],\"hasNextPage\":true}"
        );
        ImdbImportSupport.PageData second = ImdbImportSupport.parsePage(
            "{\"listId\":\"ls1\",\"title\":\"X\",\"movies\":[{\"imdbId\":\"tt1\"},{\"imdbId\":\"tt2\"}]}"
        );

        JSONObject aggregate = ImdbImportSupport.createAggregate(first);
        java.util.HashSet<String> seen = new java.util.HashSet<>();

        assertEquals(1, ImdbImportSupport.appendUniqueMovies(first, aggregate, seen));
        assertEquals(1, ImdbImportSupport.appendUniqueMovies(second, aggregate, seen));
        assertEquals(2, ImdbImportSupport.countMovies(aggregate.toString()));
    }

    @Test
    public void isAllowedNextPageUrl_acceptsImdbHttpsOnly() {
        assertTrue(ImdbImportSupport.isAllowedNextPageUrl(
            "https://www.imdb.com/list/ls1/?page=2"
        ));
        assertFalse(ImdbImportSupport.isAllowedNextPageUrl("http://www.imdb.com/list/ls1/"));
        assertFalse(ImdbImportSupport.isAllowedNextPageUrl("https://example.com/list/ls1/"));
    }
}
