import { Button } from "../primitives/Button";
import type { ButtonProps } from "../primitives/Button";
import "./ImageButton.scss";

export type ImageButtonSize = "large" | "middle" | "small";

type DistributiveOmit<T, K extends PropertyKey> = T extends unknown ? Omit<T, K> : never;

export type ImageButtonProps = DistributiveOmit<
  ButtonProps,
  "aria-label" | "children" | "variant"
> & {
  label: string;
  size: ImageButtonSize;
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
    className={["image-button", `image-button--${size}`, className].filter(Boolean).join(" ")}
    aria-label={label}
  >
    <img src={art} alt="" draggable={false} />
  </Button>
);
