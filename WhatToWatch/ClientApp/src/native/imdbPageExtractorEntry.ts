import { extractWatchlistFromNextData } from "./extractWatchlistFromNextData";

type ExtractorGlobal = typeof globalThis & {
  __whatToWatchExtractImdbPage?: () => string | null;
};

const extractImdbPage = (): string | null => {
  const element = document.getElementById("__NEXT_DATA__");
  if (!element?.textContent) 
    return null;

  try {
    const result = extractWatchlistFromNextData(JSON.parse(element.textContent), {
      pathname: location.pathname,
      documentTitle: document.title,
      currentUrl: location.href,
    });
    
    return result ? JSON.stringify(result) : null;
  } catch {
    return null;
  }
};

(globalThis as ExtractorGlobal).__whatToWatchExtractImdbPage = extractImdbPage;

export default extractImdbPage();
