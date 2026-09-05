# VM-01 reference-machine record

Recorded 2026-09-05 in the `art-pipeline-harness` worktree.

| Field | Result |
|---|---|
| Host OS/GPU | macOS 26.5.2 (arm64), Apple M1 Pro integrated GPU (16 cores) |
| .NET SDK used | 10.0.301 |
| Project target | `net9.0`, `RollForward=Major` |
| Resolved MonoGame tools | 3.8.5.1 |
| Release/verification command | `dotnet build TheLostSoulOfFire.sln --no-restore` |
| Build result | PASS after the pipeline change |
| DesktopGL capture | PASS — title and first-arena 1280×720 PNG + JSON sidecars captured with `--exit-after-capture` |
| Performance | NOT_RUN — this slice records no p95/p99 frame budget measurement |

This is a build/inventory/capture reference, not a performance reference
machine. p95/p99 measurements must still be recorded when VM-03/VM-04 fixtures
are run.
