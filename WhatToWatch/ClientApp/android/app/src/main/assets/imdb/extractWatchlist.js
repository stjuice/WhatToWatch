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
