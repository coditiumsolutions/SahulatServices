# Sahulat Ghar Tak: web parity UI reference

**Audience:** the website developer restyling the client and provider dashboards to look like the Flutter app on a phone. Structure, copy, API and business rules are in `web-parity-spec.md`; this file only adds what the app looks like, the measurements, and the reusable image assets.
**Source:** this Flutter repo at Android `1.0.6+12` / iOS `1.0.8+12` (`pubspec.yaml:6-8`).
**Rule:** every measurement cites `file:line` in the Flutter repo. "UNVERIFIED" marks anything the code or the screenshots did not prove. No emojis, no secrets in this file.

## 0. How the screenshots were taken (read this first)

| Item | Fact |
|---|---|
| Renderer | Flutter web release build, run in desktop Chrome at 390x844 CSS px, device scale factor 2 (images are 780x1688). Small captures are 360x640 (720x1280). Wide capture is 1024x768. |
| Why not a phone | No Android emulator was running and the local backend was down. The web build was pointed at production with two test accounts (one client, one provider). |
| What differs from a real phone | There is no status bar and no system navigation bar in the images. Text renders in Roboto because the web build bundles it; Android renders Roboto too, iOS renders San Francisco (see section 2b). The Google Map tiles are blank on web. |
| Redaction | None. The user asked for no blurring. The images show the test accounts' names, numbers, CNIC and profile photo. Do not publish them outside the team. |
| Data | Both accounts have almost no data: the client has zero requests, the provider has zero requests and zero bookings. Request cards, booking cards and booking detail are therefore not captured (section 8). |
| Production write | The provider "Online" pill was toggled to Offline once and put back to Online with the saved timing unchanged. Nothing else was submitted, saved or deleted. |

Images live in `screenshots/`. Asset files live in `screenshots/assets/`.

## 1. Screenshot index

| # | Screen and state | File |
|---|---|---|
| 01 | Landing | `screenshots/01-landing.png` |
| 02 | Login empty | `screenshots/02-login-empty.png` |
| 02 | Login with validation errors | `screenshots/02-login-error.png` |
| 03 | Customer registration top | `screenshots/03-customer-registration.png` |
| 03 | Gender pill selected | `screenshots/03-customer-registration-gender.png` |
| 03 | Terms section expanded | `screenshots/03-customer-registration-terms.png` |
| 03 | Terms expanded, bottom, Register disabled | `screenshots/03-customer-registration-b.png` |
| 04 | Customer inbox top, all read | `screenshots/04-notifications.png`, `-b`, `-c` |
| 04 | Provider inbox with unread items | `screenshots/04-provider-notifications.png` |
| 05 | Client Home top, services, featured, reviews, Contact Us | `screenshots/05-client-home.png`, `-b`, `-c` |
| 05 | Home search with suggestions | `screenshots/05-client-home-search.png` |
| 05 | Home at 360x640 | `screenshots/05-client-home-small.png` |
| 05 | Home at 1024x768 | `screenshots/05-client-home-wide.png` |
| 05 | Guest Home | `screenshots/05-guest-home.png` |
| 05 | Guest "Login Required" dialog | `screenshots/05-guest-login-required.png` |
| 06 | Sub-categories | `screenshots/06-subcategories.png` |
| 07 | Request form top | `screenshots/07-request-form.png`, `-b`, `-c`, `-d` |
| 07 | Service title menu open | `screenshots/07-request-form-titles.png` |
| 07 | After choosing a title (budget card) | `screenshots/07-request-form-budget.png` |
| 07 | "Other (not listed)" | `screenshots/07-request-form-other.png`, `-other-b` |
| 07 | "Same as my account" off | `screenshots/07-request-form-contact-off.png` |
| 08 | My Requests Active / Completed / Cancelled (all empty) | `screenshots/08-my-requests.png`, `-completed`, `-cancelled` |
| 08 | My Requests guest lock | `screenshots/08-my-requests-guest.png` |
| 08 | My Requests 360 and 1024 | `screenshots/08-my-requests-small.png`, `-wide` |
| 09 | Confirm dialog (logout) | `screenshots/09-confirm-dialog-logout.png` |
| 09 | Message dialog (login failed) | `screenshots/09-message-dialog-error.png` |
| 11 | Customer Profile | `screenshots/11-customer-profile.png`, `-b`, `-c` |
| 11 | Guest Profile | `screenshots/11-profile-guest.png` |
| 11 | Customer Edit Profile | `screenshots/11-customer-edit-profile.png`, `-b` |
| 11 | Add Address (map blank on web) | `screenshots/11-add-address.png`, `-b` |
| 13 | My Documents, verified and locked | `screenshots/13-documents.png`, `-b` |
| 14 | Provider Home online / offline | `screenshots/14-provider-home-online.png`, `-offline` |
| 14 | Available Timing dialog | `screenshots/14-provider-availability-dialog.png` |
| 14 | Provider Home 360x640 | `screenshots/14-provider-home-small.png` |
| 15 | Provider Requests empty | `screenshots/15-provider-requests-empty.png` |
| 15 | Rejected Requests empty | `screenshots/15-rejected-requests.png` |
| 16 | Provider Bookings empty, 390 and 360 | `screenshots/16-bookings.png`, `-small` |
| 18 | Wallet (credit balance, one transaction) | `screenshots/18-wallet.png` |
| 19 | Provider Profile, top to bottom | `screenshots/19-provider-profile.png`, `-b` to `-h` |
| 19 | Provider Edit Profile | `screenshots/19-provider-edit-profile.png`, `-b` |
| 19 | Categories and Services | `screenshots/19-categories.png` |
| 19 | Primary-category dialog | `screenshots/19-primary-category-dialog.png` |
| 19 | Category picker | `screenshots/19-category-picker.png` |

## 2. Global design facts

### 2a. Colours

