# Visual Max master plan

Status: **READY_WITH_ASSUMPTIONS** · Baseline: `f510e4dcdefb837957c577cd197a8c22ca2f1116` · 2026-09-05

## Outcome and scope

Make the existing four-wave arena a distinctive, commercially presentable vertical slice of **Soulfire Gothic**: a small, readable bearer of violet Death Flame moves through a haunted industrial cathedral; violence is sharp and brief; releasing a Soul brings visible relief; the final orange Life Flame changes the emotional temperature of the scene.

“Max” means the best coherent presentation this game can sustain at 60 fps and reproduce through its production tools. It does not mean enabling every effect. The first deliverable is one exceptional arena, the existing Player/Hollow/Burning/Devourer cast, and the complete title-to-ending loop. Market competitiveness is a quality objective, not a sales prediction or a claim of parity with a larger studio.

Keep MonoGame DesktopGL, the .NET target, ContentManager/MGCB, Game1/GameWorld ownership, entity state machines, collision rules, and combat timers. Extend the existing rendering/presentation classes. No engine migration, ECS replacement, 3D conversion, new biome system, online generation dependency, or broad runtime refactor is part of this plan. Optional tools produce files for the existing stack.

This session delivers planning and contracts only. The backlog authorizes no paid generation, release publication, or implementation by itself; a later instruction to implement a slice supplies that scope.

## Read this package

| Document | Purpose |
|---|---|
| [REPOSITORY-AUDIT.md](REPOSITORY-AUDIT.md) | Current implementation, evidence, gaps, and historical-document conflicts |
| [ART-BIBLE.md](ART-BIBLE.md) | Palette, pixel treatment, silhouettes, composition, VFX grammar, readability |
| [TECHNICAL-ART-ROADMAP.md](TECHNICAL-ART-ROADMAP.md) | Incremental rendering changes, budgets, compatibility and fallback decisions |
| [LORE-PRESENTATION-PLAN.md](LORE-PRESENTATION-PLAN.md) | Canon, arena motifs, Soul Sense, release and ending beats |
| [AI-ASSET-PIPELINE.md](AI-ASSET-PIPELINE.md) | Astra/Ludo/ElevenLabs roles, asset contracts, bounded iteration and provenance |
| [VISUAL-QA-HARNESS.md](VISUAL-QA-HARNESS.md) | Capture fixtures, determinism, evidence and visual regression rules |
| [IMPLEMENTATION-BACKLOG.md](IMPLEMENTATION-BACKLOG.md) | Ordered implementation slices, dependencies and acceptance criteria |
| [HANDOFF.md](HANDOFF.md) | Verification results and next-session instructions |

The locked [MVP lore](../mvp/01_LORE_AND_WORLD.md) and [art direction](../mvp/02_ART_DIRECTION.md) remain creative authority. [Full Vision](../vision/FULL_VISION.md) supplies optional future ideas. Current code is implementation truth. This package is the execution order for Visual Max; older MVP phase numbers are not a second backlog to replay.

## Visual pillars

1. **A readable figure in monumental darkness.** Large arches, boilers and chains frame a quieter floor. The Player's asymmetric coat, scythe and Core remain identifiable before particles appear.
2. **Death Flame behaves incorrectly.** Violet shards converge, shear sideways, fall and reverse; white is a brief concentration of force. Light has a source and a reason.
3. **Impact has a shape and a rhythm.** Anticipation, strike and recovery are visibly different. A Hollow sweep, Burning charge and Devourer slam remain distinguishable in grayscale and with sound muted.
4. **The world remembers people.** A few meaningful objects, interrupted work motions and Soul Sense traces imply human history. Decorative density never becomes the main storytelling method.
5. **Release changes the mood.** A Soul departs independently; only residue returns to the Player. Machinery and ambient motion subside. Rare orange Life Flame delivers warmth without explaining the Player's origin.

## Decisions and assumptions

| ID | Default for autonomous follow-up | How to resolve without stalling |
|---|---|---|
| A01 | Polish the existing arena and four waves before expanding content | Finish the showcase gate; keep other region ideas in future notes |
| A02 | Keep `net9.0` and DesktopGL 3.8.x; current restore resolved 3.8.5.1 tooling | Record resolved dependencies; handle framework maintenance separately |
| A03 | 1280×720 is the baseline; test 1920×1080 and 1280×800 presentation | Add aspect-fit viewport/input mapping without changing arena bounds |
| A04 | Pixel-art-inspired hybrid, not a new strict 320×180 renderer | Preserve source clusters and PointClamp; smooth only light/low-frequency effects |
| A05 | Target 60 fps on a named integrated-GPU desktop reference machine | Record actual hardware in VM-01; until then performance is UNMEASURED |
| A06 | Existing approved assets remain usable; three locked source hashes remain immutable | Create versioned derivatives; structural or aesthetic failure does not require replacing the full cast |
| A07 | No new generation budget has been set | Prepare prompts and use existing assets/procedural layers; generation branch stays OPTIONAL_PENDING_BUDGET |
| A08 | Astra assists at authoring time; tool availability may vary | Keep prompts and artifact formats model-independent; use current coding/vision tools when unavailable |
| A09 | UI stays concise English, matching current runtime | Keep copy out of generated art and make new strings easy to extract; localization is later scope |
| A10 | No representative player study or current sales benchmark exists | Use explicit internal gates and record subjective findings; schedule external validation separately |

