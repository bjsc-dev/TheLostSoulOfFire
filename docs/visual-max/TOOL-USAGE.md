# External-tool handoff

External generators are optional offline authoring tools. The only integration
surface is an ordinary PNG plus its local manifest; no API key, SDK, or service
access is added to the MonoGame project.

## Ludo or a comparable image tool

Start with `docs/visual-max/templates/asset-brief.md`, then make one canonical
pose or loop before requesting a complete family. Supply the style anchor and
the exact output contract: transparent PNG, frame/cell dimensions, directions,
anchors, no typography, no baked Core, no checkerboard background, and no
mirroring unless approved. Download the original immediately to `source/`;
preview pages and expiring URLs are not provenance.

Normalize into `derived/`, calculate the SHA-256, complete the manifest and
run the validator. Inspect alpha against black/gray/light backgrounds and make
a contact sheet before runtime integration. Default cap: two candidates and
one diagnosis-driven retry. With no approved budget, stop at `BRIEF_READY` and
use an existing/procedural fallback.

## Optional future tools

The same contract works for a local editor, PixelLab/Scenario/Pixelpart trial,
or procedural export. A trial needs a specific problem to solve (for example,
direction consistency), source/right evidence, and the same review capture.
Do not change providers merely to increase volume. Sound tools remain governed
by the separate audio ledger and `tools/audio/validate_audio.py`.

## Prompt pack

Use this as a compact starting suffix, then add the asset brief's subject and
geometry:

> High three-quarter Gothic-industrial pixel-art-inspired game asset for The
> Lost Soul of Fire; charcoal and cold gray metal, restrained pale-violet
> soulfire, sparse bright accents, readable silhouette at 1280×720, transparent
> background, no text, no UI, no watermark, no checkerboard, no generic neon
> magic, no baked character Core. Preserve a quiet combat-floor value range.

For animation: “row-major `<columns>`×`<rows>` sheet, `<frameWidth>`×
`<frameHeight>` cells, stable feet at `<x,y>`, no camera rotation between
frames.” Treat this as an input brief, not a guarantee that the export is
usable.