There are two colour families. The tokens are in `lib/utils/constants.dart:4-6` and repeated as private constants in most widgets.

| Token | Hex | Where it is used |
|---|---|---|
| kPrimaryColor (indigo) | `#6366F1` | `constants.dart:4`. Auth group only: Login, Registration, Forgot/Reset, Edit Profile (customer and provider headers excepted, see below), Add Address header. Header background, primary button, focus ring (1.5 px, `auth_card_scaffold.dart:153`), selected gender pill (`:245`), text links ("Forgot password?", "Create account"). Also the default accent of the request form (`service_request_form_screen.dart:56`). |
| brandBlue | `#016EE3` | Client Home/Requests/Profile header gradient end, bottom nav gradient end, search icon, section bars, profile buttons, provider dashboard everywhere (`home_screen.dart:46`, `bottom_nav.dart`, `provider_tab_header.dart:5-7`). Also the launcher/splash background (`pubspec.yaml:70,75`). |
| brandDark | `#0A4FA8` | Gradient start for every header and for both bottom navs; the card shadow tint (`0x330A4FA8`, `home_screen.dart:317-318`). |
| brandAccent | `#4FC3F7` | Selected nav icon colour and pill glow (`bottom_nav.dart:155,171`), decorative glow circle in headers at 14% alpha (`provider_tab_header.dart:50-55`), Landing "Register" button fill (screenshot 01; line UNVERIFIED). |
| ink | `#14213D` | Section titles on Home (`home_screen.dart:48`). |
| text strong | `#1A2233` | Card titles, empty-state titles, toast background (`app_toast.dart:44`, `empty_state_placeholder.dart:54`). |
| page background | `#F4F7FB` | Scaffold background on Home, Requests, notifications (`home_screen.dart:200`, `service_requests_screen.dart:152`, `notifications_screen.dart:64`). |
| input fill | `#F5F5F7` | Text fields and unselected gender pills (`auth_card_scaffold.dart:144,245`). |
| inner panel | `#F6F8FC` | Info panel inside request cards (`service_requests_screen.dart:400`). |
| tab track | `#EAEFF6` | Segmented filter track (`status_filter_tabs.dart:36`). |
| border light | `#E5E5EA`, `#E6EBF2` | Sub-category tile border (`subcategory_card.dart:47`), read notification border (`notifications_screen.dart:192`). |
| success | `#16A34A` | Success toast and dialog, "accepted/completed" notification (`app_toast.dart:26`, `notifications_screen.dart:162`). |
| teal | `#0D9488` | "job started" notification (`notifications_screen.dart:163`). |
| warning | `#D97706` | "reassigning" notification (`notifications_screen.dart:164`). |
| danger | `#DC2626` | Cancelled and unavailable notification (`notifications_screen.dart:165`). |
| violet | `#7C3AED` | "app update" notification (`notifications_screen.dart:166`). |
| WhatsApp | `#25D366` | WhatsApp action (7 uses in `lib`). |
| Service group palette | `#FF6B6B #4ECDC4 #45B7D1 #FFA07A #98D8C8 #F7DC6F #BB8FCE #85C1E2` | Cycled over service groups for tile fill and for the request form accent (`service_colors.dart:6-14`). The screenshots show sky `#45B7D1` for Home Maintenance. |

**Answer to the indigo-versus-blue question:** indigo `#6366F1` is used only on the auth group and as the request form's default accent. Everything on the client side after login (header, nav, buttons on Profile, links) and everything on the provider side uses brand blue `#016EE3` with `#0A4FA8` as the dark end. The request form then takes the colour of the service group the customer tapped, so its header, switches, budget card and submit button change per service (teal in `07-request-form.png`).

Empty-state icon circles look indigo-tinted in screenshots 08 and 15. The source line was not located: UNVERIFIED.

### 2b. Typography

No `fontFamily` and no `textTheme` are set (`main.dart:155-158`), so Material 3 defaults apply: Roboto on Android, San Francisco on iOS (system font), Roboto in the web screenshots. No font files are bundled (no `fonts:` block in `pubspec.yaml`; no `assets/fonts`). For the website, use Roboto as the closest match on Android and `-apple-system` on iOS.

