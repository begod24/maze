---
name: suno-music-prompt
description: Write a ready-to-paste Suno prompt for No Way Out music — menu/lobby theme, layered run ambience (stems), entity stingers, exit and finale — built from the shared base style (dark ambient, no vocals, voice-safe mix), plus stem/layer instructions and where the files go. Use whenever the user asks for a track, a stinger or music for a game moment.
---

# Suno music prompt

Music design is in `Docs/GDD.md` §14 and the music system in `Docs/TechnicalOverview.md` §17. Always write **one complete, ready-to-paste prompt**. Never hand the user pieces to combine.

## Base style (shared by every track)

- **Genre:** dark ambient, drone horror, cinematic sound design. Vast, cold, unsettling, hollow.
- **Key:** D minor (Phrygian colour allowed). Every track uses it, so layers and stingers fit together. If the user picks another key, update this skill and GDD §14.
- **Tempo:** mostly free-time. When a pulse is needed, 60 BPM.
- **Palette:**
  - deep analog drones, sub-bass rumble,
  - distant metallic resonances (bowed metal, concrete reverb tails),
  - slow granular textures, reversed swells,
  - detuned felt piano or music box (**only** for the menu and finale melody),
  - low heartbeat-like pulses, faint tape hiss.
  - Signature texture: the room tone of an endless concrete void, with echoes falling into the distance.
- **Voice-safe mix:** energy lives in the sub/low and the airy highs, and the 300 Hz – 3 kHz range stays clear for teammates' voices. Wide stereo, dark and dry enough not to blur footsteps.
- **Never:** vocals, choir, whispers, breaths, speech or anything voice-like. Voices are gameplay (teammates, the Voice entity) and the music must never be mistaken for them. No drums or beats, no drops, no fade-outs.
- **Loops:** the ending flows back into the start.

## Per moment

| Moment | Energy | Content |
|---|---|---|
| **Menu and lobby** | low-medium | The **main motif** on detuned felt piano or music box over a soft drone. Sad, curious, slightly wrong. A steady loop, calm but uneasy |
| **Run ambience** (one track, delivered as stems) | low → high by layer | **Bed:** drone and void room tone. **Pulse:** slow sub pulses at 60 BPM (separation, void reshaping). **Tension:** dissonant bowed metal and strings, rising clusters (entity nearby). **Air:** high shimmering harmonics, glassy light (near the exit or going up) |
| **Entity stingers** | sharp, 2–6 s, one-shot | One per entity, starting fast and ending with a short tail (no loop): <br>• **Observer:** a bright glassy tone with CRT hum, freezing in place <br>• **Distortion:** a reversed swell into a crunch <br>• **Hands from the door:** a low wooden knock and a creaking cluster <br>• **Doppelganger:** the main motif detuned and slightly wrong <br>• **Voice:** a granular, non-vocal smear moving through stereo <br>• **Player death:** a deep falling drone into silence |
| **Exit and finale** | medium, release | The main motif returns and resolves (still in D minor, softer). The "what each of you saw" screen uses a lighter, curious variation |

For a specific entity's stinger, first read its row in GDD §6 and turn its behaviour into the sound.

## Prompt format (Suno)

- **Style of Music:** comma-separated descriptors, most important first (genre, key, tempo or free time, mood, then instruments, then mix and arrangement). Under about 600 characters. Positive descriptors only, because Suno adds whatever it reads, even after "no". No artist or song names.
- **Exclude Styles:** `vocals, choir, whisper, spoken word, lyrics, breath, drums, beat, EDM, drop, build-up, upbeat, fade out`
- **Lyrics:** Instrumental **on**, plus bracketed meta tags that keep one even section all the way through, e.g. `[Instrumental]`, `[Intro: drone fades in]`, `[Main Loop: steady, no change in energy]`, `[Outro: flows back into the start, no fade]`. Stingers use `[Short one-shot hit]`, `[Tail: 2 seconds]`.
- **Run ambience:** ask for **clearly separated** instruments (drone, sub pulse, bowed metal/strings, high shimmer), so Suno's stem split maps onto the Bed, Pulse, Tension and Air layers.
- Suggest settings: **Weirdness** around 40–55%, **Style Influence** around 60–70%.
- Remind the user to cut the loop:
  - ambient layers: a seamless 60–120 s section,
  - the menu theme: 8 or 16 bars at 60 BPM (1 bar = 4 s),
  - stingers: trimmed to the hit plus its tail.

## After generation

- **Stems and files:**
  - `Assets/_Project/Audio/Music/<Track>/MUS_<Track>_<Layer>.wav`, with Layer one of Bed, Pulse, Tension, Air, Full,
  - stingers in `Audio/Music/Stingers/MUS_Stinger_<Entity>.wav`.
- **Import settings:**
  - long loops: Vorbis, quality around 70, Load Type Streaming,
  - stingers: Decompress On Load.
- Add a row to `Docs/Decisions.md` if the style rules changed. Note the chosen prompt next to the track (`Audio/Music/<Track>/prompt.txt`) so it can be regenerated.
