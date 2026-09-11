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
import { TextKeys } from "../i18n/textKeys";
import { Fireworks } from "../components/Fireworks";
import { Button } from "../primitives/Button";
import { ErrorText } from "../primitives/ErrorText";
import { StatusText } from "../primitives/StatusText";
import { routePaths } from "../routes/routePaths";
import { PartyStatuses, type PartyState } from "../types/party";
import { MoviePage } from "./MoviePage";
import {
  getPartyErrorText,
  partyErrorCodes,
  partyErrorTextKeys,
} from "./partyText";
import "./PartyPlayPage.scss";

type TerminalView =
  | typeof TextKeys.Party_Expired
  | typeof TextKeys.Party_Errors_Finished
  | typeof TextKeys.Party_Errors_InvalidPlayerToken
  | typeof TextKeys.Party_SessionMissing
  | null;

export const PartyPlayPage = () => {
  const { partyId = "" } = useParams();
  const [state, setState] = useState<PartyState | null>(null);
  const [terminalView, setTerminalView] = useState<TerminalView>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isVoting, setIsVoting] = useState(false);

  const refresh = useCallback(async () => {
    const session = getPartySession();
    if (!session || session.partyId !== partyId) {
      setTerminalView(TextKeys.Party_SessionMissing);
      setIsLoading(false);
      return;
    }

    setError(null);
    setIsLoading(true);
    try {
      const nextState = await getPartyState(partyId, session.playerToken);
      setState(nextState);
      setTerminalView(null);
      if (
        nextState.status === PartyStatuses.Finished ||
        nextState.status === PartyStatuses.Expired
      ) {
        clearPartySession();
      }
    } catch (loadError) {
      if (
        loadError instanceof ApiError &&
        (loadError.code === partyErrorCodes.PartyExpired ||
          loadError.code === partyErrorCodes.AlreadyFinished ||
          loadError.code === partyErrorCodes.InvalidPlayerToken)
      ) {
        clearPartySession();
        setTerminalView(
          loadError.code === partyErrorCodes.PartyExpired
            ? partyErrorTextKeys.PartyExpired
            : loadError.code === partyErrorCodes.AlreadyFinished
              ? partyErrorTextKeys.AlreadyFinished
              : partyErrorTextKeys.InvalidPlayerToken
        );
      } else {
        setError(getPartyErrorText(loadError, TextKeys.Party_Errors_Load));
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
      if (
        nextState.status === PartyStatuses.Finished ||
        nextState.status === PartyStatuses.Expired
      ) {
        clearPartySession();
      }
    } catch (voteError) {
      if (
        voteError instanceof ApiError &&
        (voteError.code === partyErrorCodes.PartyExpired ||
          voteError.code === partyErrorCodes.AlreadyFinished)
      ) {
        clearPartySession();
        setTerminalView(
          voteError.code === partyErrorCodes.PartyExpired
            ? partyErrorTextKeys.PartyExpired
            : partyErrorTextKeys.AlreadyFinished
        );
      } else {
        setError(getPartyErrorText(voteError, TextKeys.Party_Errors_Vote));
      }
    } finally {
      setIsVoting(false);
    }
  };

  if (isLoading && !state) {
    return <StatusText>{text(TextKeys.Party_Loading)}</StatusText>;
  }

  if (terminalView) {
    return <PartyTerminal message={text(terminalView)} />;
  }

  if (!state) {
    return (
      <main className="party-play party-play--terminal">
        <ErrorText>{error}</ErrorText>
        <Button onClick={() => void refresh()}>{text(TextKeys.Party_Refresh)}</Button>
      </main>
    );
  }

  if (state.status === PartyStatuses.Matched && state.matchedMovie) {
    return (
      <main className="party-play party-play--matched">
        <h1 className="party-play__heading">{text(TextKeys.Party_Matched)}</h1>
        <MoviePage movie={state.matchedMovie} />
        <Fireworks />
      </main>
    );
  }

  if (state.status === PartyStatuses.Finished) {
    return <PartyTerminal message={text(TextKeys.Party_Finished)} />;
  }

  if (state.status === PartyStatuses.Expired) {
    return <PartyTerminal message={text(TextKeys.Party_Expired)} />;
  }

  if (!state.currentMovie || state.progress.isExhausted) {
    return (
      <main className="party-play party-play--terminal">
        <StatusText>{text(TextKeys.Party_Exhausted)}</StatusText>
        <Button disabled={isLoading} onClick={() => void refresh()}>
          {text(TextKeys.Party_Refresh)}
        </Button>
        <ErrorText>{error}</ErrorText>
      </main>
    );
  }

  return (
    <main className="party-play">
      <h1 className="party-play__heading party-play__heading--code">
        {text(TextKeys.Party_PlayCode, { code: state.joinCode })}
      </h1>
      {!state.opponentPresent || !state.opponentOnline ? (
        <StatusText className="party-play__presence">
          {text(TextKeys.Party_WaitingForOpponent)}
        </StatusText>
      ) : null}
      <div className="party-play__card">
        <MoviePage movie={state.currentMovie} showImdbLink={false} />
        <Button
          variant="icon"
          className="party-play__vote party-play__vote--no"
          aria-label={text(TextKeys.Party_VoteNo)}
          disabled={isVoting}
          onClick={() => void vote(false)}
        >
          <img src={noHeart} alt="" draggable={false} />
        </Button>
        <Button
          variant="icon"
          className="party-play__vote party-play__vote--yes"
          aria-label={text(TextKeys.Party_VoteYes)}
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
    <Button to={routePaths.party}>{text(TextKeys.Party_Back)}</Button>
  </main>
);
