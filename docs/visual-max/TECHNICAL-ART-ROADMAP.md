# Technical art roadmap

Status: READY_WITH_ASSUMPTIONS. Preserve MonoGame/.NET and the existing object ownership. [Audit](REPOSITORY-AUDIT.md) distinguishes current systems from proposed work; [backlog](IMPLEMENTATION-BACKLOG.md) defines slice boundaries.

## Upgrade order and architecture

First make capture and coordinates reliable. Then fix overlap/animation/cues, improve authored values and control particle density. Only then evaluate extra GPU passes. Keep GameWorld as the coordinator, entities as gameplay owners, ArtAssets as content/playback owner, and SoulfireRenderer as GPU-resource owner. Small helpers and presentation records are appropriate; a render graph framework or new scene architecture is unnecessary for this arena.

The proposed final pass order is explicit:

```text
Physical target [PointClamp]
  floor/base → ground decals/shadows → actors and props sorted by foot Y
  → attached weapons/local physical accents
Optional world-only distortion + scene grade
Emission [glows; optional half-resolution emission/blur]
Critical information [threat outlines, projectile heads, Core/Soul markers]
Soul Sense memories [low-priority; masked/suppressed near threats]
Vignette + bounded screen feedback
HUD/cinematic text [screen coordinates, crisp, excluded from postprocessing]
```

This is a logical ordering contract, not a requirement to allocate a target per line. Keep baseline alpha/additive paths when an effect is disabled. Soft trails may sit behind their critical projectile head. A Soul Sense memory is lower priority than a Core marker even if both are drawn through the same class.

## Coordinate and playback contract — VM-05–07

Compute one presentation transform per frame from camera, zoom, viewport and effective shake/kick. World, light, Soul overlays and debug bounds use that same transform. Simulation remains in world units. Define a separate unshaken logical transform for input: mouse position maps into the combat viewport, excluding letterbox/pillarbox bars; presentation recoil never changes world aim. Draw the reticle from the resulting world target using the presentation transform. Validate this policy under recoil instead of assuming the hardware cursor and shaken reticle must coincide.

For pixel stability, first compare current floating camera with render-only camera-translation snapping at baseline zoom. Do not round simulation positions. If rounding worsens slow motion or attachment jitter, retain the existing hybrid with a documented comparison. Fit 16:9 content into 16:10 rather than stretching actors. Keep a single shared screen/world conversion helper so capture resolution changes do not fork input math.

Use stable foot anchors for actor/prop sorting, with a deterministic tie-breaker. Draw attached body/Core/weapons together in intended front/back sublayers. Ground telegraphs and optional shadows are separate; critical outlines can be redrawn above bodies. Do not sort the whole HUD/particle system by Y.

Expose read-only presentation phase/progress from existing state machines. Map telegraph/active/recovery progress onto clip ranges; choose contact cells by visual inspection. Changing direction picks the corresponding sheet without restarting the phase. Hitstop freezes contact-dependent animation intentionally; ambient/cinematic clocks may continue. Document clocks and test them through VM-03. Do not move damage application into sprite frame callbacks.

## Low-cost atmosphere and material gains — VM-10–11

Use one small contact-shadow texture or existing shape helper for feet, low-opacity floor wear, and curated perimeter light pools. Any replacement background is a versioned derivative with before/after evidence. Preserve source references and original content as rollback. Avoid doubled baked and dynamic shadows.

Add a compact visual-settings object with baseline/high/reduced-effect choices at presentation boundaries. Keep gameplay constants in GameBalance. Route each existing VFX trigger through a family configuration (tint, scale, local light budget, effect priority) without introducing a general event bus. Critical warnings are separate from optional particles.

Provisional hard caps for the first budgeted implementation: 512 decorative combat particles, 64 sprite one-shots, 24 contributing dynamic glow sources, and the existing 40 ambient slots. Prioritize Player/action contact, then nearby enemy action, then ambient contributions. When saturated, discard/fade low-priority decoration; never drop damage warnings or projectile heads. Count dropped effects. Pool only where profiling shows allocation pressure; a bounded reused list can be enough. Reject looping clips in the one-shot Spawn path or require explicit owner cancellation.

## Selective emission bloom — VM-12, optional

