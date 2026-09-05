# Visual asset folder and naming conventions

## Runtime versus authoring

| Location | Purpose | Commit guidance |
|---|---|---|
| `src/TheLostSoulOfFire/Content/Textures/` | Shipped PNGs | Only approved, MGCB-registered assets |
| `art/ludo_delivery/` | Historical delivery and locked source evidence | Do not overwrite |
| `art/visual-max/<asset-id>/<version>/` | Briefs, inputs, normalized candidates, review evidence | Commit small reviewed records; avoid bulk throwaways |
| `artifacts/screenshots/` | F9 captures | Ignored, local |
| `artifacts/visual-max/<run-id>/` | Capture/comparison runs | Ignored, local |
| `docs/visual-max/evidence/` | Small durable inventories/review decisions | Commit intentionally |

Each authoring version is self-contained:

```text
art/visual-max/<family-or-asset-id>/v001/
  manifest.json
  brief.md
  source/                 # immutable provider/local originals
  derived/                # normalized PNG candidates
  review/                 # contact sheet, selected crops, decision notes
```

Use lowercase kebab-case IDs (`hollow-warning`, `arena-floor-value`) and
monotonic versions (`v001`, `v002`). Runtime filenames remain lower snake case
and describe the actual geometry: `fx_cannon_charge_loop.png`,
`arena_base_1800x1000.png`. Directions are exactly `n ne e se s sw w nw`.

For a sprite sheet, write the actual cell size/frame count in the manifest; do
not infer it from a filename. The existing convention is row-major 3×3/9-frame
sheets, with 4×4/16-frame sheets for Devourer slam and selected VFX. Export
straight-alpha PNG. MGCB already premultiplies once, so never pre-premultiply
an input without declaring it and checking for fringes.

`manifest.json` is required from the brief onward. It records runtime key,
geometry, anchors, hashes, provenance, review result, and a bounded generation
budget. `BRIEF_READY`, `TOOL_UNAVAILABLE`, and `OPTIONAL_PENDING_BUDGET` are
valid placeholder states; they intentionally do not claim that artwork exists.
