# AI-assisted asset pipeline

Status: READY_WITH_ASSUMPTIONS. This is an offline authoring workflow. No generation call, subscription change or paid asset batch was made for this plan.

## Roles and evidence

| Tool | Best role here | Boundary / fallback |
|---|---|---|
| Astra in the coding workspace | Inspect paired captures; identify the three highest-impact defects; propose one change; implement harness/presentation slices; review lore consistency; prepare shot lists and generation briefs | Proposed project workflow, not a benchmark result for this game. Code/image evidence and deterministic checks decide acceptance. Record actual available tools; ordinary scripts/current coding tools can continue without Astra |
| Ludo | Reference-guided props, directional sprite variants, short VFX sheets and optional sound candidates | Reuse this repo's approved pack first. Normalize and validate downloaded results; do not trust preview grids, alignment or alpha automatically |
| ElevenLabs | Focused original sound effects, furnace/chain textures and quiet release/ending accents | Authoring only; master into current WAV/OGG formats and map existing AudioCue events. This repo's current committed ledger is Ludo-based; do not relabel it |
| Existing Python art/audio scripts | Grid/alpha/hash audits, normalization, contact sheets, mastering and file-format checks | Extend portable path handling and manifest contracts; keep old source references immutable |
| Optional local editor/exporter | Hand-clean clusters, alpha edges, anchors and effect timing; export ordinary PNG frames | Use an already available editor. No new purchase or engine plugin is required |
| Optional procedural VFX authoring | Precisely controlled loops exported to PNG sheets | Consider only if a named effect fails in current tools; exported frames must fit ArtAssets |

Official OpenAI guidance documents Astra for multistep software/tool workflows. The choice to use it for this project's review–edit–capture cycle is a planning judgment; this session does not establish a quality/cost advantage for Soulfire. [OpenAI model guidance, checked 2026-09-05](https://developers.openai.com/api/docs/guides/latest-model).

Ludo's documentation provides sprite-sheet PNG export and animation-pack alignment workflows. Treat those as authoring conveniences and still validate the delivered file. [Ludo Sprite Generator](https://ludo.ai/docs/sprite-generator).