| Style | Size / weight | Colour | Line height | Letter spacing | Source |
|---|---|---|---|---|---|
| Home header title | 18 / 800 | white | default | -0.2 | `home_screen.dart:364-366` |
| Home header subtitle | 12 / 500 | white, reduced alpha (value UNVERIFIED) | default | 0 | `home_screen.dart:373-374` |
| Provider tab header title | 20 / 800 | white | default | -0.2 | `provider_tab_header.dart` (title block) |
| Provider tab header subtitle | 12.5 / 500 | white 75% | default | 0 | same |
| Compact header title | 17 / 700 | white | default | -0.2 | `compact_app_header.dart` |
| Section title | 18 / 800 | `#14213D` | default | -0.2 | `home_screen.dart:500-503` |
| Auth page title | 24 / bold | near black | default | 0 | `auth_card_scaffold.dart:83-84` |
| Auth subtitle | 14 / regular | grey | default | 0 | `auth_card_scaffold.dart:93` |
| Field label | 14 / 600 | black 87% | default | 0 | `auth_card_scaffold.dart:165` |
| Primary button label | 16 / 600 | white | default | 0 | `auth_card_scaffold.dart:310`, `landing_screen.dart:107-108` |
| Landing text links | 14.5 / 600 | white, underlined | default | 0 | `landing_screen.dart:145-146` |
| Service tile label | 16 / 700 | ink | default | 0 | `main_category_card.dart:70` |
| Sub-category label | 12 / 600; caption 10 black 38% | | default | 0 | `subcategory_card.dart:71-78` |
| Request card title | 16.5 / 800 | `#1A2233` | 1.2 | 0 | `service_requests_screen.dart:367-370` |
| Request card subtitle | 12.5 / 600 | status colour | default | 0 | `service_requests_screen.dart:377-378` |
| Request card body | 13.5 | grey | 1.35 | 0 | `service_requests_screen.dart:392-393` |
| Request card caption | 11.5 / 500 | grey | default | 0 | `service_requests_screen.dart:475-476` |
| Urgent pill | 11 / 700 | | default | 0 | `service_requests_screen.dart:434-436` |
| Stat card value | 21 / 800 | `#12182B` | 1.0 | -0.3 | `dashboard_stat_card.dart:74-78` |
| Stat card label | 12 | grey | default | 0 | `dashboard_stat_card.dart:85` |
| Empty-state title | 16 / 800 | `#1A2233` | default | 0 | `empty_state_placeholder.dart:52-54` |
| Empty-state body | 13.5 | grey | 1.4 | 0 | `empty_state_placeholder.dart:62-64` |
| Dialog title | 17 / 800 | brand dark / ink (UNVERIFIED which) | default | 0 | `message_dialog.dart:82-83`, `confirm_dialog.dart:54` |
| Toast | 13.5 / 600 | white on `#1A2233` | default | 0 | `app_toast.dart:62-63` |
| Filter tab label | 12.5; weight differs selected vs not | | default | 0 | `status_filter_tabs.dart:82-84` |
| Status chip | 12 / 600 | status colour | default | 0 | `status_chip.dart:33` |
| Bottom nav label | client 11.5, provider 10.5; selected 700, unselected 500 | white / white 54% | default | 0.2 | `bottom_nav.dart:180-182`, `provider_bottom_nav.dart:160-162` |

### 2c. Icons

The set is Flutter's built-in Material Icons, almost always the Rounded variant, with an Outlined variant for unselected nav items (`bottom_nav.dart:19-34`). The web equivalent is Material Symbols Rounded.

| Element | Icon name | Source |
|---|---|---|
| Client nav, selected / unselected | `home_rounded` / `home_outlined`; `receipt_long_rounded` / `receipt_long_outlined`; `person_rounded` / `person_outline_rounded` | `bottom_nav.dart:19-34` |
| Provider nav, selected / unselected | `home_rounded`; `inbox_rounded`; `work_rounded`; `account_balance_wallet_rounded`; `person_rounded` / the `_outlined` or `_outline_rounded` forms | `provider_bottom_nav.dart:17-29` |
| Notification bell | `notifications_none_rounded`, size 26, white, with Badge | `notification_bell.dart:25-31` |
| Home search | `search_rounded` (brand blue), `close_rounded` to clear | `home_screen.dart:405-409` |
| Contact Us button | `support_agent_rounded` | `home_screen.dart:267` |
| Gender pills | `male_rounded`, `female_rounded`, `transgender_rounded` | `auth_card_scaffold.dart:181-183` |
| Auth header avatar | `person` (login), `person_add` style (registration; exact name UNVERIFIED) | `auth_card_scaffold.dart:22` |
| Request card rows | `location_on_rounded`, `event_rounded`, `payments_rounded`, `priority_high_rounded`, `schedule_rounded`, `password_rounded` | `service_requests_screen.dart:406-496` |
| Request status | `check_circle_rounded`, `cancel_rounded`, `sync_rounded`, `person_pin_circle_rounded`, `hourglass_top_rounded` | `service_requests_screen.dart:279-287` |
| Notification types | `work_outline_rounded` (job_assigned), `check_circle_outline_rounded` (booking_accepted), `block_rounded` (job_unavailable), `sync_rounded` (provider_reassigning), `cancel_outlined` (booking_cancelled), `play_circle_outline_rounded` (job_started), `task_alt_rounded` (job_completed), `system_update_rounded` (app_update), default `notifications_none_rounded` | `notifications_screen.dart:147-155` |
| Stat cards | `inbox`, `work`, `task_alt`, `star`, `account_balance_wallet`; corner `arrow_outward_rounded` | `provider_home_tab.dart:77-101`, `dashboard_stat_card.dart:66` |
| Online pill | `wifi_tethering` / `wifi_tethering_off` | `provider_home_tab.dart:137-138` |
| Toast and dialog | `check_circle_outline_rounded`, `error_outline_rounded`, `info_outline_rounded`, confirm default `help_outline_rounded` | `app_toast.dart:27-35`, `confirm_dialog.dart:15` |
| Empty and error | `wifi_off_rounded`, `lock_outline_rounded`, `refresh_rounded` | `home_screen.dart:460-473`, `service_requests_screen.dart:162,211` |
| Back arrow | `arrow_back` (auth, compact header), `arrow_back_rounded` (inbox) | `auth_card_scaffold.dart:55`, `compact_app_header.dart`, `notifications_screen.dart:71` |

### 2d. Shape and elevation

