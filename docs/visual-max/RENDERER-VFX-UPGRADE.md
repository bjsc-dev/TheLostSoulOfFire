# Renderer, VFX and screen-juice slice

Status: **IMPLEMENTED** · 2026-09-05

This slice keeps the existing MonoGame DesktopGL, .NET, `Game1`/`GameWorld`, `ArtAssets`, entity-state and content-pipeline architecture. It changes only presentation ownership and debug/capture seams. Damage, ranges, wave flow, combat timers and hitstop duration remain gameplay-owned values.

## What changed

The renderer now has three bounded presentation stages:

```text
Point-clamped physical scene target
  → scene grade / Soul Sense veil
  → full-resolution emission target
  → optional half-resolution soft emission halo
  → Soul Sense critical layer / vignette / screen feedback
  → crisp HUD and cinematic overlay
```

Emission is separate from the scene and never receives HUD or title pixels. High quality downscales and bilinearly enlarges only the emission target before compositing the sharp source target. This gives Core, Souls, cannon fire and major sprite effects a restrained soft halo without adding an `.fx` build dependency or changing the DesktopGL Reach profile. `baseline` retains the sharp emission target alone.

`PresentationSettings` is the run-local tuning surface. The default is `high` with full effects. **F10** toggles the reduced-effects preset and **F11** switches high/baseline quality. The preset sets camera shake/kick and full-screen flash to zero, halves non-critical combat particle density, suppresses decorative particles, reduces glow and vignette intensity, and disables the soft halo. It does not alter hitstop, damage windows, warnings, Core/Soul visibility or gameplay input timing.

`VisualEffectFamilies` records the blend mode, priority and emission envelope for all twelve existing sprite VFX sheets. Alpha effects stay in the physical scene; additive effects are drawn in a separate world pass. Sprite one-shots are capped at 64 and combat particles at 512. A critical event can evict older lower-priority decoration, but lower-priority events cannot evict critical effects. Looping clips are rejected from the one-shot system and must remain owned by their existing playback owner. The debug overlay reports active and dropped effect counts.

Screen effects retain gameplay hitstop and now route shake/kick, impact-frame darkening and colored flashes through the shared presentation settings. Combat contact uses violet for ordinary contacts and Soul white for core/full-cannon/resonance peaks. The Soul Sense layer now uses the same camera kick transform as scene and emission, keeping world anchors registered during cannon recoil.

## Reproducible visual scenarios

The executable accepts a fixed-tick, real-input scenario runner. It only uses injected normal input and the existing F2–F7 debug seams; it does not mutate private entity state or game balance. A scenario writes one named frame then exits. It is intended for a DesktopGL graphics host.

```bash
dotnet build TheLostSoulOfFire.sln -c Release --no-restore
dotnet run -c Release --no-build --project src/TheLostSoulOfFire -- \
  --visual-scenario cannon-sense \
  --visual-quality high \
  --capture-output artifacts/visual-review/renderer
```

| Scenario | Primary presentation evidence |
|---|---|
| `title-arrival`, `arena-idle` | scene grade, composition, HUD exclusion and vignette |
| `dash`, `scythe-combo` | motion accents, local contact and additive melee effects |
| `hollow-swipe`, `burning-charge`, `devourer-slam` | enemy threat and impact readability |
| `cannon-sense` | charge/fire emission and Soul Sense registration under recoil |
| `resonance-busy` | peak emission, effect caps and reduced-noise comparison |
| `soul-release`, `death-retry`, `ending` | release/death/relief paths and warm ending light |

Capture a whole representative set after a Release build with:

```bash
bash tools/visual-max/capture-renderer-review.sh
```

The script intentionally does not delete older artifacts; each scenario overwrites only its own named PNG. It also saves a baseline `cannon-sense` comparison and reduced-effects `resonance-busy` comparison in subdirectories. `--capture-after-ticks N --capture-output DIR --exit-after-capture` is also available for a raw timed still. `--visual-quality baseline` and `--reduced-effects` make a matched fallback/comfort capture. Invalid visual arguments exit with code 2 and an explanation. Screenshot root discovery recognizes both a `.git` directory and a worktree `.git` file.

## Extension points

- Add a new sprite sheet to `VisualEffectFamilies` before spawning it through `SpriteVfxSystem`; choose a blend, priority and local glow envelope there.
- Add a new cosmetic control to `PresentationSettings`, then apply it in the owning presentation system. Do not put it in `GameBalance`.
- A future true bloom shader can replace the high-quality soft-emission step inside `SoulfireRenderer`. It must keep the emission/HUD separation, feature flag and baseline fallback.
- New visual states belong in `VisualScenarioRunner` and the capture script with a fixed capture tick and named output.

## Limits of this slice

The soft halo is a shader-free bounded vertical slice, not a claim that selective multi-pass bloom or GPU timing has been validated. The runner captures the first rendered frame after its scheduled fixed update; driver scheduling can delay the draw after the scenario tick. Its output is deterministic input evidence for one graphics host, while golden-image comparison and exact one-draw-per-tick control remain VM-03/VM-04 work.
