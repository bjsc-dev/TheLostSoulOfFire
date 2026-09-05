# Repository audit

Inspected 2026-09-05 at `f510e4dcdefb837957c577cd197a8c22ca2f1116`. Paths below are relative to the repository root. These are code observations unless explicitly marked as visual evidence or a hypothesis.

## Systems already present

| Area | Evidence | Current behavior and extension point |
|---|---|---|
| Stack/lifecycle | `src/TheLostSoulOfFire/TheLostSoulOfFire.csproj`, `Game1.cs`, `Program.cs` | `net9.0`, MonoGame DesktopGL/Content Builder `3.8.*`, `RollForward=Major`; fixed 60 Hz update, 1280×720 window; Game1 owns Content, ArtAssets, GameWorld and SoulfireRenderer |
| World/render orchestration | `Game/GameWorld.cs`: `Update`, `Draw`, `DrawScene` | Existing title/intro/combat/wave-transition/completion loop; four waves; concrete entity lists; presentation, audio and VFX updated alongside gameplay |
| Scene rendering | `Rendering/SoulfireRenderer.cs` | One viewport-sized `RenderTarget2D`, PointClamp scene draw, color tint, shadow veil, Soul Sense suppression; generated radial glow and vignette textures; target resized/disposed as needed |
| Light emission | `Effects/SoulfireLighting.cs` | Additive source glows for player, Souls, enemies, cannon, atmosphere and ending; LinearClamp for generated light only |
| Assets/animation | `Rendering/ArtAssets.cs` | ContentManager loads five static textures plus 96 directional clips and 12 VFX clips; metadata in C#; 8-way angle selection; per-owner `SpritePlayback` keyed by family/action/direction |
| Content build | `Content/Content.mgcb` | DesktopGL Reach profile; textures use premultiplied alpha, no mipmaps, no color key or power-of-two resize; WAV/OGG content also built |
| Source art | `art/ludo_delivery/generation_manifest.json`, `GENERATION_MANIFEST.md`, `tools/` | Delivery contains source candidates, direction masters, normalized sheets and six review boards; audit protects three locked hashes and expects exactly 116 runtime PNGs |
| Camera | `Rendering/Camera2D.cs`, `Rendering/CinematicPresentation.cs` | Floating-point follow/zoom, world clamping, inverse mouse transform; cinematic title/intro/retry/wave/death/ending framing |
| Combat feedback | `Combat/CombatPresentation.cs`, `Effects/ScreenEffects.cs`, entity and weapon draw methods | Scythe/cannon/dash feedback, local trails/afterimages, camera kick/shake, hitstop, flash/impact frames; no custom `.fx` postprocess found |
| VFX lifetime | `Effects/SpriteVfxSystem.cs`, `Effects/ParticleSystem.cs` | Sprite one-shots expire; procedural free/converging particles; both use growable lists rather than explicit global budgets |
| Atmosphere | `Effects/ArenaAtmosphere.cs` | Seeded ambient system, capacity 40, separate ash/smoke/ember limits, haze, furnace pulses, faults, reaction to force/resonance and calm completion |
| Soul Sense | `Rendering/SoulSensePresentation.cs` | Staged world suppression then Soul emergence, hardcoded trace paths, Hollow cores, Burning fractures, trapped Devourer Souls and Player response |
| Lore state | `Entities/Soul.cs`, `Entities/Devourer.cs`, `Game/GameWorld.cs` | Exposed/BeingDevoured/Releasing/Residue/Released/Consumed; destroyed manifestation leaves a Soul; residue travels to Player; Devourer can imprison Souls |
| HUD/text | `Rendering/HudRenderer.cs`, `Rendering/PixelText.cs` | Procedural meters/icons and bitmap glyphs; title/transition/death/completion text in CinematicPresentation; no separate narrative database/localization system found |
| Audio | `Audio/AudioDirector.cs`, `Content/Audio/SOURCES.md` | Authored SoundEffect/Song playback, loops, mix changes and voice limits; generated-tone fallback; committed ledger attributes current 29 assets to Ludo, not ElevenLabs |
| Capture/debug | `Debugging/ScreenshotCapture.cs`, `Game1.cs`, `Input/InputState.cs` | F9 writes backbuffer PNGs to `artifacts/screenshots`; context names selected from gameplay; F1 debug, F2–4 spawn enemies, F5 resonance, F6 kill, F7 Soul Sense, F8 reset |
| Automated checks | `Debugging/AudioRuntimeTestGame.cs`, Game1 automated modes, `tools/audio/validate_audio.py`, `art/ludo_delivery/tools/audit_delivery.py` | Audio/playthrough/restart test flags and asset validators exist; no test project in solution; no automated visual scenario runner or golden-image comparator found |