| Element | Radius | Shadow / border | Source |
|---|---|---|---|
| Buttons (primary, Landing, gender pill) | 14 | elevation 0 | `auth_card_scaffold.dart:299`, `landing_screen.dart:100,121` |
| Prominent profile buttons | 14, border 2 px for outlined | elevation 4 | `constants.dart:9-30` |
| Text inputs | 14, no border; focus border 1.5 px indigo | none | `auth_card_scaffold.dart:144-155` |
| Search field | 16 | blur 14, offset (0, 6) | `home_screen.dart:388-393` |
| Service tile | 16 | blur 12, offset (0, 6) | `main_category_card.dart:37-55` |
| Sub-category tile | 14, border 1 px `#E5E5EA` | blur 6, offset (0, 2) | `subcategory_card.dart:40-52` |
| Featured card | 18 | blur 14, offset (0, 8) | `featured_services_carousel.dart:74-79` |
| Request card | 18 | `#0A4FA8` 6%, blur 18 | `service_requests_screen.dart:333-346` |
| Request inner panel | 12 | none | `service_requests_screen.dart:401` |
| Stat card | 20, 1 px border at status colour 12% | `#0A4FA8` 6%, blur 18, offset (0, 6) | `dashboard_stat_card.dart:25-44` |
| Dialogs (message, confirm) | 24, max width 380, inset 32 | blur 28, offset (0, 12) | `message_dialog.dart:50-61`, `confirm_dialog.dart:22-33` |
| Toast | 14, elevation 6 | none | `app_toast.dart:45-50` |
| Chips | 12 | none | `status_chip.dart:28` |
| Segmented filter track | 14 (thumb 11) | thumb blur 6, offset (0, 2) | `status_filter_tabs.dart:37,55-60` |
| Carousel dots | 4, active 22x8, inactive 8x8 | none | `featured_services_carousel.dart:165-168` |
| Home and provider header bottom corners | 28 | `#0A4FA8` 20%, blur 20, offset (0, 8) | `home_screen.dart:314-318`, `provider_tab_header.dart:34-40` |
| Compact header bottom corners | 18 | `#0A4FA8` 20%, blur 12, offset (0, 4) | `compact_app_header.dart` |
| Auth and profile white sheet top corners | 36 | none | `auth_card_scaffold.dart:69`, `curved_profile_header.dart:59` |
| Logo tile on Landing | 36 | blur 20, offset (0, 8) | `landing_screen.dart:58-63` |
| Bottom nav | no radius, full-bleed | blur 24, offset (0, -6), `#0A4FA8` 35% | `bottom_nav.dart:77-79` |

### 2e. Spacing

| Item | Value | Source |
|---|---|---|
| Page padding on Home | 16 all sides | `home_screen.dart:214` |
| Home grid | max tile width 200, aspect 1.3, gap 20 / 20 | `home_screen.dart:238-243` |
| Featured carousel height | 200, slide margin 4 horizontal | `featured_services_carousel.dart:62,72` |
| Home section gaps | 12 under titles, 24 between blocks, 40 at the end | `home_screen.dart:222-272` |
| Auth card inner padding | L/R 24, top avatar radius + 24, bottom 24 | `auth_card_scaffold.dart:75` |
| Auth input padding | 18 horizontal, 16 vertical; label bottom gap 8 | `auth_card_scaffold.dart:146,162` |
| Auth title block | 6 gap to subtitle, 32 before the form | `auth_card_scaffold.dart:88,97` |
| Request card padding | L/R 14, top 16, bottom 12; list padding 16 / 14 / 16 / 4 | `service_requests_screen.dart:360,191` |
| Stat card padding | 16 / 14 / 14 / 14 | `dashboard_stat_card.dart:48` |
| Empty state | 32 horizontal, 40 vertical; icon circle 72 | `empty_state_placeholder.dart:35,40-41` |
| Dialog padding | 24 / 26 / 24 / 20 | `message_dialog.dart:64` |
| Safe area | Headers use `SafeArea(bottom: false)`; bottom navs use `SafeArea(top: false)` so the gradient extends under the system bar (`bottom_nav.dart:83`, `provider_tab_header.dart:58`). Auth header height is 85 plus (top inset minus 20) (`auth_card_scaffold.dart:27-34`). |

### 2f. Gradients

| Where | Direction | Stops | Source |
|---|---|---|---|
| Landing and Splash background | top-left to bottom-right | `#0A4FA8` to `#016EE3` | `landing_screen.dart:14-26` |
| Home, Requests banner, provider tab headers, compact header | top-left to bottom-right | `#0A4FA8` to `#016EE3` | `home_screen.dart:310-313`, `provider_tab_header.dart:30-33`, `compact_app_header.dart` |
| Both bottom navs | top-left to bottom-right | `#0A4FA8` to `#016EE3` | `bottom_nav.dart:70-74` |
| Profile header | optional list, else solid colour | per screen | `curved_profile_header.dart:40-47` |
| Stat cards | soft diagonal tint of the card colour (stops UNVERIFIED) | | `dashboard_stat_card.dart` |

Decorative glow circles sit inside headers: one 150 px at top -40 / right -30 in accent at 14% alpha, one 130 px at bottom -60 / left -30 in white at 6% (`provider_tab_header.dart:46-56`). Auth screens use two white 8% circles, 180 px and 160 px (`auth_card_scaffold.dart:37-50`).

### 2g. Motion

| Item | Value | Source |
|---|---|---|
| Timing constants | quick 180 ms, medium 260 ms, slow 320 ms; curves easeOut and easeOutCubic | `motion.dart` |
| Tab switch (client) | fade plus scale from 0.96 over 260 ms, fastOutSlowIn | `main_navigation_shell.dart` (AnimatedSwitcher) |
| Nav pill | background and glow animate over 260 ms; icon swaps outline to filled with scale 0.9 to 1.0 over 180 ms | `bottom_nav.dart:143-170` |
| Carousel | auto-advance every 4 s; active dot widens to 22 px | `featured_services_carousel.dart:35,165` |
| Tile entrance | 60 ms stagger per index, scale over 300 ms | `main_category_card.dart:24-26` |
| Tile to sub-category | container transform 380 ms (`OpenContainer`) | `main_category_card.dart:38` |
| Request card to detail | container transform (`OpenContainer`) | `service_requests_screen.dart:330` |
| Reduced motion | all of the above drop to zero duration when the OS asks | `motion.dart` |
| Loading | circular progress indicator inside buttons (20x20) and a centred spinner for lists (exact style UNVERIFIED); no skeletons were seen | `auth_card_scaffold.dart:304-305` |

### 2h. Dark mode

None. `MaterialApp` sets only `theme`, with no `darkTheme` and no `themeMode` (`main.dart:155-158`), so the app stays light whatever the phone setting is. No dark screenshots exist.

