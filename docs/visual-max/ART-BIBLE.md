# Art bible — Soulfire Gothic

Status: READY_WITH_ASSUMPTIONS. Extends the locked [MVP art direction](../mvp/02_ART_DIRECTION.md); implementation work is listed in [the backlog](IMPLEMENTATION-BACKLOG.md).

## Image to build toward

Cold stone and exhausted machinery form an asymmetrical cathedral around a legible fighting floor. The Player is slender, angular and visibly burdened by a heavy cannon; the scythe cuts a broad, simple silhouette. Violet energy is concentrated in cores, wounds and momentary actions. Behind the violence, small remnants of human work make the arena feel inhabited by memory. Orange belongs to the Life Flame reveal.

The visual hierarchy in active combat is: imminent danger and Player location → target/weak point/projectile → Soul state and interactable information → architecture → ambient detail. In a calm scene, architecture and memory can take the foreground. Decorative effects never gain priority merely by being brighter.

## Palette logic

These base swatches are copied from current `GameBalance`; they are starting tokens, not a global quantization requirement. Measure final captured colors after tinting and blending.

| Role | Hex | Usage |
|---|---|---|
| Void | `#07060C` | Deep recesses, outer framing; avoid filling the whole traversable floor with it |
| Floor | `#13121B` | Quiet floor mass |
| Floor detail | `#1F1C2A` | Seams, worn tile and sparse soot patterns |
| Stone | `#272630` | Structural planes and larger silhouette separation |
| Metal | `#3D3C48` | Restrained edges, weapon material and machine accents |
| Death exterior | `#3F1870` | Deep energy envelope, angular trails |
| Death energy | `#9147FF` | Core/body of supernatural effects |
| Death bright | `#DDBEFF` | Short rims, charged states and readable warnings |
| Soul white | `#F6EFFF` | Soul identity and tiny high-intensity impact cores |
| Sense trace | `#AA70E8` | Lower-priority environmental memory |
| Life amber | `#FFB252` | Existing completion accent; proposed shared Life Flame token |
| Life pale | `#FFE8B2` | Proposed small warm interior; final flame may approach white |

Ordinary stone/metal remain low saturation. Off-white masks are material highlights, not light sources. Every family shares violet but differs in contour, cadence and direction. Burning is violet supernatural combustion; orange must not become a generic danger color. Do not use cyan debug geometry as a shipping visual language.

Starting composition targets, measured as review heuristics: roughly 70–85% quiet/dark world, 10–25% midtone structure/actors, and ≤5% very bright pixels in steady combat. Impact frames can exceed the last number briefly; they must return quickly to readable combat. These are art-direction starting points, not validated accessibility metrics. Tune exposure locally before raising global brightness or glow.

## Pixel density and material treatment

Preserve the established pixel-art-inspired hybrid. Character/world textures use PointClamp with no mipmaps. Smooth radial light, low-opacity haze and trails may coexist with crisp sprites; whole-scene blur and glossy material gradients weaken the look.

Current runtime cells: Player/Hollow/Burning 128×128, Devourer 192×192; effects 128×128 or 256×256. Typical displayed frame extents: Player 100, Hollow 112, Burning 104, Devourer 174 world units before zoom (Devourer grows with consumed Souls). Transparent margins mean these are not body heights. Keep the existing sizes until an occupancy/anchor contact sheet establishes a better common scale.

Use consistent clusters and a few intentional edges, not single-pixel noise everywhere. Review each sprite at its in-game scale and while moving. Texture filtering alone does not guarantee stable pixels when sprites/camera use fractional transforms. VM-05 evaluates render-only snapping of the physical scene while simulation remains continuous. Do not shrink the entire game into a retro virtual resolution as an unreviewed shortcut.

Materials need different large-scale behavior: stone has broken planar chips; metal has narrow cold edge accents; fabric has broad torn folds; ash has soft low-contrast accumulation. Avoid painting emissive violet into every material. Runtime light cannot remove a contradictory highlight baked into source art.

## Cast and anchors

| Subject | Required silhouette | Anchor/production constraints |
|---|---|---|
| Player | Slender asymmetric coat, long scythe arc, heavy cannon mass | Preserve locked master; runtime Core remains separate; stable feet, chest, grip and muzzle markers; do not bake duplicate weapons/Core into new overlays |
| Hollow | Tall narrow body and unmistakable pale mask | Swipe anticipates from one side; mask/material remains readable without a full-body glow |
| Burning | Compact forward pressure, split/cracked form | Charge direction is visible in pose and ground lane before movement; cracks do not make the whole body white |
| Devourer | Broad heavy mass and torso opening | Hands/frame support slam weight; trapped Souls stay inside torso region and are distinct from outside release Souls |
| Lost Soul | Small pale suspended presence | Departure silhouette differs from inward residue specks; never looks like a projectile |

Anchors are metadata, initially calibrated from the current sheets. Avoid per-frame automatic bounding-box recentering: it creates foot sliding and moving sockets. Start with one common cell-space anchor per character/action, then add hand-authored per-frame sockets only where necessary. Rotate with eight directions and preserve action phase while facing changes.

