# Implementation backlog

Status: **IMPLEMENTATION_IN_PROGRESS**. VM-01 and VM-02 are DONE; remaining slices are TODO. Follow [HANDOFF.md](HANDOFF.md) for evidence and the next ready slice. File paths are relative to the repository root; runtime shorthand `Rendering/`, `Game/`, etc. means beneath `src/TheLostSoulOfFire/`.

## Execution rules

Take the highest-priority TODO whose dependencies are DONE. P0 establishes evidence; P1 delivers the core visual target; P2 contains optional ceiling/production work; P3 packages the showcase. Within a priority, prefer numeric order. A coding session should complete one bounded slice or a tightly coupled pair, capture evidence, update status and hand off. Estimates are focused sessions (roughly 2–4 hours each), not guarantees; split a slice if its evidence work exceeds that size.

Every runtime slice must build the solution/content, preserve current gameplay damage/range/timers, pass relevant existing smoke tests, satisfy its visual criteria on a graphics host and record results using [the handoff template](templates/session-handoff.md). Focused tests are appropriate for new timing/coordinate/lifecycle contracts; do not create tests just to mirror tuning constants. Documentation/authoring-only slices use structural and review evidence instead of unnecessary runtime tests.

For each numbered acceptance item record PASS, FAIL or NOT_RUN. DONE requires all applicable items passed and supporting evidence. If graphics or a provider is unavailable, finish independent work and record `IN_PROGRESS / BLOCKED_EXTERNAL` for the dependent item; a plan can remain READY_WITH_ASSUMPTIONS. Optional shader experiments can finish with a documented REJECT decision and the working fallback; later work must not depend on their adoption.

## Priority/dependency index

| ID | Priority | Slice | Depends on | Estimate | Status |
|---|---|---|---|---|---|
| VM-01 | P0 | Asset/build inventory and reference machine | — | 1 | DONE — inventory, locked hashes, Release build and reference-host record added |
| VM-02 | P0 | Worktree-safe capture CLI and baseline | VM-01 | 1 | DONE — worktree-safe CLI, sidecars, title/arena baselines and gameplay smoke verified |
| VM-03 | P0 | Deterministic visual fixtures | VM-02 | 2–3 | TODO |
| VM-04 | P0 | Comparison reports and frame metrics | VM-03 | 1–2 | TODO |
| VM-05 | P1 | Shared coordinates and pixel/aspect policy | VM-04 | 1–2 | TODO |
| VM-06 | P1 | Actor anchors and depth ordering | VM-05 | 1–2 | TODO |
| VM-07 | P1 | Gameplay-aligned animation phases | VM-03, VM-06 | 2 | TODO |
| VM-08 | P1 | Shape-coded enemy warnings | VM-07 | 1–2 | TODO |
| VM-09 | P1 | Feedback hierarchy and reduced effects | VM-08 | 1 | TODO |
| VM-10 | P1 | Arena value/material polish | VM-06, VM-09 | 1–2 | TODO |
| VM-11 | P1 | VFX family settings and capacity budgets | VM-09, VM-10 | 1–2 | TODO |
| VM-12 | P2 | Selective bloom experiment | VM-04, VM-11 | 1–2 | TODO |
| VM-13 | P2 | One material/shadow/distortion experiment | VM-10, VM-12 | 1 | TODO |
| VM-14 | P1 | Three environmental Soul traces | VM-08, VM-10 | 1–2 | TODO |
| VM-15 | P1 | Release, imprisonment and ending clarity | VM-11, VM-14 | 1–2 | TODO |
| VM-16 | P1 | HUD/title/transition presentation | VM-05, VM-09, VM-15 | 1–2 | TODO |
| VM-17 | P2 | Asset promotion tooling and manifests | VM-01, VM-04 | 1–2 | TODO |
| VM-18 | P1 | Audio/visual contact and relief pass | VM-07, VM-11, VM-15 | 1 | TODO |
| VM-19 | P3 | Full visual gate and showcase handoff | VM-12–13 decisions; VM-16–18 | 1–2 | TODO |

Default critical path starts **01 → 02 → 03 → 04 → 05 → 06 → 07 → 08 → 09 → 10 → 11**, then completes 14–16/18 before optional 12–13 and production 17. VM-17 can be pulled forward if new asset volume justifies it. Existing art and local derivatives are valid for every P1 slice; no P1 slice depends on purchasing a tool or generating a new paid asset.

## VM-01 — Asset/build inventory and reference machine

