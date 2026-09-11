import { useEffect, useState, type CSSProperties } from "react";
import "./Fireworks.scss";

export interface FireworksProps {
  durationMs?: number;
  fadeMs?: number;
}

type FireworksPhase = "running" | "fading" | "hidden";

const prefersReducedMotion = () =>
  typeof window !== "undefined" &&
  typeof window.matchMedia === "function" &&
  window.matchMedia("(prefers-reduced-motion: reduce)").matches;

export const Fireworks = ({
  durationMs = 5000,
  fadeMs = 1000,
}: FireworksProps) => {
  const [phase, setPhase] = useState<FireworksPhase>("running");
  const reducedMotion = prefersReducedMotion();

  useEffect(() => {
    if (reducedMotion) return;

    let currentPhase: FireworksPhase = "running";
    let fadeTimer: number | undefined;

    const finish = () => {
      currentPhase = "hidden";
      setPhase("hidden");
      window.removeEventListener("pointerdown", dismiss);
    };

    const dismiss = () => {
      if (currentPhase !== "running") return;

      currentPhase = "fading";
      setPhase("fading");
      window.clearTimeout(durationTimer);
      fadeTimer = window.setTimeout(finish, Math.max(0, fadeMs));
    };

    const durationTimer = window.setTimeout(
      dismiss,
      Math.max(0, durationMs)
    );
    window.addEventListener("pointerdown", dismiss);

    return () => {
      window.clearTimeout(durationTimer);
      if (fadeTimer !== undefined) window.clearTimeout(fadeTimer);
      window.removeEventListener("pointerdown", dismiss);
    };
  }, [durationMs, fadeMs, reducedMotion]);

  if (reducedMotion || phase === "hidden") return null;

  return (
    <div
      className={`fireworks${phase === "fading" ? " fireworks--fading" : ""}`}
      aria-hidden="true"
      style={
        {
          "--fireworks-fade-duration": `${Math.max(0, fadeMs)}ms`,
        } as CSSProperties
      }
    >
      <span className="fireworks__burst fireworks__burst--first" />
      <span className="fireworks__burst fireworks__burst--second" />
    </div>
  );
};