Current render order is:

```text
Scene target: arena → background atmosphere/gates → afterimages → enemies
              → Souls → projectiles → particles → Player/weapons → world accents/VFX/aim
Backbuffer: graded scene → additive glows → Soul Sense layer → vignette
            → screen feedback → HUD/cinematic overlay
```

Enemy iteration order and the always-later Player draw currently determine overlap. This is not a Y-sorted actor pass. `Game/Arena.cs` retains procedural arena drawing helpers, but current `DrawScene` calls `ArtAssets.DrawArena` for the textured base; do not assume editing the procedural arena artwork changes the shipped floor.

## Concrete improvement opportunities

1. `ScreenshotCapture.FindRepositoryRoot` checks `Directory.Exists(.git)`. This checkout has a `.git` **file**. From the repo root the current-working-directory fallback works; from a nested working directory output may land in the wrong place. VM-02 should use a worktree-aware marker and explicit output option.
2. World and lighting use `ScreenEffects.CameraOffset` (shake + kick); Soul Sense uses `ShakeOffset` alone. Source inspection predicts separation during cannon recoil. Capture the combined state before fixing in VM-05.
3. Mouse inversion uses a transform with zero visual offset. Choose and test an explicit logical-aim policy rather than blindly inverting recoil. Otherwise a rendering fix can alter weapon aim.
4. Direction is part of the `SpritePlayback` key, so turning restarts playback. Hollow swipe is 9/18 = 0.5 s but starts during a 0.42 s telegraph before its 0.13 s active window. Devourer slam is 16/16 = 1 s across 0.88 s telegraph + 0.18 s active phase. These are timing risks, not proof of a specific incorrect impact cell; VM-07 must inspect the sheets and align phases.
5. Art/VFX/presentation time advances before the hitstop early return; actor simulation freezes later. Fixed timestep and seeded RNG alone do not create deterministic captures. Draw itself initializes playback on first use. VM-03 must control update/draw cadence and reset clocks/owner playback.
6. Ambient particles are bounded, but combat particle and sprite-effect lists have no explicit cap. `SpriteVfxSystem` only removes nonlooping clips; its public Spawn can accept a looping clip. Define ownership/lifetime and saturation rules before growing VFX density.
7. `ArtAssets` loads textures directly without the audio system's fallback pattern. A missing required texture will fail content loading; distinguish required baseline content from optional polish and test both paths.
8. There is **no actual bloom extraction/blur, normal-map lighting, shadow map or shader chromatic-aberration pass** in the inspected runtime. Historical plans/manifest prose describe some broader effects; do not count them as implemented.
9. The legacy art audit assumes exactly 116 PNGs and validates delivery sheets more deeply than runtime/source parity. Preserve its historical-delivery purpose while adding an extensible inventory/validation layer in VM-01/17.
10. The player source filename `player_master_128.png` is misleading: the manifest records a 768×768 locked source. Read actual dimensions/metadata; runtime player clips use 128×128 cells.

## Visual evidence and its limits

Inspected [archived gameplay capture](../../art/ludo_delivery/review/06_final_ingame.png). It shows a strong Gothic perimeter, a very dark central combat floor, fine/dark Player detail and closely overlapping Hollow silhouettes. Those support priorities for floor value separation, actor anchoring and overlap/readability review. It includes OS chrome and a historical title bar; it is **not a fresh capture of this commit**, a golden baseline, or evidence of the present cinematic/HUD state. VM-02 must capture the current build.

## Documentation conflicts

- `docs/vision/CODEX_CURRENT_REPO_ADDENDUM.md` calls the repo a minimal greenfield starter. This is stale relative to all systems above. Preserve current architecture.
- `docs/mvp/` defines locked creative rules and historical implementation phases. Existing human review gates belong to that MVP workflow; this planning session does not restart those phases. Later Visual Max sessions follow their requested slice and report evidence.
- `docs/vision/ASSET_TOOL_NOTES.md` is an August tool exploration, not current pricing, account entitlement or mandatory purchasing authority.
- `art/ludo_delivery/GENERATION_MANIFEST.md` reports an earlier successful build/run. Today's results are separately recorded in [HANDOFF.md](HANDOFF.md).
- User context includes ElevenLabs usage; the inspected committed audio ledger states that this particular delivery uses Ludo. Preserve both facts and verify provenance per asset.

No applicable `AGENTS.md` was found in this checkout or the inspected ancestor paths. OpenSpec exists with a generic config; this package is intentionally in the requested `docs/visual-max/` and does not create a duplicate OpenSpec change.