Outcome: a portable inventory of what actually loads, what it costs, and how to validate it. Touch `tools/visual-max/` (new), `docs/visual-max/evidence/` (new) and, only where needed, existing audit integration. Read ArtAssets, Content.mgcb and source manifests. Do not replace content or rewrite the old delivery ledger.

Acceptance:

1. Inventory matches 116 runtime PNGs, 96 directional sheets, 12 VFX sheets and 29 audio assets at this baseline; distinguish files shipped from textures actually loaded. Record real dimensions, frame metadata, hashes and estimated uncompressed texture bytes.
2. Inventory detects a missing path, invalid grid and duplicate runtime key using isolated temporary fixtures; errors identify asset/key/path. No production asset is altered by validation.
3. Three locked-source hashes remain valid; ledger references are repo-relative and no new absolute machine paths are serialized.
4. Record Release build command, resolved SDK/MonoGame versions and graphics-host identity. If reference GPU access is missing, record it and proceed with inventory; performance remains unmeasured.

Evidence: inventory JSON/table, validator output, build result and reference-host record. Next: VM-02.

## VM-02 — Worktree-safe capture CLI and current baseline

Outcome: save an actual game frame on command and exit reliably. Touch `Program.cs`, `Game1.cs`, `Debugging/ScreenshotCapture.cs` and a small option/helper class if needed.

Acceptance:

1. `.git` directory and `.git` file roots resolve correctly from repo root and nested working directories; explicit output directory takes precedence. Paths with spaces work.
2. The proposed capture-after-ticks/output/exit options in the QA design work; invalid input or write failure produces a nonzero exit and useful diagnostic. F9 and existing audio flags still work.
3. PNG dimensions equal the actual render dimensions; file contains the complete post-HUD game image without OS chrome. Capture tick/context and build identity accompany it.
4. Fresh title and first-arena captures establish a dated baseline. Keep the archived Ludo capture as reference-only. Run the existing automated playthrough after option handling changes.

Evidence: two PNGs/metadata and path/argument checks. Rollback: new CLI path optional; ordinary game startup remains available.

## VM-03 — Deterministic visual fixtures

Outcome: reproduce semantic states without manually timing keys. Touch `Debugging/VisualScenarioRunner.cs` (new), Program/Game1, narrow test setup hooks in GameWorld/InputState and reset/clock APIs where required.

Acceptance:

1. Implement the eight baseline scenarios named in the QA document; each reaches its requested event before a finite tick limit and exits with an explicit result.
2. Repeated fresh runs with equal seed/inputs produce equal gameplay state and stable captures on the same host; any image tolerance is documented with cause. Different seeds change optional atmosphere without changing scripted combat outcomes.
3. Capture records distinguish simulation/presentation ticks and render cadence; hitstop, random streams and ArtAssets first-draw playback no longer make the fixture depend on wall time or a previous run.
4. Reset returns camera, playback, particles, Soul Sense and scenario input to known state; ordinary interactive game and audio playthrough/restart still work.

Evidence: per-scenario manifests, two-run comparison, expected state assertions. Do not expose arbitrary private-state editing to production content.

## VM-04 — Comparison reports and frame metrics

Outcome: paired images and performance evidence that future sessions can interpret. Touch `tools/visual-max/`, minimal debug metrics instrumentation and evidence docs.

Acceptance:

1. Report contains before/after/heatmap, native crops, grayscale/reduced-size views and event strips for matching scenario/tick/settings; incompatible baselines produce a diagnostic.
2. A deliberately blank capture, a displaced Core marker and an intentionally changed image are each detected in temporary fixtures. Tolerances and allowed masks are explicit; critical ROIs cannot be masked away.
3. Record frame work times, allocations/GC, active/dropped effect counts as available, settings and host; distinguish CPU work from vsync/GPU duration and exclude capture readback from performance samples.
4. Baseline adoption is a recorded review action with a new version; comparison never silently rewrites old evidence. Missing GPU results are NOT_RUN.

Evidence: self-contained report and raw metrics from at least idle and busy fixtures. Next: VM-05.

## VM-05 — Shared coordinates and pixel/aspect policy

Outcome: world, light and Soul Sense stay attached under recoil, zoom and alternate aspect ratios. Touch Camera2D, GameWorld.Draw/DrawSoulfireLighting, SoulSensePresentation, viewport/input handling.

Acceptance:

1. World, glow and Core anchor projections differ by ≤1 output pixel in cannon+Soul Sense recoil captures at baseline resolution; all use one intended presentation transform.
2. Logical unshaken aiming and reticle policy in the roadmap are implemented and tested at center, corners, non-unit zoom and letterbox edges; recoil does not alter the world target.
3. 1280×720, 1920×1080 and 1280×800 fit correctly without stretched sprites or clipped HUD; bars do not inject unintended gameplay aim.
4. Slow pan/dash comparison records adopt/reject of render-only snapping; no simulation rounding, new collision jitter or changed movement distance is introduced.

