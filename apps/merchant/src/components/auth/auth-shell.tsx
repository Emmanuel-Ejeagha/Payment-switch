import Image from "next/image"
import Link from "next/link"
import type { ReactNode } from "react"
import { Check } from "lucide-react"
import { BrandMark } from "@/components/landing/brand-mark"

/** Shared input styling so login and register cannot drift apart. */
export const inputClass =
  "flex h-11 w-full rounded-lg border border-input bg-background px-3.5 text-sm shadow-subtle transition-colors placeholder:text-muted-foreground/70 hover:border-ring/40 focus-visible:border-ring focus-visible:outline-none aria-[invalid=true]:border-destructive"

export const labelClass = "block text-sm font-medium text-foreground"

export const submitClass =
  "inline-flex h-11 w-full items-center justify-center gap-2 rounded-lg bg-primary px-4 text-sm font-medium text-primary-foreground shadow-glow transition-colors hover:bg-primary/90 disabled:pointer-events-none disabled:opacity-60"

interface AuthShellProps {
  /** Full-bleed artwork for the brand panel. */
  image: { src: string; alt: string }
  panelHeading: string
  panelBody: string
  /** Optional selling points rendered as a checklist under the body copy. */
  highlights?: readonly string[]
  /** Optional metric shown in the glass card over the artwork. */
  metric?: { label: string; value: string }
  heading: string
  subheading: string
  children: ReactNode
  /** Rendered under the form — the cross-link to the other auth page. */
  footer: ReactNode
}

export function AuthShell({
  image,
  panelHeading,
  panelBody,
  highlights,
  metric,
  heading,
  subheading,
  children,
  footer,
}: AuthShellProps) {
  return (
    <div className="flex min-h-screen bg-background">
      {/* Brand panel. Hidden below lg — the photo is decoration, not content. */}
      <aside className="relative hidden w-1/2 shrink-0 overflow-hidden bg-slate-950 lg:flex xl:w-[55%]">
        <Image
          src={image.src}
          alt=""
          fill
          priority
          unoptimized
          sizes="55vw"
          className="object-cover opacity-45"
        />
        {/* Scrim keeps text contrast fixed regardless of the photo behind it. */}
        <div
          className="absolute inset-0 bg-gradient-to-br from-slate-950/95 via-slate-950/75 to-indigo-950/85"
          aria-hidden="true"
        />
        <div
          className="absolute inset-0 bg-grid opacity-[0.07]"
          aria-hidden="true"
        />

        <div className="relative z-10 flex w-full flex-col justify-between p-10 xl:p-14">
          <Link
            href="/"
            className="flex w-fit items-center gap-2.5 text-white"
            aria-label="PaymentSwitch home"
          >
            <BrandMark />
            <span className="text-[15px] font-semibold tracking-tight">PaymentSwitch</span>
          </Link>

          <div className="max-w-md">
            <h2 className="text-4xl font-semibold leading-[1.12] tracking-tight text-white">
              {panelHeading}
            </h2>
            <p className="mt-4 text-[15px] leading-relaxed text-slate-300">{panelBody}</p>

            {highlights && highlights.length > 0 && (
              <ul className="mt-8 space-y-3">
                {highlights.map((h) => (
                  <li key={h} className="flex items-center gap-3 text-sm text-slate-200">
                    <span className="inline-flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-emerald-500/15 ring-1 ring-emerald-400/30">
                      <Check className="h-3 w-3 text-emerald-400" aria-hidden="true" />
                    </span>
                    {h}
                  </li>
                ))}
              </ul>
            )}

            {metric && (
              <div className="mt-8 w-fit rounded-xl border border-white/10 bg-white/[0.06] p-4 backdrop-blur">
                <p className="text-[11px] uppercase tracking-wider text-slate-400">
                  {metric.label}
                </p>
                <p className="tabular mt-1 text-2xl font-semibold text-white">
                  {metric.value}
                </p>
              </div>
            )}
          </div>

          <p className="text-xs text-slate-500">
            &copy; {new Date().getFullYear()} PaymentSwitch. All rights reserved.
          </p>
        </div>
      </aside>

      <main className="flex w-full flex-col lg:w-1/2 xl:w-[45%]">
        {/* Compact header stands in for the brand panel on small screens. */}
        <div className="flex items-center justify-between px-6 py-6 lg:hidden">
          <Link
            href="/"
            className="flex items-center gap-2.5"
            aria-label="PaymentSwitch home"
          >
            <BrandMark className="h-7 w-7" />
            <span className="text-sm font-semibold tracking-tight">PaymentSwitch</span>
          </Link>
        </div>

        <div className="flex flex-1 items-center justify-center px-6 pb-12 sm:px-8 lg:py-12">
          <div className="w-full max-w-sm">
            <div className="space-y-2">
              <h1 className="text-2xl font-semibold tracking-tight sm:text-3xl">
                {heading}
              </h1>
              <p className="text-sm text-muted-foreground">{subheading}</p>
            </div>

            <div className="mt-8">{children}</div>

            <div className="mt-8 text-center text-sm text-muted-foreground">{footer}</div>
          </div>
        </div>
      </main>
    </div>
  )
}
