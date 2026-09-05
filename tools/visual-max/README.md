# Visual Max tools

Asset inventory commands are offline and use only the standard Python library.

```bash
python3 tools/visual-max/visual_assets.py inventory
python3 tools/visual-max/visual_assets.py validate
python3 tools/visual-max/visual_assets.py self-test
```

`inventory` derives runtime paths and sprite metadata from `ArtAssets.cs`, checks
that each loaded texture is registered in `Content.mgcb`, verifies the locked
source hashes, and writes a reviewable JSON inventory. `validate` checks only
new versioned work beneath `art/visual-max/`; it intentionally does not alter
the historical Ludo delivery audit or its fixed 116-file claim.

Native visual review (requires a DesktopGL graphics host and a Release build):

```bash
bash tools/visual-max/capture-astra-review.sh artifacts/visual-review/astra/final
python3 tools/visual-max/check-captures.py artifacts/visual-review/astra/final --require-suite
```

The checker uses Pillow, also required by the historical art audit. Add `quick`
as the capture script's second argument for twelve scenarios and three quality
comparisons without the event matrix. See [the handoff](../../docs/visual-max/ASTRA-FINAL-HANDOFF.md)
for exact flags, semantic evidence, current limits and this host's interpreter.