Current radial glows already provide a workable fallback. The proposed experiment separates emissive masks from dark physical art, renders emission at half width/height, applies horizontal/vertical blur, and composites a restrained result before critical overlays/HUD. Keep sharp source cores at full resolution. Do not threshold the entire scene: white masks/UI and metal would bloom unintentionally.

MonoGame documents render-target light composition using its existing graphics APIs; that supports feasibility within this stack, not a requirement to adopt its full sample renderer. [Official 2D light tutorial](https://docs.monogame.net/articles/tutorials/advanced/2d_shaders/08_light_effect/index.html).

First prove that a tiny `.fx` effect compiles through this checkout's MGCB and runs on DesktopGL/Reach. Record SDK, MGCB, shader compiler prerequisites, graphics profile and GPU. Do not silently require HiDef, Windows-only tooling or a package upgrade. If the supported build path is unavailable, complete a diagnostic/design record and keep additive glows; shader-independent work continues.

Quality gate: same busy scene, Core/telegraph pixel crops, baseline/high/reduced views, and measured work-time delta. Accept only if the art rubric improves without clipping, threat loss, alpha fringes or a >2 ms p95 work-time increase on the reference machine. The visual target does not require bloom if it fails this test.

## Shadows, normals and distortion — VM-13, optional experiment

Try **one** static perimeter prop first. Prefer a hand-authored contact shadow/occlusion mask. If it looks insufficient, compare a simple silhouette shadow; the engine supports shader-based 2D shadows, but that technique has an additional authoring and performance cost. [Official shadow tutorial](https://docs.monogame.net/articles/tutorials/advanced/2d_shaders/09_shadows_effect/index.html).

Normal maps for the entire animated cast are deferred: 96 directional sheets multiply consistency/lighting work. A single static prop normal map is a bounded comparison, not permission to build a deferred renderer. Local heat/space distortion is an alternative experiment, masked around a cannon impact or resonance perimeter, with no displacement of HUD or critical geometry. Persistent chromatic aberration, global blur, volumetric lighting and screen-space reflections have low priority for this art style. Choose one experiment per session; record adopt/reject and its cost.

## Provisional performance envelope

These are design budgets, not measured results. VM-01 records the reference machine; VM-04 adds reproducible counters. Use identical Release build, resolution, seed and quality preset when comparing.

| Metric | Initial target | Method/limitation |
|---|---|---|
| Frame work time | p95 ≤16.67 ms; p99 ≤25 ms; sustained mean near/below 16.67 ms | Record Update/Draw CPU work separately from presentation/vsync; CPU submission timing is not GPU timing |
| Visual regression cost | ≤10% sustained work-time regression; optional bloom ≤2 ms p95 added work | Compare 30 s warmup + 120 s fixed workload; retain raw samples |
| Effects stress | 2× current wave-four actor composition for 120 s | Test-only fixture; use source wave composition, no gameplay balance edits |
| Runtime allocations | No continuous growth after warmup; no sustained Gen2 churn caused by effects | Record GC counts, allocated bytes and active counts; investigate trends rather than a guessed zero-allocation claim |
| Extra GPU targets | ≤32 MiB at 1080p for initial experiments | RGBA8 formula `width*height*4`; include every retained target, not disk PNG size |
| Restart/resource lifecycle | Stable resource/effect counts over 20 resets | Dispose/recreate render targets on resize; no stale VFX or owner loops after restart |

A 1920×1080 RGBA8 scene target is about 7.91 MiB; three 960×540 emission/blur targets add about 5.93 MiB. Additional full-resolution masks/ping-pong targets must be counted before adoption. If GPU timing is unavailable, label it UNMEASURED, include wall-clock frame pacing and GPU identity, and avoid claiming GPU headroom from CPU timestamps.

Low quality uses current crisp scene plus cheap glows/contact shadows. High may add selective bloom/one approved material effect. Reduced effects is an independent comfort preference that limits shake/flash/density at either quality. No gameplay information or timing may depend on a shader or quality level.

## Verification and rollout

All runtime slices build through the normal solution/content pipeline. Add small focused tests when they protect timing, coordinates, sorting or lifecycle; no test framework is needed merely to store this plan. Capture feature off/on in the same fixture. Keep optional features disabled until evidence satisfies the gate, then update their default and record a baseline version. Do not silently replace golden images to hide failures. Finish each slice with a local rollback path and an explicit next dependency.
