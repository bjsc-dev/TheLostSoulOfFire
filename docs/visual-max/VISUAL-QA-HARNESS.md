# Screenshot and visual QA harness

Status: PARTIALLY IMPLEMENTED. The current twelve-scenario runner, exact-tick sequences, state sidecars, isolated threat fixtures and missing-world/repeat checks are documented in [Astra final handoff](ASTRA-FINAL-HANDOFF.md). Run `bash tools/visual-max/capture-astra-review.sh`. The broader sixteen-scenario catalog, resolution/seed CLI, full performance metrics and cross-driver golden suite below remain design targets; do not treat them as implemented flags.

## Goal and architecture

Give future sessions a repeatable answer to “did this improve the game?” using the actual MonoGame renderer. Build a small debug runner alongside `Debugging/ScreenshotCapture.cs`, configured by Program and hosted in Game1/GameWorld. Reuse `InputState` injection and the pattern of the existing audio tests. No browser recreation of gameplay, secondary renderer, external game engine or full replay architecture is needed.

Proposed command after VM-03:

```bash
dotnet run --project src/TheLostSoulOfFire --no-build -- \
  --visual-scenario soul-release --seed 947 --resolution 1280x720 \
  --visual-quality baseline --capture-output artifacts/visual-max/local-run
```

VM-02 initially supports a smaller `--capture-after-ticks N --capture-output PATH --exit-after-capture` path for title/normal input runs. Parse invalid arguments with a helpful nonzero exit. Resolve output relative to the explicit root; recognize `.git` as file or directory. Preserve F9 and the audio flags. Do not add a second keybinding that conflicts with them.

## Determinism contract

Use a 60 Hz integer tick. Derive each test subsystem's random seed from a stable root seed and explicit stream IDs; never `string.GetHashCode()`. Existing seeds (particles 1987, screen 947, atmosphere 4129) are useful baseline evidence but do not solve reset determinism. Fresh fixture construction must recreate/reseed those systems, reset all timers, art playback owners, audio callbacks and camera state. Record the seed derivation version.

Scenario setup is bounded test-only state: arrange actual entities, use current gameplay state transitions, inject input/aim and capture when a named semantic state is reached. It must never change production damage, radii or durations. Prefer explicit debug setup functions over reflection or arbitrary JSON mutation of private fields. If a desired state is unreachable, fail the scenario with observed state/tick instead of saving the wrong picture.

Distinguish simulation tick, presentation tick and draw number. `ArtAssets` currently starts clips during Draw; variable draw cadence therefore changes results. The runner must draw once per test tick or move playback initialization into deterministic presentation update. Hitstop freezes simulation where the current game does; presentation/ambient time may continue according to the recorded policy. Input is captured in logical world coordinates and mapped through the same viewport policy as play.

Capture at the end of Draw before buffer presentation, after all requested layers. Event strips sample at least pre-event, warning start/mid/end, contact tick, contact+1/+3/+6, and recovery. Recreate the fixture for each independent variant so a previous capture cannot advance RNG or animation for the next one.

## Required scenario catalog

These IDs are the durable contract for later implementation. Capture ticks are resolved from semantic events and saved in results; a scenario must have a maximum tick budget and fail if the event never occurs. Default positions are the arena center and safe offsets inside CombatBounds; VM-03 stores exact positions in its fixture definitions.

| ID | Setup/action | Required evidence / invariants |
|---|---|---|
| `title-arrival` | Fresh world; title still then injected start | Title, intro and first controllable frame; no OS chrome, no missing text/content |
| `arena-idle` | Player centered, no active threats; fixed camera | Normal, Soul Sense and reduced-effect views of same scene |
| `directions` | Player and each enemy turn through 8 facings while idle/moving | Contact sheets with ground/Core/socket markers; phase continuity |
| `actor-overlap` | Player traverses front/back of Devourer and a tall test prop | Stable foot sorting and visible Player identification; no collision changes |
| `hollow-swipe` | One Hollow in attack range; hold scripted Player aim | Full 0.42 s telegraph, active sweep and recovery; contact frame matches state |
| `burning-charge` | Burning at valid charge distance; movement input fixed | Lane and direction during 0.62 s warning; charge and detonation captured separately |
| `devourer-slam` | Player in slam range; one exposed Soul available in a separate segment | Slam warning/contact/recovery and devour/imprisonment strip |
| `scythe-combo` | Three strikes against arranged targets | Three arcs/contact frames, Core and body hit variants, trails within effective range |
| `dash` | Dash across reference floor markers | Source, middle, destination/recovery; afterimages cannot obscure live Player |
| `cannon-sense` | Weak and full charge; Soul Sense on; target Core in aim | Charge stages, fire/recoil/impact; world/glow/Core registration under kick |
| `soul-release` | One normal kill plus separate Devourer extraction case | Exposed → Releasing → Residue → Released; no intact Soul entering Player |
| `resonance-busy` | Fill/activate resonance with wave-four actors and simultaneous effects | Warning visibility at activation and sustain; clipping/dropped-effect statistics |
| `lore-traces` | Same three motif anchors before/with Sense/after completion | Triptychs; trace suppression during nearby telegraph; no canon mismatch |
| `death-retry` | Fatal damage then R | Violet remnant and restart; no stale VFX/trace/loop state |
| `ending` | Complete four waves through deterministic test setup | Stillness, Life Flame at reveal and settled ending; orange rarity |
| `stress` | 2× actual wave-four composition, repeated effects and 20 resets | 120 s metrics after warmup; bounded counts/memory, no lost critical cues |

