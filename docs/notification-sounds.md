---
status: current
---

# Notification sounds: spec and how to add them

Status: **sounds chosen (Pixabay) and named on the backend** (`NotificationSounds.cs`). The files are **not stored in this repo**: they
are bundled in the Flutter app (`res/raw/*.ogg` for Android, `ios/Runner/*.wav` for iOS) as `job_request`,
`booking_update` and `announcement`. The backend only sends their names. They only play once the Flutter build
contains them and the channels are created with a `sound` (see `docs/flutter-changes.md`). Builds without the files
fall back to the device default sound.

Sources (all Pixabay Sound Effects, pixabay.com/sound-effects, Pixabay Content License; re-check the licence before
release):

| Sound | Channel | Pixabay file |
|---|---|---|
| `job_request` | `job_requests_v2` | universfield, "new-notification-033" (id 480571) |
| `booking_update` | `booking_updates_v2` | universfield, "new-notification-018" (id 363746) |
| `announcement` | `announcements_v2` | soundshelfstudio, "ui-chime-confirm" (id 567486) |

## 1. Sounds to pick

One per channel. Each is a short, single clean sound (a chime or soft "ding"), not a melody or loop.

| Channel (`channel_id`) | Used for | What to look for | Length |
|---|---|---|---|
| `job_requests_v2` | New job for a provider (`job_assigned`) | The most attention-grabbing of the three: a bright double chime or a short two-note "ding-dong". Providers need to notice it when the phone is in a pocket. | 1.5 to 3 s |
| `booking_updates_v2` | Accepted, started, completed, cancelled, reassigning | A soft, pleasant single pop or chime. Heard often, so gentle. | 0.5 to 1.5 s |
| `announcements_v2` | App update / staff broadcast | Calm and neutral, quieter than the other two. | 0.5 to 1.5 s |

Quality checklist: starts immediately (no silent lead-in), no long reverb tail, not clipped at the end, peak level
around -3 dB (not distorted, not whisper quiet), no vocals or music that gets annoying, and the three are clearly
different from each other. Keep each file small (under about 100 KB).

Licence: it must allow commercial use inside an app without attribution, or you must credit it in the app. Check each
file's licence, do not assume a site is all free.

## 2. Where to find them

- Pixabay Sound Effects (pixabay.com/sound-effects): free for commercial use, no attribution required under the Pixabay
  Content License. Search "notification", "chime", "ding", "pop".
- Mixkit (mixkit.co/free-sound-effects/notification): free under the Mixkit licence, a good notification category.
- Freesound (freesound.org): huge library. Filter by licence **CC0** (no attribution). CC-BY needs credit; avoid
  non-commercial ones.
- Google Material sound resources (m3.material.io, search "sound resources"): notification and alert sounds made for
  Android apps, with a stated licence.
- Zapsplat (zapsplat.com): free with an account and attribution, or paid for no attribution.

I am recalling these from memory, so open each site and read its current licence page before using a file.

## 3. File formats

| Platform | Format | Notes |
|---|---|---|
| Android | `.ogg` (preferred) or `.mp3` / `.wav` | Lowercase letters, digits and underscores only, e.g. `job_request.ogg`. Goes in `android/app/src/main/res/raw/`. |
| iOS | `.caf`, `.aiff` or `.wav` (linear PCM, IMA4, uLaw or aLaw) | 30 seconds or less or iOS plays the default instead. Goes in the Runner target (added to the Xcode project, Copy Bundle Resources). On a Mac: `afconvert -f caff -d ima4 in.wav job_request.caf`. |

Suggested names: `job_request`, `booking_update`, `announcement` (same base name on both platforms).

## 4. Adding them (when the files exist)

1. **Flutter** (steps are in `docs/flutter-changes.md`, section "Notification appearance, channels and sounds"):
   put the files in `res/raw/` and the iOS bundle, and create the channels with a `sound`. Android channel sound is fixed
   once a channel exists on a device, so a changed sound needs new channel ids. The current ids are already the `_v2` ones (the first channels had no sound).
   The app also accepts the ids without `_v2` in foreground banners, for compatibility only; the backend sends `_v2`.
2. **Backend:** set the names in `HomeServicesPortal/Services/NotificationSounds.cs`
   (done: `job_request` for Android, `job_request.wav` for iOS, and so on for each channel). The Android name only matters on Android 7 and older; on 8+ the channel plays its own sound.
3. Test each channel with `/Admin/PushTester` using "Send on the type's Android channel" on a build that has the files.
4. Make sure `Notifications:AndroidChannelsEnabled` is on in production once that build is live.
