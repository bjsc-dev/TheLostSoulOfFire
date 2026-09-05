# Screenshot and visual testing workflow

F9 still captures a complete post-HUD game frame to `artifacts/screenshots/`.
Each capture now gets an adjacent JSON file with context, fixed-update tick,
dimensions, timestamp, framework and OS/build identity.

For reproducible title/intro baselines, use the capture CLI:

```bash
# Current title after 30 fixed updates.
dotnet run --project src/TheLostSoulOfFire -- \
  --capture-after-ticks 30 --capture-output artifacts/visual-max/title \
  --exit-after-capture

# Start automatically on update 2 and capture the first arena/intro frame.
dotnet run --project src/TheLostSoulOfFire -- \
  --capture-start-at-tick 2 --capture-after-ticks 150 \
  --capture-output "artifacts/visual-max/first arena" --exit-after-capture
```

`--capture-output` accepts a directory or exact `.png` filename; an explicit
path takes precedence over the default and works from a worktree/nested
directory. Invalid capture arguments exit 2. A failed write exits 1 when
`--exit-after-capture` is used. Capture flags deliberately cannot mix with the
automated audio modes.

For quick manual states, start normally and use the existing debug controls:
F1 debug overlay, F2 Hollow, F3 Burning, F4 Devourer, F5 resonance ready, F6
kill active enemies, F7 Soul Sense, F8 reset, F9 screenshot. These are useful
inspection hooks, not deterministic golden scenarios.

Review paired frames at native resolution, 640×360, and grayscale. Keep
matching state/settings/tick in their sidecars; never compare two arbitrary
real-time screenshots as a regression result. Bulk runs stay ignored in
`artifacts/visual-max/`; promote only selected baseline PNGs and a review
decision to `docs/visual-max/evidence/`.

The next increment is the scenario runner described in
[VISUAL-QA-HARNESS.md](VISUAL-QA-HARNESS.md): semantic fixtures, seeded reset,
event strips, and heatmap comparisons. This capture primitive supplies its
shared output contract without pretending that free-play timing is deterministic.
