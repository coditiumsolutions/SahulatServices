# Plan: Urdu variants of the public website

Status: planning only, nothing implemented. Written 2026-10-08. Follow-up to [SEO-Report.md](SEO-Report.md) (section 3.7) and [GEO-Report.md](GEO-Report.md) (section 4.7).

## Goal

Serve the public pages in Urdu (script) as well as English so that people searching in Urdu, and AI assistants answering Urdu questions, can find and quote the site. Roman Urdu is a search habit rather than a page language: handle it with keywords inside the English pages, not a third version.

## Decisions to make first

| Decision | Recommendation |
|---|---|
| URL structure | Subfolder per language: `/ur/...` for Urdu, English stays at the root. One domain keeps ranking authority together; a separate domain or `?lang=` parameter is worse for SEO. |
| Who translates | A human fluent Urdu writer or reviewer. Machine translation is fine for a first draft, but unreviewed output reads badly and quality-filters treat it as low value. Keep brand names (Sahulat Ghar Tak, Bahria Town) consistent. |
| Scope of v1 | Home, About, Download App, and later the service landing pages. Privacy policy and delete-account can follow, but check with the owner first because the privacy policy has its own sign-off. |
| Default language handling | Never auto-redirect by browser language or IP. Show a visible language switch and let crawlers reach both versions. |

## Technical plan (ASP.NET Core 8 MVC)

1. **Routing.** Add a route prefix `/{culture:regex(^(en|ur)$)}` (or an `[Route("ur/...")]` pair) for the public controller actions only. Admin and `/api/*` stay untouched. Set `RequestLocalization` with supported cultures `en` and `ur-PK`, taking the culture from the URL segment.
2. **Text storage.** Public strings currently live in the Razor views. Move the translatable copy into `.resx` resources (`IViewLocalizer`) so each view has one template and two languages. Avoid duplicating whole views.
3. **Right-to-left layout.** Set `<html lang="ur" dir="rtl">` for Urdu. Add an RTL stylesheet (Bootstrap 5.3 has an RTL build) and review the hero, stats cards, marquee, footer and header for mirrored layout. Use an Urdu web font such as Noto Nastaliq Urdu (or Noto Naskh Arabic for easier reading), loaded with `font-display: swap`.
4. **`hreflang`.** In `_PublicLayout.cshtml` emit, for every page that has both versions:
   - `<link rel="alternate" hreflang="en" href="https://sahulatghartak.com/..." />`
   - `<link rel="alternate" hreflang="ur" href="https://sahulatghartak.com/ur/..." />`
   - `<link rel="alternate" hreflang="x-default" href="(English URL)" />`
   Each version must also carry a self-referencing canonical (the existing canonical code builds from the request path, which already works with the `/ur` prefix). Links must be reciprocal or Google ignores them.
5. **Sitemap.** List both versions of each page, with `xhtml:link` alternates inside each `<url>`. This is the point at which the static `sitemap.xml` should become a generated one.
6. **Metadata and schema.** Translate title, description, Open Graph tags and FAQ text per language. Set `inLanguage` in the JSON-LD (`en` / `ur`) and keep `availableLanguage` in the contact point. The `FAQPage` block should contain the Urdu questions on the Urdu page only.
7. **Dynamic content.** Service category and title names come from the database in English. For Urdu pages either add nullable `NameUr` / `DescriptionUr` columns (script plus mapping, following the repo's SQL-script convention) or, for v1, show the English term in brackets after the Urdu name. Decide before building service landing pages so they are not rebuilt twice.
8. **Language switcher.** A plain link in the header and footer to the same page in the other language (not a JS toggle), so crawlers can follow it.
9. **Stats and live counts.** The home-page counts already come from the database and need only translated labels and Eastern/Western digit formatting (Western digits are fine and common in Pakistani web copy).

## Content guidance

- Use the words people search: for example electrician (بجلی والا / الیکٹریشن), plumber (پلمبر), AC repair (اے سی کی مرمت), cleaning (صفائی), carpenter (بڑھئی), painter (پینٹر). Have the reviewer confirm which terms are most used in Islamabad and Rawalpindi, and check Search Console queries once data exists.
- Keep the same facts in both languages (areas served, categories, verification claims). A claim that differs between versions will be flagged by readers and by AI systems.
- Write Urdu naturally, not as a sentence-by-sentence mirror of the English.

## Mobile app link

The Flutter app is a separate decision. If the app gets Urdu, the website language switch and the app's language setting are independent; no backend API change is needed for the website work.

## Suggested order

1. Choose the translator and confirm scope and URL structure.
2. Extract the home, about and download strings into resources; add the culture routing and RTL layout with English only, and check nothing regresses.
3. Add Urdu copy and the language switcher; add `hreflang` and the sitemap alternates.
4. Validate with a crawler and Search Console's international targeting and indexing reports; submit the `/ur` URLs.
5. Extend to service landing pages and the `NameUr` columns when those exist.

## Verification checklist

- Every Urdu URL returns 200, has its own canonical, and appears in the sitemap.
- `hreflang` pairs are reciprocal on every page.
- Right-to-left layout is correct on a phone-width screen (most visitors are on mobile).
- `robots.txt` does not block `/ur/`.
- Spot-check with an Urdu speaker for tone, spelling and correct place names.
- Search Console shows the Urdu pages indexed after a few weeks.
