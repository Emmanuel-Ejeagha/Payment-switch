import type { Config } from "tailwindcss"
import defaultTheme from "tailwindcss/defaultTheme"

const config: Config = {
  content: ["./src/**/*.{ts,tsx}", "../../packages/**/src/**/*.{ts,tsx}"],
  darkMode: "class",
  theme: {
    extend: {
      fontFamily: {
        // --font-sans is supplied by next/font in the root layout. The system
        // stack stays behind it so a font that fails to load degrades cleanly.
        sans: ["var(--font-sans)", ...defaultTheme.fontFamily.sans],
      },
      colors: {
        border: "hsl(var(--border))",
        input: "hsl(var(--input))",
        ring: "hsl(var(--ring))",
        background: "hsl(var(--background))",
        foreground: "hsl(var(--foreground))",
        primary: {
          DEFAULT: "hsl(var(--primary))",
          foreground: "hsl(var(--primary-foreground))",
        },
        secondary: {
          DEFAULT: "hsl(var(--secondary))",
          foreground: "hsl(var(--secondary-foreground))",
        },
        destructive: {
          DEFAULT: "hsl(var(--destructive))",
          foreground: "hsl(var(--destructive-foreground))",
        },
        muted: {
          DEFAULT: "hsl(var(--muted))",
          foreground: "hsl(var(--muted-foreground))",
        },
        accent: {
          DEFAULT: "hsl(var(--accent))",
          foreground: "hsl(var(--accent-foreground))",
        },
        card: {
          DEFAULT: "hsl(var(--card))",
          foreground: "hsl(var(--card-foreground))",
        },
        // Dashboard canvas behind cards. Additive; nothing else references it.
        surface: "hsl(var(--surface))",
        // Marketing/auth accents. Additive — the dashboard tokens above are
        // untouched, so existing screens keep their current appearance.
        brand: {
          start: "hsl(var(--brand-start))",
          mid: "hsl(var(--brand-mid))",
          end: "hsl(var(--brand-end))",
        },
        success: {
          DEFAULT: "hsl(var(--success))",
          foreground: "hsl(var(--success-foreground))",
        },
      },
      borderRadius: {
        lg: "var(--radius)",
        md: "calc(var(--radius) - 2px)",
        sm: "calc(var(--radius) - 4px)",
        "2xl": "calc(var(--radius) + 8px)",
        "3xl": "calc(var(--radius) + 16px)",
      },
      boxShadow: {
        subtle: "0 1px 2px 0 hsl(var(--foreground) / 0.04)",
        card: "0 1px 3px 0 hsl(var(--foreground) / 0.06), 0 8px 24px -12px hsl(var(--foreground) / 0.10)",
        lift: "0 2px 4px 0 hsl(var(--foreground) / 0.06), 0 24px 48px -20px hsl(var(--foreground) / 0.22)",
        glow: "0 0 0 1px hsl(var(--primary) / 0.18), 0 18px 40px -18px hsl(var(--primary) / 0.45)",
      },
      keyframes: {
        "fade-up": {
          from: { opacity: "0", transform: "translateY(12px)" },
          to: { opacity: "1", transform: "translateY(0)" },
        },
        "fade-in": {
          from: { opacity: "0" },
          to: { opacity: "1" },
        },
        float: {
          "0%, 100%": { transform: "translateY(0)" },
          "50%": { transform: "translateY(-8px)" },
        },
        "pulse-ring": {
          "0%": { opacity: "0.7", transform: "scale(0.85)" },
          "70%, 100%": { opacity: "0", transform: "scale(1.9)" },
        },
      },
      animation: {
        "fade-up": "fade-up 0.6s cubic-bezier(0.16, 1, 0.3, 1) both",
        "fade-in": "fade-in 0.5s ease-out both",
        float: "float 6s ease-in-out infinite",
        "pulse-ring": "pulse-ring 2.4s cubic-bezier(0.24, 0, 0.38, 1) infinite",
      },
    },
  },
  plugins: [],
}

export default config
