import { createContext, useContext, useMemo, useState } from "react";
import type { Dispatch, ReactNode, SetStateAction } from "react";
import { text } from "../i18n/text";
import { Button } from "../primitives/Button";

interface SpookieWatchLinkState {
  link: string | null;
  setLink: Dispatch<SetStateAction<string | null>>;
}

const missingLink: SpookieWatchLinkState = {
  link: null,
  setLink: () => {},
};

const SpookieWatchLinkContext = createContext<SpookieWatchLinkState>(missingLink);

export const SpookieWatchLinkProvider = ({ children }: { children: ReactNode }) => {
  const [link, setLink] = useState<string | null>(null);
  const value = useMemo(() => ({ link, setLink }), [link]);

  return (
    <SpookieWatchLinkContext.Provider value={value}>{children}</SpookieWatchLinkContext.Provider>
  );
};

export const useSpookieWatchLink = (): SpookieWatchLinkState => useContext(SpookieWatchLinkContext);

export const SpookieWatchActions = () => {
  const { link } = useSpookieWatchLink();
  if (!link)
    return null;

  return (
    <div className="app__actions">
      <Button
        variant="primary"
        className="spookie-watch-link"
        href={link}
        target="_blank"
        rel="noopener noreferrer"
      >
        {text("spookie.watchHere")}
      </Button>
    </div>
  );
};
