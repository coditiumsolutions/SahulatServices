# SEO Report: Sahulat Ghar Tak

Researched 2026-10-08. Companion to [GEO-Report.md](GEO-Report.md).

**Goal:** get sahulatghartak.com to show up in Google Search, including the AI Overviews / AI Mode / Gemini surfaces that sit on top of it.

## Implementation status (2026-10-08)

Done in the codebase (uncommitted at the time of writing): home title and description for Islamabad / Rawalpindi / Bahria Town; coverage wording aligned in the footer, FAQ and schema; live provider, category and zone counts replacing the hard-coded 500+ / 18+ / 24/7 stats (cached 10 minutes); richer `Organization` and `WebSite` schema (cities, contact point, store links, no `priceRange`); AI-crawler rules in `robots.txt` (search bots allowed, `GPTBot` / `ClaudeBot` / `CCBot` blocked); `/about` added to the sitemap; Meta Pixel moved to the shared layout (ID overridable with `Marketing:MetaPixelId`) with a `Lead` event on Google Play / App Store clicks.
Still open: Google Business Profile, Search Console and Bing setup, privacy-policy mention of the Pixel, service landing pages, dynamic sitemap, Core Web Vitals pass. Urdu: see [Urdu-Variants-Plan.md](Urdu-Variants-Plan.md).

## 1. Key finding: for Google's AI surfaces, GEO is SEO

Google's own guidance says AI Overviews and AI Mode have **no special eligibility requirements**. A page qualifies if it is indexed and snippet-eligible, as for normal Search. They run on the core ranking systems, using retrieval plus "query fan-out" (one question becomes several related sub-searches). Google says you do **not** need:

- `llms.txt` or other AI-specific files
- special "AI schema"
- chunking content into tiny pieces or rewriting it for AI
- manufactured brand mentions

What Google does recommend: helpful, original, people-first content ("non-commodity": real observations a generic page cannot give); crawlable HTML; good internal linking and page experience; text alongside images/video; structured data that matches visible text; and an up-to-date Google Business Profile.

So the SEO work below is also the Gemini / AI Overviews work. Non-Google chatbots need extra steps, covered in the GEO report.

## 2. Current state of the site (audit of the repo)

What exists and is good:

| Item | Where | Status |
|---|---|---|
| `robots.txt` with sitemap link, admin/API disallowed | `wwwroot/robots.txt` | OK |
| `sitemap.xml` | `wwwroot/sitemap.xml` | Static, 4 URLs, no `lastmod` |
| Title, description, canonical, OG and Twitter tags | `Views/Shared/_PublicLayout.cshtml` | OK, set per page via ViewData |
| Organization-style JSON-LD | `Views/Shared/_OrganizationSchema.cshtml` | `LocalBusiness`, thin (see below) |
| FAQ section plus `FAQPage` JSON-LD | `Views/Home/Index.cshtml` | OK |
| Server-rendered HTML, one H1, logical H2/H3 | `Views/Home/Index.cshtml` | OK |

Gaps found, highest impact first:

1. **The public site is one page.** Home, About, Download, Privacy and Delete-account are all there is. There is no page for "electrician in Bahria Town", "AC repair", "plumber Islamabad", and so on. Local service searches are matched to a specific service plus place page, so today the site can only compete on the brand name. This is the single biggest gap.
2. **No Google Business Profile** is mentioned anywhere in the repo. For "near me" and map-pack results it is the highest-leverage asset (see section 3.4).
3. **Thin `LocalBusiness` schema.** It has no address, no geo, no `sameAs` links, and `areaServed` is the whole country. `priceRange: "$$"` is a dollar sign for a PKR market; use a real range or drop it.
4. **Geographic claims disagree.** Schema, meta description and FAQ say "across Pakistan". The footer says "across Bahria Town", and the FAQ itself says "Bahria Town and other served areas". Search and AI systems cross-check these. Pick the true scope (the `Zone` configuration is the source of truth for where providers exist) and state it identically everywhere.
5. **Claims that need to be true and provable.** "500+ Verified Providers", "background-checked", "24/7 support", "no hidden charges". Hard-coded numbers go stale and invite trust problems. Either back them with data from the DB or soften them.
6. **Home page title is "Home - Sahulat Ghar Tak"** (`ViewData["Title"] = "Home"`). Use a keyword-bearing title, e.g. "Book Verified Electricians, Plumbers & AC Technicians in Bahria Town | Sahulat Ghar Tak".
7. **Sitemap omits `/about`** and has no `lastmod`; it is hand-maintained and will drift once more pages exist.
8. **`<meta name="keywords">`** is ignored by Google; harmless, not worth effort.
9. **No `hreflang` or Urdu content.** Pakistani users search in English, Urdu script and Roman Urdu, and often mix them with a city name.
10. **`robots.txt` prefix matching.** `Disallow: /Services`, `/Bookings`, `/Customers` etc. match by *prefix* (and case-sensitively). Any future public page under a path starting with `/Services...` would be blocked. Put new public landing pages under a clearly different lowercase prefix, e.g. `/services/...`, `/areas/...` and test with the robots tester.
11. **Third-party scripts.** The Meta Pixel and Bootstrap/Font Awesome/Google Fonts loads from CDNs affect page speed. Worth measuring (section 3.6).
12. **A Meta Pixel is already installed, but only as a bare page-view counter** (details and how to use it in section 3.9). It is not connected to any download-intent measurement.