VM-03 implements the first baseline set (`title-arrival`, `arena-idle`, `hollow-swipe`, `cannon-sense`, `soul-release`, `resonance-busy`, `death-retry`, `ending`). The owning later slice adds its remaining scenarios. VM-19 runs all sixteen; the planning catalog is not a claim that those fixtures are already executable.

## Capture matrix and outputs

Every changed visual feature: 1280×720 baseline and reduced effects, same seed/camera/tick. Coordinate/HUD changes additionally require 1920×1080 and 1280×800 aspect-fit captures. Optional shaders require feature off/on. Critical combat changes require normal, Soul Sense, resonance and combined state; automate these variants rather than relying on a tester's key timing.

Each run writes `run.json`, full PNGs, optional layer PNGs, event strips/contact sheets, `metrics.csv` and `review.md`. Fields: schema version, scenario/version, root and stream seeds, setup/inputs, simulation/presentation ticks, capture event, commit and dirty status, source asset hashes, settings, resolution/viewport/camera, build configuration, SDK/MonoGame version, OS/GPU, warnings, exit code and comparison baseline ID. Never record tokens or credentials. The runner must report missing required assets rather than silently draw an unrelated placeholder.

Layer inspection should initially expose physical scene, glow, critical/Sense overlay and final frame. Avoid `GetBackBufferData` every gameplay frame: GPU readback is for requested captures only and excluded from frame-work performance samples. Unsupported GPU hosts exit with a named capability failure, not a green placeholder PNG. CI can run structural/timing checks separately; real DesktopGL visual checks require a known graphics runner.

## Comparison policy

Compare matching scenario/settings/hardware families. Same host and deterministic content should produce stable image hashes or a narrowly documented tolerance; cross-driver color differences must not force false exact-pixel promises. Repeated-run instability is a harness defect to explain before baselines are trusted.

Start with absolute RGB difference and changed-pixel heatmaps, then inspect regions of interest. Provisional same-host alert: more than 0.5% of final pixels differ by >8/255 in any RGB channel. This is an investigation threshold, not an automatic rejection of intentional art changes. Keep unmasked final frames; masks may exclude measured nondeterministic diagnostic text, never threats or the Player. Detect blank/near-blank outputs separately.

Measure critical ROI edge separation, fully clipped bright-pixel coverage, actor/anchor displacement and warning visibility over time. A useful initial cue heuristic is a ≥3:1 local luminance contrast between warning edge and adjacent floor sample for most of the edge; it is not a formal game-accessibility standard. Shape/temporal review remains required and exceptions need actual crops. Shader and camera alignment should stay within one output pixel for matching world anchors at baseline resolution.

Apply the art bible's rubric to native-size images, a 640×360 reduction, grayscale and a short sequence. Color-vision simulations may reveal problems but never replace the shared shape grammar. Human/player comprehension testing is valuable later; automated/agent review must be labeled as such.

## Baselines, failures and future sessions

Store small approved baseline sets with a JSON manifest under a versioned evidence location; keep bulk output in ignored artifacts. VM-02 records the first baseline before polish. New baselines require a before/after explanation, passing invariant checks, and an explicit review decision in the slice handoff; do not overwrite previous evidence. The archived Ludo screenshot is reference-only.

Failures report scenario, event/tick, expected/actual state, capture path and likely subsystem. Fail the affected slice on a missing critical cue, mismatched action timing, leaked target or changed gameplay result. For unavailable graphics/tooling, record `NOT_RUN` and continue independent checks. `READY_WITH_ASSUMPTIONS` describes plan readiness; it never converts a runtime failure into an acceptance pass.