ElevenLabs documents sound generation with duration control and a loop option for its v2 sound-effects model. Recheck available model/options before a batch; short combat transients may need trimming from longer generated audio. [ElevenLabs sound-effects API](https://elevenlabs.io/docs/api-reference/text-to-sound-effects/convert).

PixelLab, Scenario and Pixelpart appear in older repo tool notes. They remain optional candidates, not new dependencies or validated recommendations in this plan. A future trial must answer a concrete failure (direction consistency, style expansion or loop timing), verify current export/access, and use the same artifact contract. Do not switch services merely to increase output volume.

## Artifact flow

```text
Backlog need + failed capture/rubric
  → brief + reference hashes + target runtime contract
  → existing asset / local derivative / bounded generation candidate
  → download and immutable source record
  → normalize + structural validation + contact sheet
  → in-game A/B capture + rubric + provenance review
  → approved version → Content.mgcb + ArtAssets mapping
  → build + scenario regression → handoff evidence
```

A future workspace helper/plugin may bundle these steps after VM-17 stabilizes them. Start with explicit files and scripts; a plugin is packaging, not a required framework or runtime subsystem.

## Directory and promotion policy

Current sources stay in `art/ludo_delivery/`; committed production assets stay in `src/TheLostSoulOfFire/Content/Textures/` and `Content/Audio/`. Suggested new authoring root: `art/visual-max/<asset-id>/<version>/` with `brief.md`, `source/`, `derived/`, `review/`, and `manifest.json`. Create only directories needed by a slice. Temporary captures/reports use ignored `artifacts/visual-max/<run-id>/`. Approved compact evidence/manifest records can be committed under `docs/visual-max/evidence/`; do not commit every generated candidate by default.

Preserve the three locked references validated by `audit_delivery.py`: gameplay style anchor, Player master and original physical scythe. Use a new versioned derivative path for an approved change. Existing generator URLs are not durable provenance; the repository notes expiring Ludo links. Download sources during the authorized generation session and record checksum/provider request ID. Never store API keys or authenticated URLs in a committed manifest.

Promotion states: `BRIEF_READY → CANDIDATE → STRUCTURAL_PASS → VISUAL_PASS → APPROVED → INTEGRATED`. `REJECTED` keeps a reason; `OPTIONAL_PENDING_BUDGET` or `TOOL_UNAVAILABLE` affects the candidate branch only. Promotion requires both a technical result and a visible in-game benefit. The approving reviewer may be the implementing agent for reversible local changes under an implementation request; uncertain public distribution rights remain unresolved rather than guessed.

## Asset contract

Use [templates/asset-brief.md](templates/asset-brief.md) for every new family. The later manifest validator in VM-17 should require:

| Field group | Required information |
|---|---|
| Identity | Schema version, stable asset ID, variant/version, status, owning backlog slice, intended runtime key |
| Provenance | Provider/tool and actual model if exposed; request ID; creation/download date; exact prompt and negative constraints; seed if exposed; reference paths and SHA-256; source/derived checksums; local processing steps |
| Rights | Source/license evidence path or URL and check date; actual account entitlement if relevant; unknown facts explicitly null/unknown; no inferred blanket commercial clearance |
| Geometry | Actual image width/height, frame width/height/count, columns/rows, direction naming, center/feet/grip/Core/muzzle anchors, display scale, occupancy review |
| Playback | FPS or phase-to-frame ranges, loop/one-shot, contact cell, orientation, blend mode, layer and owning gameplay trigger |
| Quality | Alpha test, grid test, continuity/anchor review, palette rubric, before/after gameplay capture paths, accepted exceptions |
| Integration | Content path, MGCB importer/processor options, ArtAssets key, required/optional designation, previous-version rollback path |

Do not infer dimensions from filenames. Existing clips mostly use 3×3 grids of nine frames; Devourer slam and selected effects use 4×4 grids of sixteen. New grids may differ only with matching metadata and source-rectangle validation. Preserve straight-alpha PNG sources and let MGCB premultiply once; mark any already-premultiplied inputs to prevent double processing. Validate on black, neutral gray and light checker backgrounds to expose halos and baked checker patterns.

## Bounded generation and selection

Default to zero paid calls until a future session has a budget. A generation brief can be completed without one. When generation is authorized, set a per-batch currency/credit cap using current tool cost, maximum candidates per asset (default two), one retry for an explicitly diagnosed defect, and a maximum elapsed authoring window. Do not copy the historical manifest's credit estimates as current prices. Never retry indefinitely or regenerate a whole directional set to fix one bad cell.

Generate one canonical angle/pose before eight directions; confirm style and occupied scale, then expand. Produce one validated loop before producing a whole family. If generation fails: use an existing source derivative, hand/procedural cleanup, or a simpler effect. Keep the rejected candidate's reason and request ID so a future session does not repeat it.

## Astra work packets

Highest-return packet: baseline and candidate screenshots at identical seed/tick/camera, close crops of Player/telegraph, short contact strip, active settings, frame metrics, palette rules and relevant code. Ask for ranked visible defects with coordinates/frames, one minimal edit, expected improvement and the exact fixture to rerun. Do not ask it to “make everything cinematic” without a controlled comparison.

Useful specialized packets are animation contact-sheet reviews across directions; lore triptychs; shader alpha/coordinate debugging; asset-family briefs; and six-shot showcase selection. Require evidence links and uncertainty for every judgment. A model review cannot certify stable frame rate, rights, player comprehension or temporal behavior from one screenshot. Keep measurement scripts and runtime logs independent of model ratings.

A later automation loop should select one ready slice, write its implementation/evidence, validate, and update the backlog/handoff. Use the [session handoff template](templates/session-handoff.md). Keep credentials in the existing tool environment, downloads local, and generated assets out of the game until promotion gates pass.
