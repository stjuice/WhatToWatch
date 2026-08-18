import { useEffect, useState } from "react";

const ZOOM_IN_AT = 1.2;
const ZOOM_OUT_AT = 1.1;

/** Hysteresis keeps the layout from flipping while a pinch hovers around the threshold. */
export const resolveZoomedIn = (scale: number, wasZoomedIn: boolean): boolean => {
  if (!Number.isFinite(scale) || scale <= 0) {
    return wasZoomedIn;
  }

  return wasZoomedIn ? scale >= ZOOM_OUT_AT : scale >= ZOOM_IN_AT;
};

export const useViewportZoom = (): boolean => {
  const [zoomedIn, setZoomedIn] = useState(false);

  useEffect(() => {
    const viewport = window.visualViewport;
    if (!viewport) {
      return;
    }

    const sync = () => {
      setZoomedIn((was) => resolveZoomedIn(viewport.scale, was));
    };

    sync();
    viewport.addEventListener("resize", sync);
    viewport.addEventListener("scroll", sync);

    return () => {
      viewport.removeEventListener("resize", sync);
      viewport.removeEventListener("scroll", sync);
    };
  }, []);

  return zoomedIn;
};
