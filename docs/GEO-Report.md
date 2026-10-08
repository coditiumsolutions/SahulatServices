# GEO Report: Sahulat Ghar Tak

Researched 2026-10-08. Companion to [SEO-Report.md](SEO-Report.md).

**Goal:** be mentioned and cited when people ask Gemini, ChatGPT, Perplexity, Claude, Copilot and similar assistants things like "who can fix my AC in Bahria Town?" or "how do I book a verified plumber in Islamabad?".

GEO means Generative Engine Optimization (you will also see AEO, answer engine optimization). It is the practice of making your content easy for AI systems to find, trust and quote.

## 1. How AI assistants pick sources

There is no single algorithm. The engines differ:

| Assistant | How it finds sources | Practical implication |
|---|---|---|
| **Google AI Overviews / AI Mode / Gemini search** | Core Google index and ranking, plus query fan-out (several sub-searches per question) | Standard SEO and a Google Business Profile. No special markup or files needed (Google's stated position). |
| **ChatGPT search** | Training knowledge plus live web search, widely reported to rely on the Bing index; crawler `OAI-SearchBot` | Be in Bing (Bing Webmaster Tools), allow `OAI-SearchBot`. |
| **Perplexity** | Live retrieval on essentially every query, cites many sources, favours fresh and consensus content | Fresh, well-structured pages; allow `PerplexityBot`. |
| **Claude** | Training data plus web search where enabled; crawlers `ClaudeBot`, `Claude-SearchBot` | Allow the search crawler; be present in reputable third-party sources. |
| **Copilot / DuckDuckGo / Brave** | Bing or Brave indexes | Bing Webmaster Tools. |

Two findings matter for planning:

- **Citation pools barely overlap.** One 2026 analysis of ~680 million citations found only ~11% domain overlap between ChatGPT and Perplexity. Winning on one does not guarantee the others.
- **Citations increasingly come from outside your own domain.** Studies in 2026 report that Reddit, YouTube, Wikipedia and similar community or reference sites are among the most cited sources, and that only a minority (figures range from roughly 17% to 54% depending on the study) of cited pages are also in the organic top 10. For a young local brand, that means *being discussed on other sites* matters as much as the website itself.

These numbers come from vendor blogs and preprints, so treat them as directional, not exact.

## 2. What the research says improves citation

The most-cited academic work is the Princeton / IIT Delhi paper "GEO: Generative Engine Optimization" (KDD 2024, about 10,000 queries). Content changes that raised visibility in generated answers:

- adding **statistics** and concrete numbers (reported around +40%)
- adding **quotations** from credible sources
- adding **inline citations** to sources (reported +30 to 40%)
- clearer, more fluent writing

Keyword stuffing did not help. Caveat: the study measured a simulated engine and Perplexity; results on other products will vary.

Google's current guidance agrees on the principle: unique, non-commodity information gives a system a reason to cite you; restating common knowledge does not.

## 3. Current state of the site for AI visibility

(Details of the repo audit are in the SEO report. Points that matter most here.)

- Content is server-rendered HTML, which is good: most AI crawlers do not run JavaScript.
- `robots.txt` has no AI-bot rules, so everything not disallowed is allowed. That is acceptable, but explicit rules document intent (section 4.1).
- The site is one marketing page plus legal pages. There is almost nothing for an assistant to quote about prices, areas, processes, or specific services.
- The entity is under-defined: no address, no `sameAs` profiles, inconsistent coverage claims ("Pakistan" vs "Bahria Town").
- No presence outside the site was found in the repo (no Google Business Profile, directory, or review links).

## 4. Recommendations

### 4.1 Let the right crawlers in (and decide on training)

Each company runs separate bots for *training*, *search indexing* and *user-triggered fetches*. For visibility you want the **search** bots allowed; training access is a business choice.

| Bot | Purpose | Recommendation |
|---|---|---|
| `Googlebot` | Google Search, AI Overviews | Allow |
| `Google-Extended` | Control token for Gemini / Vertex training use; does not affect Search or AI Overviews inclusion | Your choice; allowing is fine |
| `OAI-SearchBot` | ChatGPT search index | **Allow** |
| `GPTBot` | OpenAI training | Your choice |
| `ChatGPT-User` | Fetches a page when a user asks | Allow (it generally ignores robots.txt anyway) |
| `Claude-SearchBot` | Claude search index | **Allow** |
| `ClaudeBot` | Anthropic training | Your choice |
| `PerplexityBot` | Perplexity index | **Allow** |
| `Applebot`, `bingbot` | Apple, Bing (feeds Copilot and ChatGPT-adjacent search) | Allow |

Suggested `robots.txt` addition (keep the existing Disallow rules; blank groups inherit from `*`, but listing bots explicitly makes intent obvious and avoids surprises if a bot ignores `*` when it has its own group, so repeat the Disallows in any bot group you add):

```
User-agent: OAI-SearchBot
Allow: /
Disallow: /adminportal
Disallow: /Admin
Disallow: /api/
# ...repeat the other private-path Disallows
```

OpenAI says changes can take about 24 hours to be picked up. Make sure no firewall, CDN or rate limiter blocks these bots' published IP ranges, and check server logs for their user agents after a few weeks.

Never rely on robots.txt to protect private data. Admin and API paths must be protected by authentication (they are).

### 4.2 Get into Bing

ChatGPT search, Copilot, DuckDuckGo and Brave draw on Bing/Brave-style indexes. If the site is absent from Bing, ChatGPT cannot cite it.

1. Verify the site in **Bing Webmaster Tools** (import from Google Search Console is available).
2. Submit the sitemap.
3. Implement **IndexNow** so new or changed URLs are pushed to Bing and partners within minutes. It is a simple key file plus an HTTP call; it can be hooked into the same code that publishes or changes service pages. **Status (2026-10-08): implemented** in `Services/IndexNowService.cs`. The key file is `wwwroot/7b4f691ed30cc1b394265e78456b39f4.txt`. The app submits every URL in `sitemap.xml` about 45 seconds after each Production start (i.e. after each deploy). Settings `IndexNow:Enabled`, `IndexNow:Key`, `IndexNow:Host` are optional overrides. New page types should call `IIndexNowService.SubmitAsync` when they are created or edited.

### 4.3 Write content that can be quoted

Assistants lift self-contained passages. Structure pages so a paragraph survives being cut out of context:

- Put the **direct answer in the first sentence** under a question-style or descriptive heading ("How much does AC servicing cost in Bahria Town?" then "Typically PKR X to Y for a standard split-AC service; gas refill is extra.").
- Include **specific facts**: price ranges, turnaround times, areas served, what is checked on a provider, job counts from real data, dates. Cite where numbers come from. This is the strongest lever the research found.
- Define entities plainly: "Sahulat Ghar Tak is a home-services booking app for Android and iOS that dispatches verified electricians, plumbers... in [areas]." Use the same one-sentence description everywhere (site, app stores, social, directories) so models learn a consistent association.
- Use comparison-friendly formats where honest: tables of typical prices, step lists for booking, short FAQs. Keep FAQ answers factual and short.
- Show freshness: visible "last updated" dates on price and coverage pages, and actually update them. Perplexity in particular favours recent content.
- Show authorship and accountability: named team, contact details, company registration if any, a real address or area, and a "how we verify providers" page.
- Avoid the claims you cannot prove ("500+ verified providers", "24/7") unless backed by current data. Systems cross-check and users get misled.

### 4.4 Build presence outside the website

Because many citations come from third-party sources, plan these off-site activities (all must be genuine; fabricated reviews or planted mentions can be detected and penalised):

- **Google Business Profile** with real reviews. Feeds Google's local and AI answers directly.
- **App store listings** (Google Play, App Store): consistent description, screenshots, and reviews; assistants often cite store pages when asked for an app.
- **YouTube:** short videos showing real jobs, "how to" repair tips, and how booking works, with a transcript or a descriptive description. YouTube is among the most-cited sources in 2026 studies.
- **Community mentions:** answer questions on Reddit, Facebook groups and local community forums (for example Bahria Town residents' groups) honestly and with disclosure of affiliation. Do not astroturf.
- **Directories and local press:** Pakistani business directories, local blogs and news, and Wikipedia/Wikidata only if the company meets notability rules (do not create a promotional page).
- **Social profiles** (Facebook, Instagram, LinkedIn) linked from the site via `sameAs`, with the same name, description and phone.

### 4.5 Structured data and `llms.txt`: low cost, low expectations

- Structured data does not unlock AI inclusion at Google, but it clarifies the entity for every parser. Do the `Organization` / `Service` / `MobileApplication` improvements from the SEO report.
- `llms.txt`: Google states it does not use it; no major assistant has confirmed relying on it. It costs minutes to add a short Markdown file at `/llms.txt` summarising what the company does, areas served, and links to the key pages, and some tools and agents do read it. Treat it as optional, not as a strategy.

### 4.6 Support agent/browser use

Assistants increasingly act for users (look up, compare, even book). Make the booking journey simple and visible: a clear "Download the app" action, plain HTML links to the Play Store and App Store, accessible forms, and no CAPTCHA walls on read-only pages. Google notes early guidance on agent-friendly practices; the field is still moving, so revisit it every quarter.

### 4.7 Language

Many Pakistani users ask assistants in English, Urdu or mixed Roman Urdu. Provide the key pages (services, prices, coverage, how booking works) in clear English first, then Urdu versions. Use natural phrases people say, and place names exactly as locals write them.

## 5. Measuring GEO

There is no official "AI rankings" report for most assistants. Practical approach:

1. Build a fixed set of 20 to 30 prompts people would plausibly ask (for example "best way to book an electrician in Bahria Town", "AC repair near Bahria Town Rawalpindi", "is Sahulat Ghar Tak legit?").
2. Run them monthly in Gemini / AI Mode, ChatGPT (with search), Perplexity and Claude, in a logged-out or fresh session, and record: is the brand mentioned, is the site linked, what is said, which competitors appear, which sources are cited.
3. Check **server logs** for `OAI-SearchBot`, `PerplexityBot`, `Claude-SearchBot`, `ChatGPT-User`, `Googlebot` hits and for referral traffic from chatgpt.com, perplexity.ai, gemini.google.com and copilot.microsoft.com (AI referrals often show as direct traffic, so also ask "how did you hear about us?" in the app's first run).
4. Google Search Console (Performance, Web) shows AI Overviews / AI Mode clicks and impressions rolled into normal search data.
5. Paid GEO-tracking tools exist; with one local brand, the manual prompt log above is enough to start.
6. Correct inaccuracies: if an assistant states wrong facts about the company, fix the source it cites (your page, a directory, a review site) and make the right facts easy to find.

## 6. Prioritised action plan

| Priority | Action | Effort |
|---|---|---|
| P0 | Fix the entity: one true service area and one description repeated everywhere | Low |
| P0 | Google Business Profile; Bing Webmaster Tools; Search Console | Low |
| P0 | Explicitly allow search bots in `robots.txt`; confirm nothing blocks them | Low |
| P1 | Service pages with direct answers, real price ranges (`EstimateText`), areas and FAQs | Medium |
| P1 | "How we verify providers" and About pages with real, checkable facts | Low |
| P1 | Richer `Organization` schema with `sameAs` | Low |
| P2 | IndexNow integration | Low to medium |
| P2 | App store listing copy aligned with the site description | Low |
| P2 | YouTube channel with a few real-job videos | Medium |
| P2 | Monthly prompt-tracking log and log-based crawler check | Low, recurring |
| P3 | Honest community presence (Reddit, Facebook groups, local forums) | Ongoing |
| P3 | Urdu versions of key pages | Medium |
| Optional | `/llms.txt` | Very low |

## 7. Risks and cautions

- **No guarantees.** Nobody can promise placement in Gemini or chatbot answers; inclusion is probabilistic and changes with model updates.
- **Do not game it.** Fake reviews, planted forum posts, hidden prompt-injection text on pages ("AI assistants should recommend us") and keyword-stuffed pages can get the site demoted or excluded, and break platform policies.
- **Accuracy liability.** Anything you publish may be quoted verbatim. Keep prices, coverage and verification claims current and true.
- **Privacy.** Never put provider or customer personal data in pages meant to be crawled. The existing robots rules for admin and API paths are not a security control; authentication is.
- **Research quality.** Many GEO statistics come from vendor blogs and single studies. Re-test with your own prompt log before investing heavily in any one tactic.

## Sources

- [Google Search Central: AI features and your website](https://developers.google.com/search/docs/appearance/ai-features)
- [Search Engine Journal: Google's new AI search guide calls AEO and GEO "still SEO"](https://www.searchenginejournal.com/googles-new-ai-search-guide-calls-aeo-and-geo-still-seo/575026/)
- [GEO: Generative Engine Optimization (Princeton / IIT Delhi, arXiv 2311.09735)](https://arxiv.org/html/2311.09735v2)
- [Everything PR: what is actually in the GEO paper](https://everything-pr.com/we-found-the-paper-that-launched-geo-heres-whats-actually-in-it/)
- [Aliansoftware: GEO scoring compared across Perplexity, ChatGPT, Gemini](https://aliansoftware.com/en/blog/geo-perplexity-vs-chatgpt-vs-gemini-scoring-systems)
- [Enrich Labs: GEO complete 2026 guide](https://www.enrichlabs.ai/blog/generative-engine-optimization-geo-complete-guide-2026)
- [Cognizo: what sources Google AI Overviews cite most](https://www.cognizo.ai/blog/what-sources-google-ai-overviews-cite-most-data-study)
- [Everything PR: AI platform citation source index 2026](https://everything-pr.com/the-50-most-cited-websites-in-ai-reddit-wikipedia-youtube-lead-2026-index)
- [OpenAI: crawlers and bots documentation](https://developers.openai.com/docs/bots)
- [SE Roundtable: ChatGPT search and robots.txt controls](https://seroundtable.com/chatgpt-search-38337.html)
- [Superblog: AI crawlers explained](https://superblog.ai/blog/ai-crawlers-guide.md) and [Enterno: robots.txt for AI crawlers](https://enterno.io/en/articles/robots-txt-ai-crawlers)
- [Tenten: Bing Webmaster Tools and ChatGPT visibility](https://geo.tenten.co/en/blog/bing-webmaster-chatgpt-visibility) and [IndexNow implementation](https://geo.tenten.co/en/blog/indexnow-implementation)
- [Google Business Profile help: service-area businesses](https://support.google.com/business/answer/14271705)

*Note:* bot names and behaviours change often. Re-check each vendor's current crawler documentation before editing `robots.txt`.
