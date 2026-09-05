# Visual Max authoring workspace

This is the staging area for new visual work. It is not loaded by MonoGame.
Only a reviewed asset copied into `src/TheLostSoulOfFire/Content/` and listed
in `Content.mgcb` can ship.

Create one version directory per asset, such as
`art/visual-max/hollow-warning/v001/`. Copy
`docs/visual-max/templates/asset-manifest.json` to `manifest.json`, then keep
the brief, original provider download, normalized PNG, and review evidence in
that same version directory. `source/` can remain absent when no source is
available; say so explicitly in provenance rather than inventing it.

Run `python3 tools/visual-max/visual_assets.py validate` before promotion.
`BRIEF_READY` manifests intentionally validate without a derived image, which
makes placeholders and provider-unavailable work visible without breaking a
build.