Evidence: transform checks, three-resolution captures, slow-motion strip and gameplay smoke. Rollback: retain a documented baseline pixel mode.

## VM-06 — Actor anchors and depth ordering

Outcome: believable overlap and stable attachment points. Touch ArtAssets metadata/draws, GameWorld.DrawScene and necessary entity presentation draw boundaries.

Acceptance:

1. Add `directions` and `actor-overlap` fixtures; each cast member has reviewed foot/center/Core/weapon anchors as applicable across all eight facings.
2. Walking in front of/behind the Devourer and one tall test prop produces stable foot-Y ordering with deterministic ties; Player remains identifiable through the declared outline/fade rule.
3. Body, Core and attached weapon remain aligned through turns and movement; no recentering based on changing per-frame alpha bounds. No duplicate body/weapon rendering.
4. Shadows/ground cues and critical overlays keep correct layers; collision bounds and enemy behavior are unchanged.

Evidence: contact sheet with anchor markers and overlap sequence. Avoid a new scene graph.

## VM-07 — Gameplay-aligned animation phases

Outcome: poses tell the truth about attack timing. Touch ArtAssets/SpritePlayback and read-only phase access in Hollow/Burning/Devourer and weapon presentation where needed.

Acceptance:

1. Hollow and Devourer anticipation/contact/recovery cells are chosen from inspected sheets and mapped to existing timers; contact pose occurs within one tick of the active event. Burning charge stays visibly directional for its actual movement interval.
2. Turning during a move/attack switches direction without restarting action progress; one-shots restart on a new action, not on every facing change.
3. Add burning-charge, devourer-slam, scythe-combo and dash fixtures; event strips cover all directions where asymmetric animation changes contact placement.
4. Hitstop policy is explicit and reproducible; no damage/range/cooldown constants change. Focused phase/direction/reset tests and existing playthrough pass.

Evidence: contact-cell mapping table, event strips and state logs. Prefer retiming current clips before generating replacements.

## VM-08 — Shape-coded enemy warnings

Outcome: shared violet energy still communicates three distinct threats. Touch enemy telegraph drawing, GameWorld pass placement and ArtAssets if a small marker texture is justified.

Acceptance:

1. Hollow sweep wedge, Burning directional lane and Devourer grounded ring appear from the first tick of their existing telegraphs (0.42/0.62/0.88 s); geometry agrees with actual direction/range.
2. Warning edge stays recognizable through Soul Sense, resonance and combined busy scenes, including grayscale and muted playback; dark separators survive floor variants.
3. Active and recovery visuals cannot imply lingering damage after the gameplay window. Friendly trails are not confusable with hostile warning shapes in side-by-side review.
4. Critical warning visibility is unaffected by disabled bloom/particles or lower quality. Document any local-contrast heuristic exception with a crop.

Evidence: three enemy event strips × required visual states; no balancing edits.

## VM-09 — Feedback hierarchy and reduced effects

Outcome: powerful impacts with a usable quieter presentation. Touch ScreenEffects, CombatPresentation, small settings/UI plumbing and relevant VFX calls.

Acceptance:

1. Independent shake/kick and flash controls plus reduced-effects preset persist for the run and are accessible without editing code; no unnecessary settings framework.
2. Zero-shake/flash output contains no residual camera kick/full-screen flash, while contact poses, local sparks and warning information remain.
3. Scythe/body hit, Core hit, full cannon and resonance have distinct local peak/recovery envelopes; sustained resonance does not hold the whole screen near white.
4. Baseline/reduced busy and death/ending sequences satisfy the readability rubric; gameplay hitstop, damage and input timings are unchanged.

Evidence: settings matrix, before/after strips and metrics. No claim of formal photosensitivity certification.

## VM-10 — Arena value and material polish

Outcome: a grounded, readable fighting floor with a stronger place identity. Touch versioned art derivatives and limited ArenaAtmosphere/ArtAssets/environment draw logic.

Acceptance:

1. Deliver a before/after idle and busy capture with improved Player/enemy separation; art rubric reaches ≥8/10 with no zero and silhouette/phase both 2.
2. Initial decorative kit stays within the art bible's bounded scope; perimeter focal point, quiet center and stable perspective are visible. No new collision or blocked route.
3. Added floor/contact-shadow treatment avoids doubled baked shadows, halos or violet glow on every prop; critical warnings retain priority.
4. Locked-source hashes and production grid/alpha checks pass; derivatives have a brief, checksum, provenance and rollback path. Existing content suffices if generation is unavailable.