Routine choices inside these defaults do not need human coordination. Record deviations and the evidence behind them. Missing tools should block only their dependent operation. Never label an unrun capture or unknown licensing state as passed.

## Delivery sequence

| Gate | Slices | Tangible result | Exit condition |
|---|---|---|---|
| G0: reproducible baseline | VM-01–04 | Asset inventory, timed captures, deterministic scenes and comparison report | Every required fixture has evidence or an explicit unsupported-host result; no fabricated baselines |
| G1: readable combat | VM-05–09 | Unified coordinates, useful depth, timed poses, shape-coded threats, restrained feedback | All three attack families readable with Soul Sense/resonance, muted audio and reduced effects |
| G2: authored atmosphere | VM-10–11, VM-14–16 | Cohesive floor/perimeter, curated energy, trace vignette, relief/ending and HUD polish | The same arena communicates place, combat and release in unedited gameplay captures |
| G3: optional rendering ceiling | VM-12–13 | Selective emission bloom and a bounded material/shadow experiment | Clear visual improvement within measured budget; otherwise retain baseline rendering |
| G4: repeatable production and showcase | VM-17–19 | Offline asset workflow, timed audio/visual review, six stills and short footage | Clean build, no unreadable threats, provenance complete, evidence bundle reproducible |

Priority matters more than numeric order: G2 core work can finish without optional G3 shaders. Use the dependency table in the backlog as the executable ordering rule. Initial planning estimate: 25–40 focused coding/art sessions including integration and review; provider delays and new content are excluded. Re-estimate after G0, using actual throughput.

## Quality gates

- Readability: Player contour, aim and current threat survive the busy fixture at native size and a 640×360 review reduction. Critical cues have shape/motion redundancy; lighting and low-effect settings cannot hide them.
- Identity: six showcase captures form one palette and material language; warm fire appears intentionally in the ending; no generic blue/red elemental recolor set.
- Timing: damage windows remain governed by existing gameplay. Enemy strike poses and core-hit/audio cues align within one 60 Hz tick of the intended event, except explicitly documented artistic tails.
- Lore: a frame sequence distinguishes manifestation destruction, Soul departure and residue intake. A Soul never appears to be ammunition swallowed by the Player.
- Performance: provisional p95 work time ≤16.67 ms, p99 ≤25 ms at baseline resolution on the recorded reference device; no sustained regression >10% from the same fixture/configuration. See roadmap for measurement limits.
- Production: stable anchors, real alpha, consistent frame grids, licenses/source IDs, content paths and checksums accompany approved assets. No service access is needed to play the game.
- Handoff: each slice ends with changed paths, commands/results, captures, known limitations and the next ready slice. Passing code checks alone does not pass a visual gate.

## Showcase and market presentation

Aim for a viewer to understand scale, combat and the release premise from six actual gameplay stills: arena entrance, readable scythe strike, full cannon impact, Soul Sense revelation, Soul departure, final Life Flame. Add a 20–30 second sequence with anticipation and recovery intact. Inspect both full resolution and thumbnail crops; the Core alone cannot be the only visible feature of the protagonist.

Use the existing assets as the product being shown. Concept paintings, enlarged sprites and generated mockups are internal references and must not masquerade as gameplay. Storefront-specific dimensions and publishing are deferred until a destination is selected. If later comparing competitors, use current primary gameplay footage and record a dated comparison of readability, animation, atmosphere and feedback; do not infer commercial success from screenshots.

## Risks and decisions that keep work moving

The largest risks are uncontrolled violet brightness, baked lighting that conflicts with runtime lighting, attack animations that finish during telegraphs, and a visual harness that captures arbitrary real time. Address those before expanding asset volume. A custom shader may also require platform-specific build tools: prove one tiny effect first and retain the current glow renderer if it cannot ship reliably.

The next session should begin with **VM-01**, then **VM-02**. No runtime feature in this package is claimed to have been implemented in the planning session.