## Arena composition and environmental kit

Keep the 1800×1000 arena and `(105,95,1590,810)` combat rectangle. One baked background already exists. First improve its readability with versioned floor detail/lighting overlays; introduce a full tilemap only if later content scope requires it.

Proposed first kit: two floor wear decals, two pipe/chain silhouettes, one bell/furnace focal prop, one abandoned personal-object cluster, one simple contact shadow. This is a maximum initial set, not a request to generate eight assets immediately. Place the tallest/densest props at the perimeter. Ground decals do not change collision. Tall props need occlusion/fade rules before crossing walkable space. Existing decorative architecture must not imply a new hidden collision boundary.

Keep a clear central fight area, quiet approach lanes, a recognizable exit/ending focal point and one asymmetric landmark. Perspective and light direction must match the locked gameplay anchor. Decorative Soul traces should avoid the screen center during overlapping attacks. Render materials/light first; add more ornaments only when a before/after capture demonstrates a storytelling gain.

## VFX families

Durations below are art envelopes to fit inside current state timing, not replacement gameplay constants. Existing clip lifetimes can supply tails; the contact frame must be mapped to the actual event.

| Family / current assets | Shape and motion | Rhythm / brightness | Readability constraint |
|---|---|---|---|
| Scythe: slash 01/02, cleave | Thin directional crescent; cleave broader with angular breaks | Faint windup; narrow bright contact; fading 0.15–0.30 s wake | Trail follows range/facing; never advertises damage beyond the true active region |
| Dash ignition | Compressed source burst, backward coat echoes and sideways/downward shards | Quick ignition at start; trail thins before next dash | New Player position reads immediately; old images cannot look equally solid |
| Cannon charge/full/muzzle/projectile | Inward convergence → mechanical latch/core → compact forward bolt | Distinct charge stages; brief white muzzle; violet tail | No screen-filling charge aura; projectile head and aim stay visible |
| Core hit | Small angular fracture at actual weak point | Stronger than body hit, local 1–3-frame accent plus short decay | Does not cover the enemy windup or masquerade as another Soul |
| Hollow threat | Open sweep wedge + pale leading edge | Continuous anticipation; clear active edge then empty recovery | Shape still identifies a sweep in grayscale |
| Burning threat/detonation | Broken forward lane/chevrons; separate radial fracture burst | Accelerating cracks while warning; local explosion at event | Lane and blast extent derive from gameplay; no orange fireball |
| Devourer slam/devour | Heavy grounded ring; inward torso threads with imprisoned silhouettes | Slow weighted anticipation; abrupt slam; sustained inward pull | Slam radius and imprisoned Souls remain readable during resonance |
| Soul release/residue | Pale silhouette detaches outward/upward; small separate inward specks | Quiet 1.25 s release; 0.85 s residue travel uses current timings | Two visual paths; brightness falls with relief, not another explosion |
| Resonance activate/sustain | Brief angular expanding crown, then tighter coherent Core/trails | Activation peak; restrained 10 s sustain with current gameplay | Sustained mode cannot whitewash all warnings or force a permanent flash |
| Player death flame | Small unstable violet remnant after coat collapse | Collapse and low pulse | Distinct from warm ending flame; retry remains legible |
| Atmosphere/memory | Sparse ash, furnace breath, work-motion echoes | Slow, low contrast, locally responsive | Existing pool remains bounded; combat suppresses optional memory detail |
| Life Flame | Soft upward organic flame and gentle warm pool | Stable breathing reveal | Warmth stays exceptional; no recoil, hard shock ring or violent shake |

## Combat readability and comfort

Preserve current warning durations: Hollow 0.42 s, Burning charge 0.62 s, Devourer slam 0.88 s. Shapes appear at the beginning of those states, not after a generated clip reaches a convenient cell. Active/recovery transitions must agree with damage logic. Make friendly energy and enemy danger distinguishable by shape, movement and placement despite sharing violet.

Warnings use a dark separator plus a pale/violet edge, with low-opacity interiors. Draw persistent critical information after decorative emission and distortion. Avoid an opaque filled disk under a crowded fight. Keep weak-point markers attached to their world anchors and leave the Player visible when behind a Devourer or prop.

VM-09 adds independent presentation controls: shake/kick intensity, screen flash intensity, chromatic/distortion intensity if introduced, ambient density, and a reduced-effects preset. Preset changes preserve gameplay hitstop and damage timing. At zero shake/flash, use pose, local hit spark and audio to preserve contact. Reduced effects must not reduce warning duration, Core visibility or Soul state information. This is a comfort-oriented design, not a photosensitivity certification.

## Art review rubric

Score 0–2 for each: silhouette, palette discipline, cluster/material consistency, phase readability, lore meaning. A release candidate needs ≥8/10 and no zero; silhouette and phase readability must each be 2. Score with a saved contact sheet, native gameplay crop and short event sequence. Agent scoring is an internal review signal, not user-study evidence. Reject baked checkerboards, drifting anatomy, duplicate Core/weapon parts, foot jitter, cropped attacks and lighting seams regardless of total score.