## 3. Screens

Measurements below that are not cited came from the screenshots (CSS px at 390 wide) and are marked "(screen)". Behaviour at 360 px is from the 360 captures where they exist; otherwise it is UNVERIFIED.

### 01 Landing (`01-landing.png`)
- Full-screen gradient, section 2f. Three decorative circles: top-left, top-right, bottom-right (screen).
- Logo tile, `app_icon.png` at 168 px, shrinking to 40% of window width on narrow screens (`breakpoints.dart:14-21`), radius 36, shadow in 2d.
- Title "Sahulat Ghar Tak" bold, white; subtitle "Quality Services Delivered to Your Doorstep", white 70% (screen). Gaps 24 then 8 (`landing_screen.dart:72,81`).
- 48 gap, then two buttons side by side, 16 apart, each 52 high, radius 14, 16/600 label (`landing_screen.dart:88-130`). Register is filled light cyan with dark blue text; Login is outlined white (screen).
- 18 gap, then two underlined text links "Continue as Guest" and "Contact Us", 14.5/600 (`landing_screen.dart:135-160`). Horizontal padding 32 (`:52`).
- 360 px: layout unchanged, the logo is 144 px (0.4 x 360).

### 02 Login (`02-login-empty.png`, `02-login-error.png`)
- Indigo header 85 px high plus the status-bar inset, two white 8% circles, back arrow at top-left. White sheet with 36 px top corners.
- 92 px white-ringed avatar circle (radius 46) straddling the header edge (`auth_card_scaffold.dart:25`), shadow blur 12 offset (0, 4) (`:119-122`), 6 px ring padding (`:125`).
- Title "Login Account" 24 bold, subtitle "Welcome back to app" 14 grey; 32 gap to the form.
- Labels "Mobile Number", "Password" 14/600. Fields: fill `#F5F5F7`, radius 14, hint "03XX-XXXXXXX" and "Enter your password"; password has a visibility-off icon (screen).
- "Forgot password?" right-aligned indigo link. Login button 52 high, indigo, radius 14. "Not register yet? Create account" centred, link in indigo.
- Error state: red border and red "Required" caption under each field (red-accent 12 px, `auth_card_scaffold.dart:212`); layout shifts down about 40 px.
- Focus: 1.5 px indigo border and slightly darker fill (screen).

### 03 Customer registration (`03-customer-registration*.png`)
- Same shell as Login with an add-person avatar icon. Fields in order: Full Name, Mobile Number, Gender, Password, Confirm Password, Terms block, Register button.
- Gender: three equal pills with 12 px gaps, radius 14, 14 vertical padding, icon over label 13/600 (`auth_card_scaffold.dart:191,238-265`). Selected is indigo with white text; unselected `#F5F5F7`.
- Terms and Conditions: rounded grey panel with chevron header (collapsed by default), expands to a bold "Customer Terms & Conditions" heading and numbered paragraphs, then a checkbox "I agree to the Terms and Conditions" (widget `terms_and_conditions_section.dart`; measurements UNVERIFIED). Register stays disabled (grey) until the box is ticked (`03-customer-registration-b.png`).
- 360 px: no change besides narrower fields.

### 04 Notifications inbox (`04-*.png`)
- Branded header with back arrow, title "Notifications", subtitle "N unread" or "You are all caught up"; "Mark all read" action at the right only while there are unread items (`04-provider-notifications.png`).
- Card per notification: radius about 16, 1 px border `#E6EBF2` when read, plain white (`notifications_screen.dart:179,192`). Left icon tile with the type icon and colour from section 2c and 2a.
- Unread: card fill is the type colour at 9% over white, a 5 px left stripe in the type colour, a "NEW" pill, bold title, and the timestamp in the type colour (`notifications_screen.dart:178-201,284`). Read: title in dark slate, body `#475569`, timestamp grey.
- Body wraps; timestamps show `dd-MM-yyyy, hh:mm a` local time (screen).
- Titles from the backend can contain an emoji, which Chrome may show as a blank box until its emoji font loads; phones render them.
- Empty state uses the shared placeholder (`notifications_screen.dart:108`); not captured.

### 05 Client Home (`05-*.png`)
- Header is a fixed `appBar` of 176 px plus the status bar inset (`home_screen.dart:204-206`), not collapsing: only the body scrolls beneath it. Gradient and 28 px bottom corners.
- Row: logo 46x46 (`home_screen.dart:348-349`), 12 gap, title 18/800 and subtitle 12/500, bell with badge on the right. Padding 20 / 8 / 20 / 28 (`:341`). Guests see no bell.
- Search field 18 px below (`:382`): white, radius 16, 15 px text, `search_rounded` icon in brand blue, vertical content padding 14 (`:388-415`).
- Suggestions: white overlay panel below the field, capped at 560 px (`breakpoints.dart:9`), two-line rows with a service icon tile (screen).
- Body (16 px padding): section title "Services" with a 4x18 blue bar, radius 2, 8 px gap (`:490-496`). Two-column grid of tiles; each tile fill is the service colour at low alpha with a 1 px tinted border, icon above a 16/700 label (`main_category_card.dart`).
- Thin divider (height 1, `:517`), "Featured Services", then the carousel with dots.
- "Customer Reviews" horizontal list of white cards (avatar initial, name, five stars, text), then an outlined "Contact Us" button with `support_agent_rounded` (`:267`).
- Search open: `05-client-home-search.png`. Guest dialog: `05-guest-login-required.png` (appears when a guest taps a service).
- 360 px: subtitle wraps to two lines and the search field moves about 16 px down inside the same 176 px header (`05-client-home-small.png`); tiles stay two per row.
- 1024 px: content is centred at max 840 px (`home_screen.dart:217`, `breakpoints.dart:5`) and the grid becomes four tiles per row; the header, the search field and the bottom nav stay full width (`05-client-home-wide.png`).

