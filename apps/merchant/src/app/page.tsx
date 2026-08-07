import { LandingNav } from "@/components/landing/landing-nav"
import { LandingFooter } from "@/components/landing/landing-footer"
import {
  LandingHero,
  LandingStats,
  LandingFeatures,
  LandingHowItWorks,
  LandingPricing,
  LandingCta,
} from "@/components/landing/landing-sections"

export default function LandingPage() {
  return (
    <div className="flex min-h-screen flex-col bg-background">
      <LandingNav />
      <main className="flex-1">
        <LandingHero />
        <LandingStats />
        <LandingFeatures />
        <LandingHowItWorks />
        <LandingPricing />
        <LandingCta />
      </main>
      <LandingFooter />
    </div>
  )
}
