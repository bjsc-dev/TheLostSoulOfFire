# Visual Max handoff

Status: **IMPLEMENTATION_IN_PROGRESS** · baseline asset/capture slice updated 2026-09-05.

## Start here

Read [SCREENSHOT-WORKFLOW.md](SCREENSHOT-WORKFLOW.md) for the immediate low-touch capture command, then [IMPLEMENTATION-BACKLOG.md](IMPLEMENTATION-BACKLOG.md). VM-01 and VM-02 are DONE: inventory/validation, worktree-safe capture options, metadata sidecars, a reference-host record and current title/first-arena PNG baselines are present. VM-03 deterministic semantic fixtures is now the next ready slice.

Use this next-session instruction:

> Implement VM-03 deterministic visual fixtures without changing MonoGame, net9.0, Game1/GameWorld ownership, gameplay constants or locked-source hashes. Reuse the capture sidecar/output contract, arrange semantic state through narrow debug hooks and record unsupported-host results honestly.

For a later full-workstream request, select ready slices by priority and dependencies, complete one or a tightly coupled pair per session, and use [templates/session-handoff.md](templates/session-handoff.md). A missing optional provider does not block code, briefs, existing-asset derivatives or capture tooling. A missing required visual test remains NOT_RUN, not a presumed pass.

## What was delivered in the implementation slice

- Offline runtime asset inventory/validator at `tools/visual-max/visual_assets.py`, with locked-hash, MGCB-path, grid, runtime-key and temporary-fixture checks.
- A staged authoring root (`art/visual-max/`), manifest template and placeholder-friendly promotion states.
- Optional `--capture-after-ticks`, `--capture-output`, `--capture-start-at-tick` and `--exit-after-capture` support; screenshots now have tick/build JSON sidecars and resolve `.git` worktree files correctly.
- Operator docs for the asset pipeline, folders, Ludo/tool handoff, screenshots and a bounded Astra review loop.

## What was delivered in planning

- Six requested planning documents, plus repository audit, visual QA design and this handoff.
- Nineteen implementation slices with priorities, dependencies, scope, explicit acceptance criteria and evidence requirements.
- Sixteen named visual scenarios and a proposed CLI/result contract; implementation is explicitly deferred.
- Palette and cast rules, twelve VFX families, comfort controls, render-pass opportunities/budgets, an arena lore vignette and offline AI production workflow.
- Reusable asset-brief and session-handoff templates; no new runtime dependency, generated asset, plugin installation or paid call.

The OpenAI Docs skill informed the Astra section by checking current official model guidance. Proposed review/iteration roles remain project judgments, not claims of measured model superiority. Official MonoGame/Ludo/ElevenLabs sources are linked beside the relevant capability statements in the roadmap/pipeline.

## Baseline and verification

Source baseline: `f510e4dcdefb837957c577cd197a8c22ca2f1116`. Worktree was clean at the start. Host: macOS 26.5, osx-arm64, installed .NET SDK 10.0.301/runtime 10.0.9. Project still targets `net9.0`, with `RollForward=Major`. Restore resolved MonoGame framework/tooling 3.8.5.1. GPU model and frame performance were not measured in this planning session.

| Check | Result | Details |
|---|---|---|
| `dotnet restore TheLostSoulOfFire.sln --ignore-failed-sources` | PASS, exit 0 | Project restore completed |
| `dotnet build TheLostSoulOfFire.sln -c Release --no-restore` | PASS, exit 0 | Content processing and Release/net9.0 assembly; 0 warnings, 0 errors |
| `dotnet test TheLostSoulOfFire.sln --no-build --no-restore` | Exit 0; NO TEST SUITE | No test project exists; no unit-test pass count can be claimed |
| `python3 tools/audio/validate_audio.py` | PASS, exit 0 | 29 assets pass format, level, duration, loop, source and manifest checks |
| Default Python art audit | Initial environment failure | `ModuleNotFoundError: PIL`; no asset defect inferred |
| Art audit with existing Pillow-capable runtime | PASS, exit 0 | 3 locked hashes, 7 static checks, 96 animation sheets, 12 VFX sheets, 116 content PNGs |
| Automated gameplay in sandbox | Initial graphics-host failure, exit 134 | `NoSuitableGraphicsDeviceException` during OpenGL initialization |
| Same automated gameplay with desktop access | PASS, exit 0 | `AUDIO_GAMEPLAY_TEST_PASS waves=4 completion=true restart=true` |
| Fresh title + first-arena capture | PASS | 1280×720 post-HUD PNG + tick/build sidecars via new CLI; stored locally under ignored `artifacts/visual-max/` |
| Deterministic visual-regression suite | NOT_RUN | VM-03 semantic fixtures and VM-04 comparison reports are not implemented yet |
| Performance / non-macOS runtime | NOT_RUN | No measured GPU budget or Windows/Linux runtime claim |
| Documentation validation | PASS | Local links resolve across 11 Markdown files; all 6 requested files exist; 19 backlog sections match the index; whitespace/newline checks pass |

The current runtime smoke command is:

```bash
dotnet run --project src/TheLostSoulOfFire --no-build -- --audio-gameplay-test
```

It uses injected F6 kills to advance through the four-wave loop and ending/restart. It verifies lifecycle progression, not skilled combat play, visual readability or whether audio was subjectively heard. A host with working desktop/OpenGL access is required; the successful run followed the sandbox graphics failure without any game-code change.

For the art audit, use an interpreter with Pillow:

```bash
python3 art/ludo_delivery/tools/audit_delivery.py
```

The successful interpreter on this host was `/Users/user/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/bin/python3`. That is a host-specific fallback, not a path to hardcode into new tooling. No package installation was needed. The ordinary Python audio validator also needs the Vorbis utilities documented by that script for full music checks; they were available here.

Other existing runtime modes, **not run in this session**, are `--audio-runtime-test`, `--audio-loop-runtime-test` and `--audio-death-restart-test`. `--expect-audio-fallback` is meaningful when deliberately testing missing audio content; do not pass it to an intact-content run and expect a normal pass. Use only relevant modes after implementation changes.

## Findings future sessions must preserve

1. The repo is not a starter. The older current-repo addendum is stale; preserve existing GameWorld/entity/presentation structure.
2. Current light is additive radial glow plus scene tint/veil/vignette. True bloom, normal maps, shadow maps and shader chromatic effects are proposals, not implemented baseline features.
3. Screenshot root detection misses `.git` files. World/Soul Sense transforms differ during recoil. Both are evidenced early work, not a reason to replace the renderer.
4. Direction changes restart clip keys; some attack clips begin during telegraphs and risk timing drift. Inspect actual contact cells before retiming.
5. Existing alpha/grid/locked-source audit is valuable but assumes exactly 116 PNGs; make future inventories extensible without rewriting history.
6. The inspected archived in-game image has dark central floor and difficult actor separation; obtain current screenshots before treating those as present-day measured defects.
7. Current committed audio ledger identifies Ludo sources. ElevenLabs fits later authoring, but existing files must keep their actual provenance.
8. No new generation budget, target reference GPU or representative player study was supplied. Defaults and independent work paths are in the master plan.

## Completion protocol

For each future slice, save its small durable evidence record, update the status/dependencies in the backlog, and replace the next-session pointer here. Keep bulk captures in ignored `artifacts/visual-max/`; preserve selected baseline images/manifests intentionally so another session can reproduce the comparison. Do not mark a slice DONE from build success alone when it changes visible behavior.

No commit or push was made in this planning session. Runtime files, Content assets and OpenSpec/agent tooling were left unchanged.