### 06 Sub-categories (`06-subcategories.png`)
- Coloured header in the service colour with a white-ringed icon avatar, title "Home Maintenance", subtitle "Choose a service to get started". Three-column grid of white tiles, radius 14, 1 px border `#E5E5EA`, 10 / 8 padding, circular icon above a 12/600 two-line label (`subcategory_card.dart:40-78`). A "Coming soon" tile exists in code (the 10 px black-38% caption) but none appeared on production data.

### 07 Service request form (`07-request-form*.png`)
- Header and avatar take the service colour. Title is the category name; subtitle "Fill in the details to request this service".
- Order: Estimated Budget card, Service Address dropdown, Service Title dropdown, optional "Specify Service" (only for Other), Service Description, Mark as Urgent switch row, On-site Contact block, Remarks, Preferred Date and Time, Submit Request.
- Budget card: accent at 8% fill, 1 px accent border at 25%, `payments_outlined` 18 px (`service_request_form_screen.dart:239-245`). Text "Select a service title below to see the estimate" until a title is chosen, then the estimate or "No estimate set for this service yet".
- Dropdowns open a white elevated menu below the field with the options plus "Other (not listed)" (`07-request-form-titles.png`).
- Switches use the accent for the active thumb (`service_request_form_screen.dart:381,410`). "Same as my account" on shows "Provider will contact you: name, number"; off reveals contact person and number fields.
- Preferred Date and Time are two half-width tiles with calendar and clock icons. Date and time pickers were not captured (section 8). Android uses Material pickers; iOS uses a Cupertino sheet (`platform_date_picker.dart:15-24`).
- Submit button: full width, accent fill, white label (screen).

### 08 My Requests (`08-my-requests*.png`)
- Banner: gradient with 28 px-class bottom corners, a 48 px frosted icon tile, title "My Service Requests", subtitle "Track your requests here" (screen).
- Segmented tabs "Active (n)", "Completed (n)", "Cancelled (n)": track 44 high, 4 padding, radius 14, selected thumb white with radius 11 (`status_filter_tabs.dart:33-60`).
- Empty state per tab: 72 px tinted circle with icon, 16/800 title ("No active requests", "No completed requests", "No cancelled requests"), 13.5 grey body (`empty_state_placeholder.dart`).
- Guest: lock icon, "Log in to see your requests", outlined "Log In" button.
- Full card anatomy from code: title 16.5/800, status line 12.5/600 with the status icon, description 13.5, a `#F6F8FC` panel (radius 12, padding 10) with location, date, urgent pill and budget rows, a created-at caption 11.5, then passcode, call, WhatsApp, cancel and delete controls (`service_requests_screen.dart:333-520`). Not captured because the account has no requests.
- Wide: tabs and empty state centre in an 840 px column; banner and nav full width (`08-my-requests-wide.png`).

### 09 Dialogs (`09-*.png`)
- Message and confirm dialogs: 24 radius, max 380 wide, 32 side inset, padding 24 / 26 / 24 / 20, 56 px tinted icon circle, 16 gap, 17/800 title, grey body, then buttons (`message_dialog.dart:50-83`, `confirm_dialog.dart:22-54`). Confirm shows a white outlined Cancel and a blue filled action side by side. Error uses a red filled OK.
- Toast: dark `#1A2233` floating bar, radius 14, 16 / 12 margin, 14 / 12 padding, icon plus 13.5/600 text, 3 s (`app_toast.dart:19-63`). Not captured.
- Cancel-reason, passcode and reject-reason dialogs: not captured (section 8).

### 11 Profile, Edit Profile, Add Address
- Customer Profile: blue header 150 px (`curved_profile_header.dart:21`), 96 px avatar (radius 48) with initial, white sheet with 36 px top corners. Then two info tiles (Account Type, User ID), a Mobile Number row, outlined Edit Profile, Addresses with an "Add" button, address cards with a "Set location" chip, Preferences switch (24-hour time), then Become a Provider (filled blue), Delete Account (red outline), Contact Us (outline) and a Privacy Policy link.
- Guest Profile: icon, "You're browsing as a guest", filled Log In and outlined Create Account.
- Edit Profile (customer): indigo header, Full Name, gender pills, Save Changes.
- Add Address: indigo header, "Pin your location" map card (about 170 high, expand button, blank on web), Save Pin outline button, then Address Title, Full Address, Area, City fields. Map and fullscreen pin not captured.

### 12 Verification Pending: not captured (section 8).

### 13 Provider documents (`13-documents*.png`)
- Compact header with back arrow, title "My Documents", subtitle "Verified". Green banner "Your documents are verified." Then slots Profile Photo, CNIC Front, CNIC Back, Police Verification (optional), each a full-width rounded image with a label strip: "Tap to change" or "Verified - locked" (dark translucent bar at the bottom of the image), and help text. Save Changes disabled when nothing changed. Empty and filled-unverified slots and the capture sheet were not captured.

### 14 Provider Home (`14-*.png`)
- ProviderTabHeader-style header, 112 px preferred height plus inset (`provider_tab_header.dart:26`), with logo, "Welcome back, Name!", status subtitle and an Online/Offline pill at the right: radius 20, padding 10 / 6, `wifi_tethering` icon, 11 px label (`provider_home_tab.dart:122-147`).
- Notifications entry card: bell tile, "Notifications", an indigo count badge, chevron.
- Section "Overview" and a two-column grid of five stat cards (section 2d): Available Requests, Active Bookings, Completed Today, Average Rating, Wallet Balance. Each card has a tinted icon square (padding 9, radius 13), an arrow in the corner, a 21/800 value and a 12 label.
- Availability dialog: blue header with clock tile, "Available Timing", two rows (From, To) with time values in blue, Skip and Save buttons. Choosing online opens this dialog.
- 360 px: stat cards stay two per row and get slightly narrower; the last row is cut off by the nav (`14-provider-home-small.png`).

