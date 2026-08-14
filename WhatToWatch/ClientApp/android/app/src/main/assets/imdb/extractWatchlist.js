/**
 * WebView extract script for ImdbImportActivity.
 *
 * Must stay behaviorally identical to src/native/extractWatchlistFromNextData.ts.
 * Vitest compares both against the same fixtures.
 *
 * evaluateJavascript expects an expression; this IIFE returns a JSON string or null.
 */
(function () {
  var el = document.getElementById("__NEXT_DATA__");
  if (!el) return null;

  var data;
  try {
    data = JSON.parse(el.textContent);
  } catch (e) {
    return null;
  }

  function readRating(node) {
    var rating = node.ratingsSummary && node.ratingsSummary.aggregateRating;
    return typeof rating === "number" ? rating : null;
  }

  function readPlot(node) {
    var plot =
      node.plot && node.plot.plotText && node.plot.plotText.plainText;
    return typeof plot === "string" ? plot : null;
  }

  function readRuntimeMinutes(node) {
    if (!node.runtime) return null;
    if (typeof node.runtime.seconds === "number" && node.runtime.seconds > 0) {
      return Math.floor(node.runtime.seconds / 60);
    }
    if (typeof node.runtime.minutes === "number" && node.runtime.minutes > 0) {
      return node.runtime.minutes;
    }
    return null;
  }

  function readGenres(node) {
    var genres =
      node.titleGenres && Array.isArray(node.titleGenres.genres)
        ? node.titleGenres.genres
        : [];
    var result = [];
    for (var i = 0; i < genres.length; i++) {
      var text = genres[i] && genres[i].genre && genres[i].genre.text;
      if (typeof text === "string" && text) result.push(text);
    }
    return result;
  }

  function readCreditName(credit) {
    var text =
      credit && credit.name && credit.name.nameText && credit.name.nameText.text;
    return typeof text === "string" && text ? text : null;
  }

  function readFirstCreditName(entry) {
    if (entry.credits && Array.isArray(entry.credits)) {
      for (var i = 0; i < entry.credits.length; i++) {
        var name = readCreditName(entry.credits[i]);
        if (name) return name;
      }
    }
    return readCreditName(entry);
  }

  function isDirectorCategory(text) {
    return typeof text === "string" && /^directors?$/i.test(text);
  }

  function readDirector(node) {
    var v2 = node.principalCreditsV2;
    if (Array.isArray(v2)) {
      for (var i = 0; i < v2.length; i++) {
        var grouping = v2[i] && v2[i].grouping && v2[i].grouping.text;
        if (isDirectorCategory(grouping)) {
          var fromV2 = readFirstCreditName(v2[i]);
          if (fromV2) return fromV2;
        }
      }
    }

    var credits = node.principalCredits;
    if (Array.isArray(credits)) {
      for (var j = 0; j < credits.length; j++) {
        var category = credits[j] && credits[j].category;
        var id = category && category.id;
        var text = category && category.text;
        if (
          (typeof id === "string" && id.toLowerCase() === "director") ||
          isDirectorCategory(text)
        ) {
          var fromV1 = readFirstCreditName(credits[j]);
          if (fromV1) return fromV1;
        }
      }
    }

    return null;
  }

  function extractWatchlistFromNextData(root, options) {
    var movies = [];
    var seen = {};

    function visit(node) {
      if (!node || typeof node !== "object") return;
      if (Array.isArray(node)) {
        for (var i = 0; i < node.length; i++) visit(node[i]);
        return;
      }

      var id = node.id;
      var titleText = node.titleText && node.titleText.text;
      if (typeof id === "string" && id.indexOf("tt") === 0 && titleText && !seen[id]) {
        seen[id] = true;
        movies.push({
          imdbId: id,
          title: titleText,
          year: node.releaseYear && node.releaseYear.year != null ? node.releaseYear.year : null,
          imageUrl: node.primaryImage && node.primaryImage.url ? node.primaryImage.url : null,
          rating: readRating(node),
          plot: readPlot(node),
          runtimeMinutes: readRuntimeMinutes(node),
          director: readDirector(node),
          genres: readGenres(node),
        });
      }

      for (var k in node) {
        if (Object.prototype.hasOwnProperty.call(node, k)) visit(node[k]);
      }
    }

    function findListName(node) {
      if (!node || typeof node !== "object") return null;
      if (Array.isArray(node)) {
        for (var i = 0; i < node.length; i++) {
          var n = findListName(node[i]);
          if (n) return n;
        }
        return null;
      }

      var keys = ["list", "predefinedList"];
      for (var j = 0; j < keys.length; j++) {
        var l = node[keys[j]];
        if (l && typeof l === "object") {
          if (l.name && (l.name.originalText || l.name.text)) {
            return l.name.originalText || l.name.text;
          }
          if (l.nameText && l.nameText.text) return l.nameText.text;
        }
      }

      for (var k in node) {
        if (Object.prototype.hasOwnProperty.call(node, k)) {
          var found = findListName(node[k]);
          if (found) return found;
        }
      }
      return null;
    }

    visit(root);
    if (!movies.length) return null;

    var pathname = (options && options.pathname) || "";
    var idMatch = pathname.match(/(ls\d+)/);
    var listId = idMatch ? idMatch[1] : null;
    var title = findListName(root) || (options && options.documentTitle) || listId || "IMDb list";

    return {
      listId: listId || title,
      title: title,
      movies: movies,
    };
  }

  var result = extractWatchlistFromNextData(data, {
    pathname: location.pathname,
    documentTitle: document.title,
  });

  return result ? JSON.stringify(result) : null;
})();
