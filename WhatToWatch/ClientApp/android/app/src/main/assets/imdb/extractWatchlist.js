(function() {
  "use strict";
  function extractWatchlistFromNextData(data, options = {}) {
    const movies = [];
    const seen = /* @__PURE__ */ new Set();

    visit(data, movies, seen);

    if (movies.length === 0)
      return null;

    const listId = extractListId(options.pathname);
    const title = findListName(data) ?? options.documentTitle ?? listId ?? "IMDb list";
    const hasNextPage = readHasNextPage(data);

    return {
      listId: listId ?? title,
      title,
      movies,
      hasNextPage,
      nextPageUrl: hasNextPage ? buildNextPageUrl(options.currentUrl) : null
    };
  }
  function extractListId(pathname) {
    const list = pathname == null ? void 0 : pathname.match(/\/list\/(ls\d+)/i);
    if (list == null ? void 0 : list[1]) 
      return list[1];

    const watchlist = pathname == null ? void 0 : pathname.match(
      /\/user\/((?:ur\d+|p\.[a-z0-9]+))\/watchlist/i
    );

    if (watchlist == null ? void 0 : watchlist[1]) 
      return watchlist[1];

    const chart = pathname == null ? void 0 : pathname.match(/\/chart\/([a-z0-9][a-z0-9-]*)/i);

    return (chart == null ? void 0 : chart[1]) ? `chart-${chart[1].toLowerCase()}` : null;
  }
  function buildNextPageUrl(currentUrl) {
    if (!currentUrl) 
      return null;

    try {
      const url = new URL(currentUrl);

      if (url.protocol !== "https:" || !/(^|\.)imdb\.com$/i.test(url.hostname))
        return null;

      const current = Number.parseInt(url.searchParams.get("page") ?? "1", 10);

      url.searchParams.set(
        "page",
        String(Number.isFinite(current) && current > 0 ? current + 1 : 2)
      );

      return url.toString();
    } catch {
      return null;
    }
  }
  function readHasNextPage(node) {
    if (node == null || typeof node !== "object") 
      return false;

    if (Array.isArray(node))
      return node.some((item) => readHasNextPage(item));
    
    const record = node;
    if (record.titleListItemSearch && typeof record.titleListItemSearch === "object" && record.titleListItemSearch !== null) {
      const search = record.titleListItemSearch;
      const pageInfo = search.pageInfo;

      if (pageInfo && typeof pageInfo === "object" && pageInfo !== null)
        return pageInfo.hasNextPage === true;
    }
    return Object.values(record).some((value) => readHasNextPage(value));
  }
  function visit(node, movies, seen) {
    if (node == null || typeof node !== "object") 
      return;

    if (Array.isArray(node)) {
      for (const item of node) {
        visit(item, movies, seen);
      }

      return;
    }

    const record = node;
    const id = record.id;
    const titleText = record.titleText && typeof record.titleText === "object" && record.titleText !== null && typeof record.titleText.text === "string" ? record.titleText.text : null;
    
    if (typeof id === "string" && id.startsWith("tt") && titleText && !seen.has(id)) {
      seen.add(id);

      const releaseYear = record.releaseYear && typeof record.releaseYear === "object" && record.releaseYear !== null ? record.releaseYear.year : null;
      const primaryImage = record.primaryImage && typeof record.primaryImage === "object" && record.primaryImage !== null ? record.primaryImage.url : null;
      
      movies.push({
        imdbId: id,
        title: titleText,
        year: typeof releaseYear === "number" ? releaseYear : null,
        imageUrl: typeof primaryImage === "string" ? primaryImage : null,
        rating: readRating(record),
        plot: readPlot(record),
        runtimeMinutes: readRuntimeMinutes(record),
        director: readDirector(record),
        genres: readGenres(record),
        titleType: readTitleType(record)
      });
    }
    for (const value of Object.values(record)) {
      visit(value, movies, seen);
    }
  }
  function readTitleType(record) {
    const titleType = record.titleType;
    if (!titleType || typeof titleType !== "object") 
      return null;
    
    const id = titleType.id;
    return typeof id === "string" && id ? id : null;
  }
  function readRating(record) {
    const summary = record.ratingsSummary;
    if (!summary || typeof summary !== "object") 
      return null;
    
    const rating = summary.aggregateRating;
    return typeof rating === "number" ? rating : null;
  }
  function readPlot(record) {
    const plot = record.plot;
    if (!plot || typeof plot !== "object") 
      return null;
    
    const plotText = plot.plotText;
    return typeof (plotText == null ? void 0 : plotText.plainText) === "string" ? plotText.plainText : null;
  }
  function readRuntimeMinutes(record) {
    const runtime = record.runtime;
    if (!runtime || typeof runtime !== "object")
      return null;
    
    const value = runtime;
    if (typeof value.seconds === "number" && value.seconds > 0)
      return Math.floor(value.seconds / 60);

    if (typeof value.minutes === "number" && value.minutes > 0)
      return value.minutes;
    
    return null;
  }
  function readGenres(record) {
    const titleGenres = record.titleGenres;
    if (!titleGenres || typeof titleGenres !== "object")
      return [];
    
    const genres = titleGenres.genres;
    if (!Array.isArray(genres))
      return [];
    
    const result = [];
    for (const entry of genres) {
      if (!entry || typeof entry !== "object")
        continue;
      
      const genre = entry.genre;
      if (typeof (genre == null ? void 0 : genre.text) === "string" && genre.text) {
        result.push(genre.text);
      }
    }
    return result;
  }
  function readDirector(record) {
    const fromV2 = readDirectorFromCredits(
      record.principalCreditsV2,
      (entry) => {
        var _a;
        const text = (_a = entry.grouping) == null ? void 0 : _a.text;
        return typeof text === "string" && /^directors?$/i.test(text);
      }
    );
    if (fromV2) {
      return fromV2;
    }
    return readDirectorFromCredits(record.principalCredits, (entry) => {
      const category = entry.category;
      const id = category == null ? void 0 : category.id;
      const text = category == null ? void 0 : category.text;

      return typeof id === "string" && id.toLowerCase() === "director" || typeof text === "string" && /^directors?$/i.test(text);
    });
  }
  function readDirectorFromCredits(credits, isDirector) {
    if (!Array.isArray(credits))
      return null;

    for (const entry of credits) {
      if (!entry || typeof entry !== "object")
        continue;
      
      const record = entry;
      if (!isDirector(record))
        continue;

      const name = readFirstCreditName(record);
      if (name)
        return name;
    }
    return null;
  }

  function readFirstCreditName(entry) {
    const credits = entry.credits;
    if (Array.isArray(credits)) {
      for (const credit of credits) {
        const name = readCreditName(credit);

        if (name)
          return name;
      }
    }
    return readCreditName(entry);
  }
  function readCreditName(credit) {
    var _a;
    if (!credit || typeof credit !== "object")
      return null;
    
    const name = credit.name;
    const text = (_a = name == null ? void 0 : name.nameText) == null ? void 0 : _a.text;

    return typeof text === "string" && text ? text : null;
  }
  function findListName(node) {
    if (node == null || typeof node !== "object")
      return null;
    
    if (Array.isArray(node)) {
      for (const item of node) {
        const name = findListName(item);

        if (name)
          return name;
      }

      return null;
    }

    const record = node;
    for (const key of ["list", "predefinedList"]) {
      const list = record[key];

      if (list && typeof list === "object" && !Array.isArray(list)) {
        const listRecord = list;
        const nameObj = listRecord.name;

        if (nameObj && typeof nameObj === "object" && nameObj !== null) {
          const originalText = nameObj.originalText;
          const text = nameObj.text;

          if (typeof originalText === "string" && originalText)
            return originalText;

          if (typeof text === "string" && text)
            return text;
        }

        const nameText = listRecord.nameText;

        if (nameText && typeof nameText === "object" && nameText !== null) {
          const text = nameText.text;

          if (typeof text === "string" && text)
            return text;
        }
      }
    }
    for (const value of Object.values(record)) {
      const name = findListName(value);

      if (name)
        return name;
    }

    return null;
  }
  const extractImdbPage = () => {
    const element = document.getElementById("__NEXT_DATA__");
    if (!(element == null ? void 0 : element.textContent))
      return null;

    try {
      const result = extractWatchlistFromNextData(JSON.parse(element.textContent), {
        pathname: location.pathname,
        documentTitle: document.title,
        currentUrl: location.href
      });

      return result ? JSON.stringify(result) : null;
    } catch {
      return null;
    }
  };
  globalThis.__whatToWatchExtractImdbPage = extractImdbPage;
  const imdbPageExtractorEntry = extractImdbPage();
  
  return imdbPageExtractorEntry;
})();