### 15 Provider Requests (`15-*.png`)
- Header "Incoming Requests" with a history icon at the right (opens Rejected Requests). Empty state "No requests yet / New job assignments from staff will show up here as soon as they come in." Request cards and the reject-reason dialog were not captured.

### 16 Provider Bookings (`16-bookings*.png`)
- Header "My Bookings", subtitle "No bookings yet". Empty state "Bookings appear here once you accept a service request from a customer." The Active / Completed / Cancelled tabs and the booking cards only render when bookings exist: not captured.

### 17 Booking detail, completion dialog: not captured (section 8).

### 18 Wallet (`18-wallet.png`)
- Header "Wallet", subtitle "Balance and payment history". White summary card: green down-arrow tile, "Company owes you", amount in green 800-weight, a divider, then "Pending Payout" with an hourglass and blue amount. "Transaction History" title, then rows with a circular plus tile, name, date `dd-MM-yy hh:mm a` and a green "+ Rs 15,000". The debit state and empty state were not seen.

### 19 Provider Profile and related (`19-*.png`)
- Profile: header 150 px with title "Provider Profile" and a logout icon, 96 px photo avatar with white ring, name with a blue verified badge, category line, star rating. Two tiles (Account Type, User ID). "Provider Details" list: icon, label, value rows with hairline dividers, a green "Verified" chip (`status_chip.dart`), then Preferences switch, Availability card (Status switch, Available Timing), then Edit Profile, My Documents and Categories (two half buttons), Switch to Customer (filled), Delete Account (red outline), Contact Us, Privacy Policy.
- Edit Profile (provider): compact blue header, Full Name, gender pills (blue selected), CNIC, Experience stepper with minus and plus circles, City dropdown, Save Changes.
- Categories and Services: "My Categories" with Edit Primary and Edit links, rows with a "Primary" chip, "My Services" grouped under blue category headings with a tag icon per service.
- Category picker: search field in the header, grouped list with circular check marks, and a bottom button "Done (3 of 3 selected)".
- Primary-category dialog: title "Choose Your Primary Category", radio list, blue Confirm. It cannot be dismissed without confirming.

## 4. Bottom navigation

| Property | Client (3 tabs) | Provider (5 tabs) |
|---|---|---|
| Items | Home, Requests, Profile | Home, Requests, Bookings, Wallet, Profile |
| Bar height | 78, plus the bottom safe-area inset | 78, plus the inset |
| Source | `bottom_nav.dart:40,86` | `provider_bottom_nav.dart:68` |
| Background | gradient `#0A4FA8` to `#016EE3`, top-left to bottom-right | same |
| Corner radius | none; the bar is edge to edge, with no floating gap | same |
| Shadow | `#0A4FA8` 35%, blur 24, offset (0, -6) (`:77-79`) | same (`:59-61`) |
| Item layout | `Expanded` equal widths, vertical padding 4, pill then 4 px gap then label | same |
| Selected pill | padding 18 x 6, radius 20, fill `#4FC3F7` at 18%, glow `#4FC3F7` 35%, blur 14, spread -2, no offset (`:146-157`) | same (`:123-136`) |
| Selected icon | filled Rounded variant, 24 px, `#4FC3F7` | same |
| Selected label | shown, white, 700 weight, 11.5 px, letter spacing 0.2 (`:180-182`) | 10.5 px (`:160-162`) |
| Unselected icon | Outlined variant, 24 px, white 70% | same |
| Unselected label | shown, white 54%, 500 weight | same |
| Tap target | full cell: one third of the width by 78 high (client), one fifth by 78 (provider); ripple is a stadium at accent 20% (`:114`) | same |
| Badge | none on the nav | none |
| Keyboard | not handled in the widget; the bar is the Scaffold's bottom bar and in screenshots with keyboards it was not seen (UNVERIFIED) | same |
| Selected-state screenshots | `05-client-home.png` (Home), `08-my-requests.png` (Requests), `11-customer-profile.png` (Profile) | `14-provider-home-online.png`, `15-provider-requests-empty.png`, `16-bookings.png`, `18-wallet.png`, `19-provider-profile.png` |

Switching tabs on the client side swaps the body with a fade and 0.96 to 1.0 scale (section 2g). On the provider side the tab swap is by the dashboard screen (`provider_dashboard_screen.dart:125`; transition UNVERIFIED).

## 5. Headers

| Header | Height | Padding | Gradient and corners | Content and trailing widgets | Scroll behaviour |
|---|---|---|---|---|---|
| Client Home | 176 plus inset (`home_screen.dart:204-206`) | 20 / 8 / 20 / 28 (`:341`) | `#0A4FA8` to `#016EE3`, bottom radius 28 (`:310-322`) | 46 px logo, title 18/800, subtitle 12/500, bell with badge, search field | Fixed. It is the Scaffold `appBar`; content scrolls under it. |
| My Requests banner | about 128 on screen; exact value UNVERIFIED | | same gradient, bottom radius 28-class (screen) | 48 px frosted icon tile, title, subtitle | Fixed above the tab track (screen). |
| ProviderTabHeader | 112 plus inset (`provider_tab_header.dart:26`) | L 20 (12 if a leading widget), T 4, R 12, B 16 (`:61-62`) | same gradient, bottom radius 28 (`:34-40`) | Title 20/800, subtitle 12.5/500 at 75%, optional leading (back arrow) and trailing (Online pill, history icon, logout) | Fixed. |
| CompactAppHeader | 56 plus inset (`compact_app_header.dart`) | back button 48 wide, 4 right | same gradient, bottom radius 18 | Back arrow, title 17/700 | Fixed. |
| Auth and Edit Profile | 85 plus (inset minus 20) (`auth_card_scaffold.dart:27-34`) | | solid colour (indigo or service colour) with two glow circles; sheet top radius 36 | Back arrow, 92 px avatar | Scrolls with the sheet (the form scrolls; the header stays) (screen). |
| Profile (customer and provider) | 150 (`curved_profile_header.dart:21`) | | solid or gradient, sheet top radius 36 | Title, logout icon, 96 px avatar | Avatar stays pinned over the sheet while the sheet scrolls (`19-provider-profile-b.png`). |

