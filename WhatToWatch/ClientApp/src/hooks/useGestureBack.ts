import { useCallback, useEffect, useRef } from "react";
import { useNavigate } from "react-router-dom";

const EDGE_START_PX = 80;
const MIN_SWIPE_DX = 64;
const MAX_SWIPE_SLOPE = 0.6;

type Options = {
  enabled: boolean;
  fallbackTo: string;
};

const canUseHistoryBack = (): boolean => {
  const idx = window.history.state?.idx;
  return typeof idx === "number" && idx > 0;
};

export const useGestureBack = ({ enabled, fallbackTo }: Options) => {
  const navigate = useNavigate();
  const startRef = useRef<{ x: number; y: number } | null>(null);

  const goBack = useCallback(() => {
    if (canUseHistoryBack()) {
      navigate(-1);
      return;
    }
    navigate(fallbackTo);
  }, [fallbackTo, navigate]);

  useEffect(() => {
    if (!enabled) {
      return;
    }

    const onTouchStart = (event: TouchEvent) => {
      const touch = event.touches[0];
      if (!touch || touch.clientX > EDGE_START_PX) {
        startRef.current = null;
        return;
      }
      startRef.current = { x: touch.clientX, y: touch.clientY };
    };

    const onTouchEnd = (event: TouchEvent) => {
      const start = startRef.current;
      startRef.current = null;
      if (!start) {
        return;
      }

      const touch = event.changedTouches[0];
      if (!touch) {
        return;
      }

      const dx = touch.clientX - start.x;
      const dy = Math.abs(touch.clientY - start.y);
      if (dx >= MIN_SWIPE_DX && dy < dx * MAX_SWIPE_SLOPE) {
        goBack();
      }
    };

    const onTouchCancel = () => {
      startRef.current = null;
    };

    window.addEventListener("touchstart", onTouchStart, { passive: true });
    window.addEventListener("touchend", onTouchEnd);
    window.addEventListener("touchcancel", onTouchCancel);

    return () => {
      window.removeEventListener("touchstart", onTouchStart);
      window.removeEventListener("touchend", onTouchEnd);
      window.removeEventListener("touchcancel", onTouchCancel);
    };
  }, [enabled, goBack]);
};
