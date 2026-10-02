---
status: current
---

# Notification sounds: spec and how to add them

Status: **no custom sounds yet.** Pushes use the device's default sound. The backend and the app are prepared so adding
sounds later is a file drop plus a few lines (below). Do not ship this half-done: sounds only work when the files are in
the app build **and** the names are set on the backend.

## 1. Sounds to pick

One per channel. Each is a short, single clean sound (a chime or soft "ding"), not a melody or loop.

| Channel (`channel_id`) | Used for | What to look for | Length |
|---|---|---|---|
| `job_requests` | New job for a provider (`job_assigned`) | The most attention-grabbing of the three: a bright double chime or a short two-note "ding-dong". Providers need to notice it when the phone is in a pocket. | 1.5 to 3 s |
| `booking_updates` | Accepted, started, completed, cancelled, reassigning | A soft, pleasant single pop or chime. Heard often, so gentle. | 0.5 to 1.5 s |
| `announcements` | App update / staff broadcast | Calm and neutral, quieter than the other two. | 0.5 to 1.5 s |

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
   once a channel exists on a device, so use new channel ids if you ever change a sound (for example `job_requests_v2`).
2. **Backend:** set the names in `HomeServicesPortal/Services/NotificationSounds.cs`
   (`job_request` for Android, `job_request.caf` for iOS, and so on for each channel). Nothing else changes.
3. Test each channel with `/Admin/PushTester` using "Send on the type's Android channel" on a build that has the files.
4. Make sure `Notifications:AndroidChannelsEnabled` is on in production once that build is live.