Evidence: matched arena normal/Sense/busy captures, small asset contact sheet and metrics.

## VM-11 — VFX family settings and capacity budgets

Outcome: coherent effects that degrade predictably under stress. Touch ParticleSystem, SpriteVfxSystem, SoulfireLighting, CombatPresentation and compact presentation settings.

Acceptance:

1. All twelve existing sprite VFX map to named families and owning events, with explicit blend/layer/scale/light choices; no accidental duplicate burst at one transition.
2. Enforce documented decorative particle, sprite-effect, glow and ambient caps; the stress fixture shows dropped counts, bounded memory/counts and priority for nearby important effects.
3. Looping clips cannot become immortal unowned one-shots; death/restart and twenty resets clear owned loops/instances correctly.
4. In saturation and reduced effects, hostile cues/projectile heads/Core and Soul states remain visible. p95/p99/allocations are reported against VM-04 baseline.

Evidence: VFX family contact sheet, `stress` fixture, lifecycle/cap tests and metrics. Do not replace all particle code solely to introduce pooling.

## VM-12 — Selective bloom experiment (optional)

Outcome: an evidence-based ADOPT or REJECT decision for an emission pass. Touch SoulfireRenderer and one MGCB `.fx` experiment, with feature flag and fallback.

Acceptance:

1. Prove minimal effect compilation and DesktopGL/Reach execution with recorded prerequisites, or document the exact unsupported toolchain failure and keep baseline working. No stack migration/profile upgrade hidden in the experiment.
2. If runnable, only emissive sources feed bloom; HUD, pale masks and ordinary metal do not bloom. Sharp Core/projectile heads remain crisp; no premultiplied-alpha fringes.
3. High/off/reduced captures show readable threat geometry; added target bytes and p95 cost meet the roadmap budget (≤2 ms p95 added work, ≤32 MiB extra targets at 1080p).
4. ADOPT requires rubric improvement plus passing capture/performance checks. Otherwise remove/disable the experimental default, retain baseline and record REJECT with evidence. Lifecycle/resize checks pass for any retained code.

Evidence: toolchain result, A/B report, metrics, decision. A documented rejection completes this optional decision slice.

## VM-13 — One material/shadow/distortion experiment (optional)

Outcome: test whether one extra technique improves the chosen scene. Touch only one static prop/material or localized effect plus its renderer integration; use VM-12's decision as context, not a requirement to adopt bloom.

Acceptance:

1. Choose one technique and one subject; compare baseline, cheap shadow/overlay treatment and the proposed effect in matched captures.
2. No global blur, animated-cast normal-map rollout, camera-space drift or displaced warning/HUD pixels. The prop's baked and dynamic lighting agree.
3. Metrics stay within the total roadmap budget and supported graphics profile; reduced/baseline modes work without the experiment.
4. Record ADOPT/REJECT and keep only justified production code/assets. A failed visual-benefit test is a valid REJECT result, not a reason for indefinite shader expansion.

Evidence: three-way comparison and measured decision.

## VM-14 — Three environmental Soul traces

Outcome: the arena suggests human history through normal/Sense/released views. Touch SoulSensePresentation and a small trace-definition/presentation owner; optional versioned prop/pose derivatives.

Acceptance:

1. Bell/chain, work station and gate motifs from the lore plan each have a distinct normal/Sense/completion state and are visible in at least one ordinary camera framing.
2. Add `lore-traces`; triptychs and short loops communicate the proposed unfinished-shift interpretation without asserting new locked canon or Player origin.
3. Decorative traces suppress near active threats, never appear collectible/attackable, and preserve weak-point visibility; only one optional trace is emphasized at a time.
4. Reset clears narrative presentation state; no damage, collision, wave or save system changes. Missing optional art has a simple silhouette fallback.

Evidence: three triptychs, busy/Sense check, lore rubric and reset result.

## VM-15 — Release, imprisonment and ending clarity

Outcome: the central theme is visible during real gameplay. Touch Soul drawing/release presentation, Devourer Soul overlays, CinematicPresentation and completion atmosphere.

Acceptance:

1. A normal kill and Devourer extraction each visibly separate the destroyed body from the Soul; intact Soul departure and small residue intake are distinct paths in the event strip.
2. Release/residue timing remains 1.25/0.85 s unless current source changed in an independently authorized gameplay task; this slice makes presentation fit gameplay.
3. Devourer inward imprisonment looks different from peaceful release; no Player animation implies consuming the intact Soul.
4. Completion calms atmosphere and reveals stable warm Life Flame at the existing beat; death remains violet. Ending/death/retry fixtures pass and all temporary state clears.

