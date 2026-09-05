# Visual Max tools

All commands are offline and use only the standard Python library.

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
