import { useEffect, useRef, useState } from "react";
import type { UIEvent } from "react";
import { useNavigate } from "react-router-dom";
import popcornFull from "../assets/popcorn-full.svg";
import { ApiError } from "../api/http";
import { createParty, joinParty, suggestPartyCode } from "../api/partyApi";
import { setPartySession } from "../api/partySession";
import "../components/WatchlistBucket.scss";
import {
  COMPACT_LABEL_FRAME,
  LARGE_LABEL_FRAME,
} from "../components/labelPill";
import { PartyBucket } from "../components/PartyBucket";
import { PopcornBucket } from "../components/PopcornBucket";
import { text } from "../i18n/text";
import { ErrorText } from "../primitives/ErrorText";
import { routePaths } from "../routes/routePaths";
import { useAppState } from "../state/AppStateContext";
import "./PartyStartPage.scss";

export const resolveCarouselActiveIndex = (
  viewportCenter: number,
  itemCenters: readonly number[]
): number => {
  if (itemCenters.length === 0) {
    return 0;
  }

  let closestIndex = 0;
  let closestDistance = Math.abs(itemCenters[0] - viewportCenter);

  for (let index = 1; index < itemCenters.length; index += 1) {
    const distance = Math.abs(itemCenters[index] - viewportCenter);
    if (distance < closestDistance) {
      closestIndex = index;
      closestDistance = distance;
    }
  }

  return closestIndex;
};

const sanitizePartyCode = (value: string): string =>
  value.replace(/\D/g, "").slice(0, 4);

export const PartyStartPage = () => {
  const { watchlists } = useAppState();
  const navigate = useNavigate();
  const carouselRef = useRef<HTMLDivElement>(null);
  const [activeIndex, setActiveIndex] = useState(0);
  const [joinCode, setJoinCode] = useState("");
  const [suggestedCode, setSuggestedCode] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const isJoining = joinCode.length > 0;

  useEffect(() => {
    let cancelled = false;

    const loadSuggestion = async () => {
      try {
        const suggestion = await suggestPartyCode();
        if (!cancelled) {
          setSuggestedCode(suggestion.joinCode);
        }
      } catch (loadError) {
        if (!cancelled) {
          setError(
            loadError instanceof Error
              ? loadError.message
              : text("party.errors.suggestion")
          );
        }
      }
    };

    void loadSuggestion();
    return () => {
      cancelled = true;
    };
  }, []);

  const updateActiveIndex = (event: UIEvent<HTMLDivElement>) => {
    const carousel = event.currentTarget;
    const viewportCenter = carousel.scrollLeft + carousel.clientWidth / 2;
    const itemCenters = Array.from(carousel.children, (item) => {
      const element = item as HTMLElement;
      return element.offsetLeft + element.offsetWidth / 2;
    });
    setActiveIndex(resolveCarouselActiveIndex(viewportCenter, itemCenters));
  };

  const handleSubmit = async () => {
    if (isSubmitting || (!isJoining && !suggestedCode)) {
      return;
    }

    setIsSubmitting(true);
    setError(null);

    try {
      const session = isJoining
        ? await joinParty(joinCode)
        : await createParty({
            watchlistId: activeIndex === 0 ? null : watchlists[activeIndex - 1]?.id,
            joinCode: suggestedCode,
          });

      setPartySession({
        partyId: session.partyId,
        playerToken: session.playerToken,
      });
      navigate(routePaths.partyPlay(session.partyId));
    } catch (submitError) {
      if (submitError instanceof ApiError && submitError.code === "CodeTaken") {
        try {
          const suggestion = await suggestPartyCode();
          setSuggestedCode(suggestion.joinCode);
          setError(text("party.errors.codeTaken"));
        } catch (suggestionError) {
          setError(
            suggestionError instanceof Error
              ? suggestionError.message
              : text("party.errors.suggestion")
          );
        }
      } else if (
        submitError instanceof ApiError &&
        submitError.code === "InvalidCode"
      ) {
        setError(text("party.errors.invalidCode"));
      } else {
        setError(
          submitError instanceof Error
            ? submitError.message
            : text("party.errors.action")
        );
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const choices = [
    { id: null, name: text("party.allMovies") },
    ...watchlists.map(({ id, name }) => ({ id, name })),
  ];

  return (
    <main className="party-start">
      <div
        ref={carouselRef}
        className={`party-start__carousel${
          isJoining ? " party-start__carousel--disabled" : ""
        }`}
        role="region"
        aria-label={text("party.watchlistCarousel")}
        aria-disabled={isJoining}
        onScroll={updateActiveIndex}
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
                className={`watchlist-bucket watchlist-bucket--${
                  isActive ? "large" : "compact"
                }`}
                art={popcornFull}
                label={choice.name}
                frame={isActive ? LARGE_LABEL_FRAME : COMPACT_LABEL_FRAME}
                labelCenter="58%"
                disabled={isJoining}
                ariaLabel={choice.name}
                ariaPressed={isActive}
                onClick={(event) => {
                  setActiveIndex(index);
                  event.currentTarget.parentElement?.scrollIntoView({
                    behavior: "smooth",
                    inline: "center",
                    block: "nearest",
                  });
                }}
              />
            </div>
          );
        })}
      </div>

      <input
        className="party-start__code"
        aria-label={text("party.codeLabel")}
        inputMode="numeric"
        maxLength={4}
        value={joinCode}
        placeholder={suggestedCode}
        onChange={(event) => {
          setJoinCode(sanitizePartyCode(event.currentTarget.value));
          setError(null);
        }}
      />

      <ErrorText className="party-start__error">{error}</ErrorText>

      <PartyBucket
        label={text(isJoining ? "party.join" : "party.create")}
        ariaLabel={text(isJoining ? "party.join" : "party.create")}
        disabled={isSubmitting || (!isJoining && !suggestedCode)}
        onClick={() => {
          void handleSubmit();
        }}
      />
    </main>
  );
};