## 3. Recommendations

### 3.1 Build indexable landing pages (do this first)

Generate them from data the app already has: `ServiceCategories`, `ServiceTitles` and the new `ProviderZones` / `Zone` configuration.

- `/services/{category}`: e.g. electrician, plumber, AC technician. Explain the service, typical jobs (from `ServiceTitles`), how booking works, what the estimate means, FAQs.
- `/services/{category}/{area}`: only where real providers exist in that zone. Do **not** mass-generate near-identical "doorway" pages for areas with no coverage; Google treats thin, templated pages as spam and AI systems ignore them.
- Each page needs unique content: real price ranges (the new `EstimateText` field fits this), typical response time, the areas actually served, photos of real work, and a short "how we vet providers" block.
- Link them from the home page, footer and each other (internal linking), and add them to the sitemap.
- Put the short, direct answer first, then detail: "How much does AC servicing cost in Bahria Town? Typically PKR X to Y." Google's guidance is to answer the main question before the nuance.

### 3.2 Make the sitemap dynamic

Replace the static file with a controller route that lists every public page, with accurate `lastmod` (use a real change date, not "now"). Keep the `Sitemap:` line in `robots.txt`. Submit it in Google Search Console and Bing Webmaster Tools.

### 3.3 Fix structured data

Structured data does not get you into AI Overviews, but it helps Google understand the entity and unlocks classic rich results. Keep it accurate and matching the visible page.

- Home: `Organization` (or `HomeAndConstructionBusiness` if there is a real service location) with `name`, `url`, `logo`, `contactPoint`, `sameAs` (Facebook, Instagram, Google Play, App Store, Google Business Profile, LinkedIn), and `areaServed` listing the real cities/zones instead of "Pakistan".
- Add a `MobileApplication` / `SoftwareApplication` block on the Download page.
- Service pages: `Service` with `provider`, `areaServed`, `serviceType`, and `offers` only where a real price is shown.
- Add `BreadcrumbList` on deeper pages.
- **Reviews:** only mark up `aggregateRating` or `Review` if the reviews are genuinely displayed on that page and are not written by you about yourself. Self-serving review markup breaks Google's policy and can cause a manual action.
- `FAQPage`: per secondary sources, Google retired the FAQ rich result in May 2026 (verify in Search Console docs). The markup is harmless and the visible FAQ is still valuable content, so keep it, but expect no dropdown in the results.
- Validate with the Schema.org validator and Search Console; the Rich Results Test is also being reduced for retired types.

### 3.4 Google Business Profile (highest local lever)

Home-services businesses with no storefront are "service-area businesses":

- Create the profile as a **service area** business; the address used for verification can be hidden from the public listing, but a real, verifiable address is still required.
- Verification for service-area businesses is usually by video: show street signs/landmarks at the address and proof of the business (equipment, branded clothing, cards).
- Choose the most specific primary category (and secondary categories for each trade), list services with descriptions, add photos regularly, and list the service areas.
- Keep **NAP** (name, address, phone) identical on the website, GBP, social pages and directories. The phone used in the schema (+92 322 5040823) must match everywhere.
- Ask real customers for reviews and reply to them. Reviews are a major ranking input for the map pack and a source AI systems quote.
- Google explicitly lists Business Profile information as something it uses for AI features, so this feeds Gemini answers about local services.

### 3.5 Content and E-E-A-T

- Publish a small number of genuinely useful, original pieces: price guides by job, "how to tell if your AC needs a gas refill", seasonal checklists (monsoon wiring/plumbing, summer AC), with real photos and the author or reviewing technician named.
- Add proof of experience: provider profiles (with consent), before/after photos, counts that come from the database, and a visible About page with company details, contact, and address.
- Build a real "How we verify providers" page that states exactly what is checked (CNIC, documents, references). The app already collects provider documents; describe the actual process.
- Do not publish AI-generated filler or mass templated text. Google's guidance specifically calls out commodity content as the thing that gets compressed away.