Evidence: release/devour comparison, before/after calm captures and ending strip.

## VM-16 — HUD, title and transitions

Outcome: the game reads as one finished visual product at supported sizes. Touch HudRenderer, PixelText and CinematicPresentation; small locally authored icon/text assets only if justified.

Acceptance:

1. Health, cannon charge/readiness, resonance and wave progress have consistent spacing/weight and remain legible at all three QA resolutions and in busy combat.
2. Title/intro/death/ending typography is crisp and excluded from scene effects; prompts are neither clipped nor obscured by the Life Flame or cinematic bars.
3. Tutorial/control hints appear when useful and do not cover threats; preserve existing bindings and bitmap glyph coverage for any new text.
4. No debug labels, generation/model/tool details or fake marketing claims appear in product UI. Capture-ready mode may hide HUD only through an explicit setting and leaves gameplay behavior unchanged.

Evidence: full title-to-ending UI strip and resolution matrix.

## VM-17 — Asset promotion tooling and manifests

Outcome: the next asset family can be authored and integrated with little coordination. Touch `tools/visual-max/`, versioned `art/visual-max/` metadata and template/example docs. Keep the historical Ludo audit useful for its original delivery.

Acceptance:

1. Implement schema/validator for the pipeline's required metadata, grid/alpha/anchors/runtime-key checks and source/derived hashes; error messages identify missing fields/files.
2. Demonstrate one existing-asset derivative through BRIEF_READY → validation → in-game review → INTEGRATED, using zero paid calls. Preserve all three locked hashes.
3. Runtime/source manifest parity and MGCB registration checks accept additional approved assets without breaking the historical 116-file delivery claim; legacy audit changes, if needed, are narrowly documented.
4. Produce a reproducible contact sheet and promotion record; a baked-checker/invalid-grid/reused-key candidate fails temporary-fixture validation. No credentials or expiring authenticated URLs are committed.
5. Briefs include budget/retry caps and unavailable-provider fallback; automation stops only the paid branch when no budget exists. Ordinary offline build/play requires no provider.

Evidence: sample manifest, validator outputs, review/promotion artifact and exact portable commands. Plugin packaging remains optional future work.

## VM-18 — Audio/visual contact and relief pass

Outcome: existing sound reinforces threat, contact and release timing. Touch AudioDirector/GameWorld cue wiring only as needed; optionally author a bounded replacement candidate with provenance.

Acceptance:

1. Log and review Hollow warning, Burning charge, Devourer slam, scythe/Core hit and full cannon audio trigger against the corresponding visual/gameplay event; software trigger offset ≤1 tick. Audible device latency is recorded separately.
2. Warning cues remain distinct under voice limits and busy effects; completion/release audio supports quieter relief without masking remaining threats.
3. Existing 29-asset validator and gameplay/death-restart/audio runtime checks pass for relevant changes; audio fallback and loop cleanup remain functional.
4. New sound, if any, has actual provider/source/license evidence and passes existing WAV/OGG level/format checks. Lack of paid generation does not block timing/mix work using current assets.

Evidence: event timeline, brief listening notes with playback context, validators and smoke results. No replacement of the current audio architecture.

## VM-19 — Full visual gate and showcase handoff

Outcome: a repeatable, reviewable Visual Max arena and an honest presentation bundle. Touch evidence, scripts/fixtures only for missing coverage, showcase outputs and final docs.

Acceptance:

1. All sixteen QA scenarios run; required normal/Sense/resonance/reduced/aspect variants pass their invariants. No unresolved critical readability, timing, canon or asset-integrity failure.
2. Measured baseline/high/reduced performance and twenty-reset lifecycle results satisfy budgets on the recorded reference machine, or high features are disabled until they do. Unknown GPU/platform coverage is listed explicitly.
3. Deliver six unedited gameplay stills and a 20–30 s real gameplay sequence covering place, combat, Sense and release/ending. Native-size and thumbnail review meets art rubric; no concept render presented as gameplay.
4. Clean solution/content build, asset/audio validators and relevant runtime tests pass; required/optional assets and provenance are complete. No paid service is needed at launch.
5. Handoff records final defaults, adopted/rejected experiments, remaining platform/player-study limitations, reproducible commands and a prioritized next scope. Publishing/marketing distribution is not performed by this slice.

Evidence: showcase index and files, full QA report, final inventory, performance samples and handoff. This is the master plan's delivery gate; future regions are a separate scope.
