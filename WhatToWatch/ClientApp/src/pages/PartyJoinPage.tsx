import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { getPartyPreview, joinParty } from "../api/partyApi";
import { setPartySession } from "../api/partySession";
import { text } from "../i18n/text";
import { ErrorText } from "../primitives/ErrorText";
import { StatusText } from "../primitives/StatusText";
import { routePaths } from "../routes/routePaths";
import { getPartyErrorText } from "./partyText";
import { TextKeys } from "../i18n/textKeys";

export const PartyJoinPage = () => {
  const { code = "" } = useParams();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const join = async () => {
      try {
        await getPartyPreview(code);
        const session = await joinParty(code);
        if (cancelled) return;

        setPartySession({
          partyId: session.partyId,
          playerToken: session.playerToken,
        });
        navigate(routePaths.partyPlay(session.partyId), { replace: true });
      } catch (joinError) {
        if (cancelled) return;

        setError(getPartyErrorText(joinError, TextKeys.Party_Errors_Join));
      }
    };

    void join();
    return () => {
      cancelled = true;
    };
  }, [code, navigate]);

  return (
    <main className="party-join">
      {!error && <StatusText>{text("party.joining")}</StatusText>}
      <ErrorText>{error}</ErrorText>
    </main>
  );
};
