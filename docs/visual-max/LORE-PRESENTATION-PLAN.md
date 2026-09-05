# Lore presentation plan

Status: READY_WITH_ASSUMPTIONS. Canon comes from [MVP lore](../mvp/01_LORE_AND_WORLD.md); future possibilities come from [Full Vision](../vision/FULL_VISION.md). The arena vignette below is a proposed interpretation, not new locked canon.

## Narrative contract

Life binds body and Soul; Death Flame separates an unnatural attachment. Death Flame is not evil. Destroying a hostile manifestation is different from releasing its Soul. A released Soul leaves; only metaphysical residue enters the Player's Core. The Player also remains trapped and cannot explain why. Do not reveal his origin, assert that both Flames are one force, add an exposition cutscene, or turn release into consuming souls for ammunition.

The presentation should let players infer these facts through repeated action. Soul Sense can suggest subjective memory; it must not become an authoritative historical surveillance recording. A small uncertain human gesture is more appropriate here than a full narrated explanation.

## Existing foundation

`Soul.cs` already models departure and residue as separate states. Devourer can target/imprison Souls. `SoulSensePresentation` draws trace paths, weak points and imprisoned Souls after world suppression. `CinematicPresentation` already provides title, arrival, wave transitions, death and orange Life Flame ending. `ArenaAtmosphere` already calms on completion. Extend these connections rather than adding dialogue trees, quest state or a codex menu for this pass.

## One arena vignette: the shift that did not end

Proposed working interpretation: a furnace hall continued demanding labor after its workers were gone. A bell, an abandoned work station and a sealed gate retain fragments of a repeated shift. Keep the event ambiguous: no invented real disaster, named culprit, faction or addiction plot becomes canon. The broad industrial-suffering themes in Full Vision can guide props without committing the whole future story.

| Motif / proposed location | Ordinary view | Soul Sense view | After release/completion | Asset/implementation scope |
|---|---|---|---|---|
| Bell and chain, upper perimeter | Bell hangs slightly crooked; chain occasionally shifts | A faint hand repeats an unfinished pull | Swing/tick subsides with completion calm | Existing geometry or one decorative prop; one trace pose sequence |
| Work station, left perimeter | Small discarded tool and worn foot marks | Two low-opacity silhouettes repeat a work gesture; one vanishes early | Foot marks remain, motion stops | One compact prop cluster and trace loop; no collision |
| Sealed gate, right perimeter | Repeated tally-like cuts and a blocked opening | A reaching silhouette stops short of the threshold | Trace releases outward and gate area becomes quiet | Reuse current gate position; overlay only, no new exit mechanic |

Positions are anchor regions, not assumed safe coordinates. VM-14 places them against actual camera framing and combat bounds using the harness. At most one decorative trace is emphasized at a time. Suppress trace opacity during nearby telegraphs; critical weak points keep their existing priority. Traces never look like enemies that can be hit or Souls that can be collected.

## Beat sequence

| Beat | Trigger in current architecture | Visual/audio treatment | Acceptance evidence |
|---|---|---|---|
| Arrival | Title → Intro | Brief arena scale and asymmetric landmark; one distant mechanical/bell accent | Entrance frame shows Player and navigable floor; controls appear before combat needs them |
| First confrontation | First Hollow telegraph/contact | Thin mask and exhausted pose; tangible strike instead of indiscriminate glowing body | Windup/contact/recovery strip identifies the enemy and action |
| Hidden anatomy | Soul Sense activation | World recedes first, then Core/trace layer emerges | Before/transition/after frames show anatomy without losing danger boundaries |
| Manifestation breaks | Enemy death → Soul creation | Dark body fragments fall or dissipate; pale Soul remains suspended | Body and Soul are visually distinct in at least one frame |
| Soul departs | Exposed → Releasing | Soul detaches into a separate outward/upward path and fades; quieter release sound | Full 1.25 s sequence shows departure away from Player |
| Residue returns | Releasing → Residue | Tiny violet remnants gather inward after the Soul has departed | 0.85 s sequence shows small residue, not the intact Soul, entering Core |
| Devourer contrast | BeingDevoured / stored Souls | Inward imprisonment remains sharp, constrained and troubling | Comparison strip distinguishes imprisonment from peaceful release |
| Relief | Final wave complete | Machinery/ash reduce, camera settles; moment of stillness | Same scene before/after has visible reduction in activity |
| Life Flame | Existing reveal at 1.05 s into completion | Small stable orange upward flame; quiet warm accent and minimal text | Flame reads without combat shake or covering title typography |
| Player death/retry | Player dead → R | Violet remnant; short retry transition; no false “Soul released” story | Death and ending side-by-side remain distinct; restart clears trace state |

The upward departure of a Soul is not a change to the Death Flame motion rule: the flame's separating action may move unnaturally while the freed Soul leaves calmly. Do not merge these visual subjects into one particle stream.

## Minimal presentation data

VM-14 may introduce a small `SoulTraceDefinition` record owned by presentation: ID, world anchor, motif, normal-visibility flag, sense opacity curve, frame/pose reference, looping duration, combat-suppression radius and completion behavior. Store definitions locally; no narrative service or save migration is needed. Begin with three definitions and deterministic phase offsets.

Trace state resets with `ResetEncounter`. Narrative flags may only select presentation; they cannot change hitboxes, wave progression, Soul release timings or collision. Optional caption strings stay separate from sprites and default off during combat. Existing title/death/ending copy remains unless VM-16 documents a specific replacement.

## Production and review

Prepare a normal/Soul-Sense/released triptych for each motif, then a short moving comparison. Astra can check whether the visual implication matches canon and whether the clue is visible at gameplay scale. Ludo can supply a matched prop/pose derivative if existing art and simple silhouettes cannot meet the rubric. ElevenLabs or Ludo can supply an original mechanical texture after a sound brief is justified; narration is unnecessary for this slice.

Acceptance requires zero canon contradictions, three distinct but related motifs, separate Soul/residue paths, and no competing combat marker. An agent can approve a reversible draft with documented interpretation; broad new story canon requires an explicit future narrative scope. A later player study can test comprehension, but its absence does not block implementation of this bounded presentation plan.