## 6. Assets (`screenshots/assets/`)

| File | Pixels | Used for |
|---|---|---|
| `app_logo_transparent.png` | 1024 x 1024 RGBA | In-header logo (Home 46 px, provider home), update screens (`home_screen.dart:350`, `provider_home_tab.dart:118`, `update_required_screen.dart:139`) |
| `app_icon.png` | 1024 x 1024 RGB | Landing and Splash logo tile, launcher icon (`landing_screen.dart:68`, `splash_screen.dart:125`) |
| `app_icon_circle.png` | 1024 x 1024 RGBA | Round icon variant; usage in `lib` UNVERIFIED |
| `app_icon_foreground.png` | 1024 x 1024 RGBA | Android adaptive icon foreground on `#016EE3` (`pubspec.yaml:70-71`) |
| `app_icon_splash.png` | 1024 x 1024 RGBA | Native splash image on `#016EE3` (`pubspec.yaml:73-79`) |
| `carpanter01.jpg` | 311 x 162 | Featured and category photo for carpenter (`category_images.dart:4-6`) |
| `electrician01.jpeg` | 2560 x 1707 | Electrician photo (`category_images.dart:2`) |
| `painter01.jpg` | 643 x 360 | Paint photo (`category_images.dart:7`) |
| `plumbing01.jpeg` | 1024 x 658 | Plumbing photo (`category_images.dart:3`) |
| `banner01.png` | 2064 x 512 RGBA | Bundled but no reference found in `lib`: UNVERIFIED |

Other facts:
- The Landing background is the gradient plus circles in code, not an image. There are no illustration or empty-state images; empty states are icons in a tinted circle.
- No font files are bundled.
- Web launcher icons exist in `web/icons` (192 and 512, normal and maskable). Not copied.
- The notification sound files (`assets/notification-sounds`) are app-only and were not copied. No store or signing material was exported.
- Icon names are in section 2c.

## 7. Open questions answered

1. **Primary colour for buttons and links:** indigo `#6366F1` on the auth group only; brand blue `#016EE3` (dark end `#0A4FA8`) everywhere else after login, for both roles. Request form uses the tapped service's colour. See 2a.
2. **Font:** the default Material 3 typography, no custom family. Roboto on Android, San Francisco on iOS. Roboto in the web screenshots. See 2b.
3. **Dark mode:** none (`main.dart:155-158`).
4. **Android versus iOS differences beyond date pickers:** the font (above); the system bars (status bar and nav bar are transparent with light icons on Android, `bottom_nav.dart:56-64`); the map (Google Maps on both, blank in the web captures); the default page transition (Flutter's Material route gives the platform transition: slide on iOS, Android's own on Android); the push permission prompt. Anything else is UNVERIFIED, because no Android or iOS device was run for this document.
5. **Max content width on tablets:** 840 px for scrolling content (`breakpoints.dart:5`), 560 px for overlay panels like search suggestions (`:9`). The bottom nav does not stay at that width; it spans the full window, and so do the headers (`05-client-home-wide.png`). Only client Home and My Requests were captured wide; provider tabs were not.
6. **Screens and states a normal user sees that are not in section 3:** Splash (gradient plus logo, `splash_screen.dart:125`), Forgot Password, Reset Password, OTP Verification, Provider Registration, Contact Us, the update-required full screen and the push-driven update overlay (`update_required_screen.dart`, `update_block_host.dart`), the optional update dialog, the delete-account password dialog (`delete_account_dialog.dart`), Verification Pending, the in-app live camera capture screen, the Service Providers screen, the "Coming soon" tile, and the offline/error cards (`home_screen.dart:445-473`).

## 8. Not captured, and why

| Missing | Reason |
|---|---|
| Date and time pickers | The headless Chrome session never opened them; the click had no effect. Android uses Material pickers and iOS uses Cupertino sheets (`platform_date_picker.dart:15-24`). |
| Request cards with data, bounced-back notice, urgent pill, budget, passcode/call/WhatsApp row, cancel/delete buttons, compact card, error state | The client test account has no requests, and creating one would write to production. |
| Request detail (all steps, cancelled state, provider card) | Needs a request; same reason. |
| Cancel-reason, passcode dialogs, toasts | Triggered only by actions that write or need data. |
| Provider request cards, reject-reason dialog | The provider has no incoming requests; staff assign jobs. |
| Provider bookings in each status, booking detail, completion dialog with labour and material rows, client location map | No bookings exist on the account. |
| Wallet debit state and empty state | The account has one credit adjustment only. |
| Verification Pending, document upload with empty and filled slots, document capture sheet, live camera | Account is already verified; opening upload would risk replacing documents. |
| Provider registration, OTP, forgot and reset password, Contact Us, Splash states | Not reached; they would send real OTP SMS or create accounts. |
| Unread notifications on the customer side, notification empty state | Everything was read; the provider side shows the unread style. |
| Map with tiles, pin and fullscreen pin | Google Maps does not render on this web build (blank grey card in `11-add-address.png`). |
| Real status bar and Android/iOS system navigation | Captures are Chrome windows, not devices. |
| Tablet width for provider tabs and for other client screens | Only client Home and My Requests were captured wide. |
| Keyboard-open behaviour of the nav | Not observable in this setup. |
| Redaction | Not applied, by the user's decision (section 0). |
