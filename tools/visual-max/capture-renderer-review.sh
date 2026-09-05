#!/usr/bin/env bash
set -euo pipefail

workspace_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$workspace_root"

for scenario in \
  title-arrival \
  arena-idle \
  dash \
  scythe-combo \
  hollow-swipe \
  burning-charge \
  devourer-slam \
  cannon-sense \
  resonance-busy \
  soul-release \
  death-retry \
  ending
do
  dotnet run -c Release --no-build --project src/TheLostSoulOfFire -- \
    --visual-scenario "$scenario" \
    --visual-quality high \
    --capture-output artifacts/visual-review/renderer
done

dotnet run -c Release --no-build --project src/TheLostSoulOfFire -- \
  --visual-scenario cannon-sense \
  --visual-quality baseline \
  --capture-output artifacts/visual-review/renderer/baseline

dotnet run -c Release --no-build --project src/TheLostSoulOfFire -- \
  --visual-scenario resonance-busy \
  --visual-quality high \
  --reduced-effects \
  --capture-output artifacts/visual-review/renderer/reduced