### 3.6 Technical and page experience

- Keep content in server-rendered HTML (already true). Do not move key text behind JavaScript-only rendering.
- Measure with PageSpeed Insights and Search Console Core Web Vitals (LCP, INP, CLS) on mobile first; Pakistan traffic is overwhelmingly mobile (secondary source: over 80%). Likely wins: compress and size `og-default.jpg` and hero imagery, `loading="lazy"` for below-the-fold images, load the Meta Pixel after interaction, self-host or subset fonts.
- Use descriptive `alt` text, and keep one H1 per page.
- Make sure HTTP -> HTTPS and `www` -> non-www (or the reverse) redirect to a single canonical host. The canonical tag is built from the request host, so a stray host header could produce a second canonical; consider hard-coding the production host.
- Return real 404 / 410 codes for removed pages; keep `noindex` on thin or utility pages (e.g. delete-account is fine to leave indexed or `noindex`, your choice).
- Add `max-snippet` is not needed; do **not** add `nosnippet` anywhere, since snippet eligibility is a requirement for AI features.

### 3.7 Pakistan-specific notes

- Queries mix English, Urdu and Roman Urdu with city or neighbourhood names ("AC repair Bahria Town", "bijli mistri"). Use the English term as the main keyword and weave in the common local terms naturally (mistri, bijli wala, geyser repair), and consider Urdu-script versions of the key service pages with `hreflang="ur"`.
- Mention actual neighbourhoods and phases in the area pages, since that is how people search.
- Local citations and directories (Yellow Pages Pakistan, local business listings, app-store listings) help consistency and referral.

### 3.8 Measurement

1. Verify the domain in **Google Search Console** (Domain property) and submit the sitemap. AI Overview / AI Mode traffic is reported inside the normal Performance report (Web search type); there is no separate AI filter, so track impressions and clicks for the target queries.
2. Verify in **Bing Webmaster Tools** (can import from Search Console). This matters for ChatGPT (GEO report).
3. Add GA4 (or keep the existing analytics) with events for "Download app" clicks and store-link clicks, since the real conversion is an app install. Use UTM-tagged links in the Play Store / App Store listings.
4. Track a fixed list of 20 to 30 target queries monthly: rank, whether an AI Overview appears, and whether the site is cited.

### 3.9 Existing resource: the Meta Pixel

**How it is added today** (added by a colleague in commit `f4e0c5b`, 2026-09-10, "about page, services pillars, and Meta Pixel"):

- The standard Meta Pixel base snippet is pasted directly into `Views/Home/Index.cshtml`, inside `@section Head` (lines 7 to 27). Pixel ID `943484539264647`.
- It runs `fbq('init', ...)` and `fbq('track', 'PageView')` and has the usual `<noscript>` tracking image.
- **It exists only on the home page.** It is not in `_PublicLayout.cshtml`, so `/about`, `/Home/DownloadApp`, `/privacy-policy` and `/delete-account` are not tracked.
- No other events are sent (no download clicks, no scroll or engagement events), no cookie/consent handling, and no server-side (Conversions API) events.
- No other analytics (GA4, Google Tag Manager, Microsoft Clarity) was found anywhere in the repo, so the Pixel is currently the **only** measurement on the site. The Pixel ID is visible in page source by design; it is not a secret.

**What it is and is not for SEO.** The Pixel does not affect Google rankings or AI citation. Its value is measurement and paid/retargeting reach on Facebook and Instagram, and it is the only behavioural data the site has, so it is worth wiring up properly. Treat the numbers as Meta's view of the traffic, not as search analytics (Search Console is the source for that).

**How to use it:**

