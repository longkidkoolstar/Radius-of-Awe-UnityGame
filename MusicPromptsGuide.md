# Music Generation Guide: "Radius of Awe"

This guide contains the exact style tags, parameters, and prompts used to generate the dynamic, crossfading soundtrack for the game using AI music tools like Suno or Udio. 

To keep the Mundane and Wonderous layers cohesive during transition crossfades, both tracks for a level must share the same **BPM (tempo)** and **Musical Key**.

---

## 🎹 Level 1: "The Portal Ascent"
* **Musical Key:** A minor (moody, mysterious)
* **Tempo:** 85 BPM (slow, relaxed)

### 🌲 Level 1 - Mundane (Bleak/Hollow)
* **Style Tags (Genre):** `minimalist ambient, quiet acoustic guitar plucking, soft reverb, 85 bpm, key of a minor, loopable, calm, spacey, indie game soundtrack`
* **Prompt Description:** `A slow, quiet, and melancholic instrumental loop. Minimalist acoustic guitar strings plucked slowly over a soft, warm, dusty background pad. Lonely, quiet, serene.`

### ✨ Level 1 - Wonderous (Glowing/Magical)
* **Style Tags (Genre):** `ambient synthwave, warm sub-bass, glowing celestial bells, sparkling synth arpeggio, 85 bpm, key of a minor, loopable, magical, wonder`
* **Prompt Description:** `A wondrous and magical instrumental loop. Slow glowing synth arpeggios, warm sub-bass, and crystal bells playing a slow hopeful melody. Ethereal, bioluminescent, and dreamy.`

---

## 🎹 Level 2: "The Updraft Odyssey"
* **Musical Key:** E minor / G major (bright, airy, open)
* **Tempo:** 100 BPM (flowing, dynamic)

### 🌲 Level 2 - Mundane (Muted/Drifting)
* **Style Tags (Genre):** `airy ambient, soft wind chimes, distant muted piano, minimalist drone, 100 bpm, key of e minor, loopable, serene, quiet`
* **Prompt Description:** `A quiet, dusty, atmospheric loop. Muted acoustic piano notes drifting slowly over a quiet wind drone. Ethereal, lonely, peaceful, organic.`

### ✨ Level 2 - Wonderous (Weightless/Wind)
* **Style Tags (Genre):** `ethereal dream-pop ambient, glowing flute synth, bubbling synth arpeggios, warm sub-bass, 100 bpm, key of e minor, loopable, weightless, wind`
* **Prompt Description:** `A glowing, weightless instrumental loop. Airy flute synths and bubbling arpeggios rising upward. Warm sub-bass and dream-like celestial pads. Energetic yet floaty.`

---

## 🎹 Level 3: "The Gravity Cascade"
* **Musical Key:** D minor (epic, tense, cascading)
* **Tempo:** 90 BPM (steadily driving)

### 🌲 Level 3 - Mundane (Tense/Hollow)
* **Style Tags (Genre):** `moody cinematic ambient, slow hollow cello, quiet electric keyboard, ticking clockwork, 90 bpm, key of d minor, loopable, tense, industrial`
* **Prompt Description:** `A slow, tense, and mysterious instrumental loop. A hollow cello melody playing over a muted electric piano with a very quiet mechanical ticking sound. Melancholic and desolate.`

### ✨ Level 3 - Wonderous (Cosmic/Epic)
* **Style Tags (Genre):** `chillstep synthwave, cascading neon synth arpeggio, warm sub-bass, sparkling chimes, 90 bpm, key of d minor, loopable, epic, cosmic, gravity`
* **Prompt Description:** `A cosmic and majestic chillstep arpeggio cascading down. Deep warm sub-bass drops, sparkling stellar bells, and a light slow electronic beat. Weightless, gravity-shifting, climactic.`

---

## 💡 AI Music Generation Tips
1. **Looping Optimization:** Always include `loopable` in the genre tags. AI-generated endings usually add silences or custom outros, so you will need to trim the final `.mp3` files in a free tool like Audacity (aligning trims to the beat grid) to get seamless looping.
2. **Cohesiveness:** Keeping the musical key and BPM identical allows the game's `DynamicMusicPlayer` to cleanly sync and crossfade between the Mundane and Wonderous worlds in real-time.
3. **The "Extend" Trick:** If you get a Mundane track you love, use the "Extend" feature in Suno starting from the middle, change the style tags to the Wonderous style, and generate the rest. This naturally aligns the rhythm and key.
