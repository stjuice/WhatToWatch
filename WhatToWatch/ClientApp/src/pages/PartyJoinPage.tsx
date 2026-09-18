import { useEffect, useRef, useState } from "react";
import type { UIEvent, WheelEvent } from "react";
import { useNavigate, useParams } from "react-router-dom";
import popcornFull from "../assets/popcorn-full.svg";
import { getPartyPreview, joinParty } from "../api/partyApi";
import { setPartySession } from "../api/partySession";
import { COMPACT_LABEL_FRAME } from "../components/labelPill";
import { PartyButton } from "../components/PartyButton";
import { PopcornBucket } from "../components/PopcornBucket";
import { text } from "../i18n/text";
import { ErrorText } from "../primitives/ErrorText";
import { StatusText } from "../primitives/StatusText";
import { routePaths } from "../routes/routePaths";
import { useAppState } from "../state/AppStateContext";
import type { IPartyPreview } from "../types/party";
import { getPartyErrorText } from "./partyText";
import { TextKeys } from "../i18n/textKeys";
import { resolveCarouselActiveIndex } from "./PartyStartPage";
import "./PartyStartPage.scss";

export const PartyJoinPage = () => {
  const { code = "" } = useParams();
  const navigate = useNavigate();
  const { watchlists, watchlistsLoading } = useAppState();
  const carouselRef = useRef<HTMLDivElement>(null);
  const [preview, setPreview] = useState<IPartyPreview | null>(null);
  const [activeIndex, setActiveIndex] = useState(0);
  const [selectionInitialized, setSelectionInitialized] = useState(false);
  const [isJoining, setIsJoining] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setPreview(null);
    setSelectionInitialized(false);
    setActiveIndex(0);
    setError(null);

    const loadPreview = async () => {
      try {
        const loadedPreview = await getPartyPreview(code);
        if (!cancelled) setPreview(loadedPreview);
      } catch (previewError) {
        if (cancelled) return;

        setError(getPartyErrorText(previewError, TextKeys.Party_Errors_Join));
      }
    };

    void loadPreview();
    return () => {
      cancelled = true;
    };
  }, [code]);

  useEffect(() => {
    if (!preview || watchlistsLoading || selectionInitialized) return;

    const hostIndex = preview.watchlistId == null
      ? 0
      : watchlists.findIndex(({ id }) => id === preview.watchlistId) + 1;
    const selectedIndex = Math.max(0, hostIndex);
    setActiveIndex(selectedIndex);
    setSelectionInitialized(true);
    const selected = carouselRef.current?.children[selectedIndex] as
      | HTMLElement
      | undefined;
    selected?.scrollIntoView?.({
      behavior: "smooth",
      inline: "center",
      block: "nearest",
    });
  }, [preview, selectionInitialized, watchlists, watchlistsLoading]);

  const choices = [
    { id: null, name: text(TextKeys.Party_AllMovies) },
    ...watchlists.map(({ id, name }) => ({ id, name })),
  ];

  const updateActiveIndex = (event: UIEvent<HTMLDivElement>) => {
    const carousel = event.currentTarget;
    const viewportCenter = carousel.scrollLeft + carousel.clientWidth / 2;
    const itemCenters = Array.from(carousel.children, (item) => {
      const element = item as HTMLElement;
      return element.offsetLeft + element.offsetWidth / 2;
    });
    setActiveIndex(resolveCarouselActiveIndex(viewportCenter, itemCenters));
  };

  const scrollCarouselWithWheel = (event: WheelEvent<HTMLDivElement>) => {
    if (Math.abs(event.deltaY) <= Math.abs(event.deltaX)) return;

    event.preventDefault();
    event.currentTarget.scrollLeft += event.deltaY;
  };

  const handleJoin = async () => {
    if (!preview || isJoining || !selectionInitialized) return;

    setIsJoining(true);
    setError(null);
    try {
      const session = await joinParty({
        joinCode: code,
        watchlistId: choices[activeIndex]?.id ?? null,
      });
      setPartySession({
        partyId: session.partyId,
        playerToken: session.playerToken,
      });
      navigate(routePaths.partyPlay(session.partyId), { replace: true });
    } catch (joinError) {
      setError(getPartyErrorText(joinError, TextKeys.Party_Errors_Join));
    } finally {
      setIsJoining(false);
    }
  };

  if (!preview && !error) {
    return <StatusText>{text(TextKeys.Party_Loading)}</StatusText>;
  }

  return (
    <main className="party-start party-join">
      {preview ? (
        <>
          <StatusText>{text(TextKeys.Party_ChooseWatchlist)}</StatusText>
          <div
            ref={carouselRef}
            className="party-start__carousel"
            role="region"
            aria-label={text(TextKeys.Party_WatchlistCarousel)}
            onScroll={updateActiveIndex}
            onWheel={scrollCarouselWithWheel}
          >
            {choices.map((choice, index) => {
              const isActive = index === activeIndex;
              return (
                <div
                  className={`party-start__choice${
                    isActive ? " party-start__choice--active" : ""
                  }`}
                  key={choice.id ?? "all"}
                >
                  <PopcornBucket
                    className={`party-start__watchlist${
                      isActive ? " party-start__watchlist--active" : ""
                    }`}
                    art={popcornFull}
                    label={choice.name}
                    size={isActive ? "m" : "ms"}
                    frame={COMPACT_LABEL_FRAME}
                    labelCenter="58%"
                    disabled={isJoining}
                    ariaLabel={choice.name}
                    ariaPressed={isActive}
                    onClick={() => {
                      setActiveIndex(index);
                    }}
                  />
                </div>
              );
            })}
          </div>
          <PartyButton
            className="party-start__action"
            label={text(TextKeys.Party_Join)}
            size="ms"
            disabled={isJoining || !selectionInitialized}
            onClick={() => {
              void handleJoin();
            }}
          />
        </>
      ) : null}
      <ErrorText className="party-start__error">{error}</ErrorText>
    </main>
  );
};
