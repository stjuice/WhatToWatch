import { Button } from "../primitives/Button";
import type { ButtonProps } from "../primitives/Button";
import { artworkSizeClass, type ArtworkSize } from "./artworkSize";
import "./ArtworkSize.scss";
import "./ImageButton.scss";

type DistributiveOmit<T, K extends PropertyKey> = T extends unknown ? Omit<T, K> : never;

export type ImageButtonProps = DistributiveOmit<
  ButtonProps,
  "aria-label" | "children" | "variant"
> & {
  label: string;
  size: ArtworkSize;
};

type ImageButtonInternalProps = ImageButtonProps & {
  art: string;
};

export const ImageButton = ({
  art,
  label,
  size,
  className,
  ...buttonProps
}: ImageButtonInternalProps) => (
  <Button
    {...buttonProps}
    variant="icon"
    className={["image-button", artworkSizeClass(size), className].filter(Boolean).join(" ")}
    aria-label={label}
  >
    <img src={art} alt="" draggable={false} />
  </Button>
);