1. **Move it to the shared layout.** Put the snippet in `_PublicLayout.cshtml` (once, not per page) so every public page, including the new service landing pages, is tracked. Remove it from `Index.cshtml` to avoid double-firing. Load the Pixel ID from configuration instead of hard-coding it.
2. **Track the real goal: app installs.** On the Download page and wherever a Google Play / App Store button appears, fire a standard event on click, for example `fbq('track', 'Lead')` or a custom event such as `fbq('trackCustom', 'StoreClick', {store: 'google_play'})`. Add `ViewContent` on service pages with the category name. A page view alone says little.
3. **Build audiences for outreach.** With events flowing you can create Facebook/Instagram custom audiences (visited a service page but did not click download) and lookalikes, and run low-budget local ads or boosts targeted to Bahria Town and neighbouring areas. This supports the same "get discovered locally" goal as SEO.
4. **Measure UTM-tagged traffic.** Use UTM parameters on posts, ads and Google Business Profile links so Pixel and any future analytics can separate social, search and direct traffic.
5. **Consider the Conversions API** (server-side events). Browser Pixels lose data to ad blockers and iOS privacy settings; the ASP.NET backend can send the same events server-side. Only worth doing once ads are actually running.
6. **Add a lightweight consent notice and a privacy policy line.** The privacy policy (both `docs/PRIVACY_POLICY.md` and `Views/Home/Privacy.cshtml`) should state that the website uses the Meta Pixel and what it collects. Check Meta's current terms and local requirements before enabling advanced matching or sending any user data; do not send phone numbers, names or booking details to the Pixel.
7. **Keep it from hurting speed.** The base code loads asynchronously, which is fine, but defer it until after first paint or user interaction if Core Web Vitals (section 3.6) show it as a cost.
8. **Pair it with free search tools.** Add Google Search Console and Bing Webmaster Tools (3.8) and GA4 or a similar tool; the Pixel does not replace them. Its Events Manager "Test events" tab can verify installs, and the Meta Pixel Helper browser extension shows what fires on each page.

Items in this list that depend on Meta's current product behaviour (event names, Conversions API, consent requirements) were not re-checked against Meta's documentation for this report; confirm in Meta Business Help before implementing.

## 4. Prioritised action plan

| Priority | Action | Effort |
|---|---|---|
| P0 | Create and verify Google Business Profile (service-area) | Low |
| P0 | Verify Search Console and Bing Webmaster Tools; submit sitemap | Low |
| P0 | Resolve geographic scope; make schema, footer, FAQ and meta say the same true thing | Low |
| P1 | Move the Meta Pixel to the shared layout and add store-click events (3.9) | Low |
| P1 | Better home title and description; add `/about` to sitemap | Low |
| P1 | Richer `Organization` schema (`sameAs`, contact, real `areaServed`); drop `priceRange: "$$"` | Low |
| P1 | Service category landing pages from `ServiceCategories` / `ServiceTitles` | Medium |
| P2 | Service plus area pages for zones with real coverage | Medium |
| P2 | Dynamic sitemap with `lastmod` | Low |
| P2 | Core Web Vitals pass | Medium |
| P3 | Content: price guides, "how we verify", seasonal articles | Ongoing |
| P3 | Urdu / Roman Urdu variants and `hreflang` | Medium |

## 5. What not to spend time on

- `llms.txt`: Google confirms it is not used by its systems. (A few other tools may read it, so it is cheap, but it is not a ranking lever; see the GEO report.)
- Keyword meta tag, keyword stuffing, hidden text.
- Buying links, fake reviews or planting brand mentions. Google's spam systems discount them and they risk penalties.
- Mass-produced city/service pages with no unique content.

## Sources

- [Google Search Central: AI features and your website](https://developers.google.com/search/docs/appearance/ai-features)
- [Search Engine Journal: Google's new AI search guide calls AEO and GEO "still SEO"](https://www.searchenginejournal.com/googles-new-ai-search-guide-calls-aeo-and-geo-still-seo/575026/)
- [Practical Ecommerce: Google says AI optimization is just SEO](https://www.practicalecommerce.com/google-says-ai-optimization-is-just-seo)
- [ALM Corp: Google's AI optimization guide](https://almcorp.com/blog/google-ai-optimization-guide/)
- [Passionfruit: Google drops FAQ rich results](https://www.getpassionfruit.com/blog/what-changed-with-google-drops-faq-rich-results-and-what-to-do-now) and [JSON-LD.com: FAQ rich results deprecated](https://jsonld.com/google-faq-rich-results-deprecated/) (secondary sources; confirm in Google's docs)
- [LSEO: LocalBusiness schema](https://lseo.com/answer-engine-optimization-services/localbusiness-schema-the-foundation-of-neighborhood-aeo/)
- [Google Business Profile help: service-area businesses](https://support.google.com/business/answer/14271705)
- [PPC Land: service area business](https://ppc.land/service-area-business/)
- [IPS News: Is SEO important for Pakistani businesses (2026)](https://ipsnews.net/business/2026/03/07/is-seo-important-for-pakistani-businesses-a-complete-guide-for-2026/)
- [SoftVirtue: SEO for Pakistani businesses and local search](https://softvirtue.com/blog/seo-for-pakistani-businesses-local-search)

*Note:* statistics quoted from third-party blogs (mobile share, FAQ retirement dates) are not primary sources. Check anything you plan to act on against Google's documentation. The audit in section 2 reflects the repo as of this date; the live site was not crawled in depth.
