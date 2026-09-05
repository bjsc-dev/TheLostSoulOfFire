# Working asset pipeline

Status: IMPLEMENTED_BASELINE (offline, zero provider calls).

The game remains MonoGame ContentManager + MGCB. Runtime art lives only in
`src/TheLostSoulOfFire/Content/Textures/`, is registered in
`Content/Content.mgcb`, and is loaded by `Rendering/ArtAssets.cs`. AI tools
never run during gameplay or the content build.

```text
brief → candidate in art/visual-max → local validation → review capture
      → approved copy into Content + MGCB + ArtAssets mapping → build
```

Use the historical `art/ludo_delivery/` as immutable delivery/provenance
evidence. New work goes in `art/visual-max/<asset-id>/<version>/`; its files
do not affect the game until promotion. That separation lets an operator keep
unusable AI candidates, hand-edits, or simple temporary replacements without
breaking the current playable set.

## One-command checks

```bash
python3 tools/visual-max/visual_assets.py inventory
python3 tools/visual-max/visual_assets.py validate
python3 tools/visual-max/visual_assets.py self-test
dotnet build TheLostSoulOfFire.sln
```

The inventory derives all loaded texture paths from `ArtAssets.cs`, checks
MGCB registration, dimensions/grid contracts, source hashes, and reports
uncompressed texture cost. It is intentionally separate from the old Ludo
audit, which retains its historical fixed-116-PNG contract.

## Promotion checklist

1. Copy the manifest template and complete the brief/provenance/budget fields.
2. Put immutable downloads in `source/`; put normalized straight-alpha PNGs in
   `derived/`. Do not put secrets or logged-in download URLs in the manifest.
3. Validate the candidate. A `BRIEF_READY` record may have no image; a
   promoted candidate requires a valid PNG, SHA-256, grid and anchors.
4. Capture it in the actual game. Approve only when a matched capture records
   a visible benefit; otherwise set `REJECTED` with a reason.
5. Copy the approved derivative to Content, add its exact relative path to
   MGCB, add/update its `ArtAssets` runtime key and rerun inventory/build.

Missing optional art should use the existing approved family or a procedural
presentation fallback. A missing required Content texture remains a build/load
error, never a silent generated replacement.
