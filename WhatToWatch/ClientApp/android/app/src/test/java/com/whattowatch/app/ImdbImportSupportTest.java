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
}
