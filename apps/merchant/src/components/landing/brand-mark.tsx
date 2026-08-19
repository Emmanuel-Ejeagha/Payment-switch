/**
 * The PaymentSwitch glyph: two crossing routes through a switch, drawn as SVG so
 * it stays crisp at any size and needs no asset pipeline or `public/` directory.
 */
export function BrandMark({ className = "h-8 w-8" }: { className?: string }) {
  return (
    <span
      className={`inline-flex items-center justify-center rounded-lg bg-brand-gradient text-white shadow-subtle ${className}`}
      aria-hidden="true"
    >
      <svg viewBox="0 0 24 24" fill="none" className="h-[62%] w-[62%]">
        <path
          d="M4 8h9a4 4 0 0 1 4 4v0"
          stroke="currentColor"
          strokeWidth="2.1"
          strokeLinecap="round"
        />
        <path
          d="M20 16h-9a4 4 0 0 1-4-4v0"
          stroke="currentColor"
          strokeWidth="2.1"
          strokeLinecap="round"
        />
        <path
          d="m17 5 3 3-3 3"
          stroke="currentColor"
          strokeWidth="2.1"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <path
          d="m7 19-3-3 3-3"
          stroke="currentColor"
          strokeWidth="2.1"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
    </span>
  )
}
