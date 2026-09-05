# Future Astra-assisted visual loop

This is a handoff design, not a runtime integration or an assertion that any
model can approve art automatically.

1. Pick one ready backlog slice and a fixed scenario/settings/tick.
2. Package baseline and candidate PNGs, the JSON sidecars, 2–3 important crops,
   a short event strip, active asset manifests, and the relevant art-bible rule.
3. Ask Astra for a ranked list of observable defects with coordinates/frame
   references, one minimal change, predicted result, and a requested rerun.
4. Apply only the scoped change. Run asset validation, build, capture the same
   fixture, then compare manually and structurally.
5. Record a human/owner decision: APPROVED, REJECTED, or NEEDS_MORE_EVIDENCE.
   Include changed paths, commands, captures, limitations and next slice in
   `templates/session-handoff.md`.

Recommended review prompt:

> Compare these matched in-game captures at the stated tick and settings.
> Rank the three most visible issues affecting Player/threat readability or
> Soulfire identity. Cite pixel regions or event frames, distinguish observed
> facts from inferences, propose one smallest reversible edit per issue, and
> name the exact capture to rerun. Do not assess licensing, frame performance,
> or player comprehension from the images alone.

The loop must stop after one hypothesis per iteration. It cannot turn an
unavailable graphics host, provider, rights record, or subjective player study
into a pass. Keep generated candidates outside runtime Content until the normal
promotion gate succeeds.
