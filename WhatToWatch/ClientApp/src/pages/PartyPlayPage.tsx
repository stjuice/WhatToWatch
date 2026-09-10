import { useCallback, useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import noHeart from "../assets/no.svg";
import yesHeart from "../assets/yes.svg";
import { ApiError } from "../api/http";
import { getPartyState, voteInParty } from "../api/partyApi";
import {
  clearPartySession,
  getPartySession,
} from "../api/partySession";
import { text } from "../i18n/text";
import { Button } from "../primitives/Button";
import { ErrorText } from "../primitives/ErrorText";
import { StatusText } from "../primitives/StatusText";
import { routePaths } from "../routes/routePaths";
import type { PartyStateDto } from "../types/party";
import { MoviePage } from "./MoviePage";
import "./PartyPlayPage.scss";

type TerminalView = "expired" | "sessionMissing" | null;

export const PartyPlayPage = () => {
  const { partyId = "" } = useParams();
  const [state, setState] = useState<PartyStateDto | null>(null);
  const [terminalView, setTerminalView] = useState<TerminalView>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isVoting, setIsVoting] = useState(false);

  const refresh = useCallback(async () => {
    const session = getPartySession();
    if (!session || session.partyId !== partyId) {
      setTerminalView("sessionMissing");
      setIsLoading(false);
      return;
    }

    setError(null);
    setIsLoading(true);
    try {
      const nextState = await getPartyState(partyId, session.playerToken);
      setState(nextState);
      setTerminalView(null);
      if (nextState.status === "Finished" || nextState.status === "Expired") {
        clearPartySession();
      }
    } catch (loadError) {
      if (
        loadError instanceof ApiError &&
        (loadError.code === "PartyExpired" ||
          loadError.code === "AlreadyFinished" ||
          loadError.code === "InvalidPlayerToken")
      ) {
        clearPartySession();
        setTerminalView("expired");
      } else {
        setError(
          loadError instanceof Error
            ? loadError.message
            : text("party.errors.load")
        );
      }
    } finally {
      setIsLoading(false);
    }
  }, [partyId]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const vote = async (liked: boolean) => {
    const session = getPartySession();
    const movie = state?.currentMovie;
    if (!session || !movie || isVoting) return;

    setIsVoting(true);
    setError(null);
    try {
      const nextState = await voteInParty(partyId, session.playerToken, {
        movieId: movie.id,
        liked,
      });
      setState(nextState);
      if (nextState.status === "Finished" || nextState.status === "Expired") {
        clearPartySession();
      }
    } catch (voteError) {
      if (
        voteError instanceof ApiError &&
        (voteError.code === "PartyExpired" ||
          voteError.code === "AlreadyFinished")
      ) {
        clearPartySession();
        setTerminalView("expired");
      } else {
        setError(
          voteError instanceof Error
            ? voteError.message
            : text("party.errors.vote")
        );
      }
    } finally {
      setIsVoting(false);
    }
  };

  if (isLoading && !state) {
    return <StatusText>{text("party.loading")}</StatusText>;
  }

  if (terminalView) {
    return (
      <PartyTerminal
        message={text(
          terminalView === "expired"
            ? "party.expired"
            : "party.sessionMissing"
        )}
      />
    );
  }

  if (!state) {
    return (
      <main className="party-play party-play--terminal">
        <ErrorText>{error}</ErrorText>
        <Button onClick={() => void refresh()}>{text("party.refresh")}</Button>
      </main>
    );
  }

  if (state.status === "Matched" && state.matchedMovie) {
    return (
      <main className="party-play party-play--matched">
        <h1 className="party-play__heading">{text("party.matched")}</h1>
        <MoviePage movie={state.matchedMovie} />
      </main>
    );
  }

  if (state.status === "Finished") {
    return <PartyTerminal message={text("party.finished")} />;
  }

  if (state.status === "Expired") {
    return <PartyTerminal message={text("party.expired")} />;
  }

  if (!state.currentMovie || state.progress.isExhausted) {
    return (
      <main className="party-play party-play--terminal">
        <StatusText>{text("party.exhausted")}</StatusText>
        <Button disabled={isLoading} onClick={() => void refresh()}>
          {text("party.refresh")}
        </Button>
        <ErrorText>{error}</ErrorText>
      </main>
    );
  }

  return (
    <main className="party-play">
      <p className="party-play__code">
        {text("party.playCode", { code: state.joinCode })}
      </p>
      {!state.opponentPresent || !state.opponentOnline ? (
        <StatusText className="party-play__presence">
          {text("party.waitingForOpponent")}
        </StatusText>
      ) : null}
      <div className="party-play__card">
        <MoviePage movie={state.currentMovie} showImdbLink={false} />
        <Button
          variant="icon"
          className="party-play__vote party-play__vote--no"
          aria-label={text("party.voteNo")}
          disabled={isVoting}
          onClick={() => void vote(false)}
        >
          <img src={noHeart} alt="" draggable={false} />
        </Button>
        <Button
          variant="icon"
          className="party-play__vote party-play__vote--yes"
          aria-label={text("party.voteYes")}
          disabled={isVoting}
          onClick={() => void vote(true)}
        >
          <img src={yesHeart} alt="" draggable={false} />
        </Button>
      </div>
      <ErrorText>{error}</ErrorText>
    </main>
  );
};

const PartyTerminal = ({ message }: { message: string }) => (
  <main className="party-play party-play--terminal">
    <StatusText>{message}</StatusText>
    <Button to={routePaths.party}>{text("party.back")}</Button>
  </main>
);
