# FINAL REPORT - The Lost Cave visibility fix

Date: 2026-11-06

## Root cause(s) of overexposure
- URP Global Volume (SampleSceneProfile) Bloom: threshold 0.8, intensity 1.2, scatter 0.7, clamp not overridden (65472).
- Tonemapping: Neutral mode with paperWhite 234, maxNits 647 (over-bright HDR).
- Directional light intensity 2.0 (white), plus many point/spot lights at intensity 2.0-2.4.
- Fog default grey with density 0.01 (could wash out).

## Values changed (old -> new)
- Bloom: threshold 0.8 -> 1.1; intensity 1.2 -> 0.4; scatter 0.7 -> 0.5; clamp 65472 (unset) -> 2 (override).
- Tonemapping: paperWhite 234 -> 100; maxNits 647 -> 200.
- Directional lights: intensity 2.0 -> 1.0 (cap).
- Point lights: intensity 2.0 -> 1.2 (cap).
- Spot lights: intensity 2.4 -> 1.5 (cap).
- Camera near clip: 0.1 -> 0.3.
- Region fog/ambient colors and densities tuned per region (forest green haze, cave dark blue, vault warm gold).
- Atmosphere thresholds: firstStageEndZ 5000, secondStageEndZ 11000.

## Prefabs/chunks repaired for obstruction
- All section prefabs and VisualPolish decoration prefabs processed: top-level children whose bounds intersected the track clearance volume were pushed to x=±5.5, raised if canopy (y>4), and scaled to 0.9.

## Verified
- Project compiles with zero errors (batchmode).
- 900-slot route, keys, checkpoints, power-ups unchanged (code-level).

## Could NOT be verified
- Play Mode visuals/screenshots (no Play Mode run in this session).
- Actual white-blowout removal, foliage clearance from gameplay camera.
- HUD at 1280x720, audio balance.
- Screenshots not captured; Docs/Screenshots created but empty.

## Things to check in editor
- Play Mode in each region and both transitions; confirm no white blowout and no foliage blocking lanes/camera.
- Capture screenshots to Docs/Screenshots (fix_region1.png etc.).
- Verify light counts/perf; adjust if too dark.

## Final gameplay correction
- Right-lane start root cause: lane index computed from spawn X but no forced centre; fixed by forcing player X=0 and laneIndex=0 in Awake/StartFreshGame/Respawn, plus ResetToCentreLane. Also aligned pickup/obstacle X from ±2.5/±1.25 to ±3.5/±1.75.
- Duration: 169s simulated (~2:49), within 2:00-3:00. Per-region ~56s each.
- Build: Builds/Win_20261106_202352, launched OK.

## Final polish summary
- Lane width: 2.5 -> 3.5 units (laneSpacing 2.5 -> 3.5); road floor scale x 8 -> 11. Route duration ~4.7 min (152 slots / 3040 units).
- Intro panel added with Start Adventure button.
- Build: Windows x64 succeeded at Builds/Win_20261106_200621, launched OK.
- Measured sim duration: 4.72 min.

## Critical bug fix: missing runner road (FE_01_ForestGate anchor mismatch)
Root cause: the earlier FixObstruction pass moved and rescaled top-level children whose bounds intersected the track clearance volume, including the ENTRY_ANCHOR and EXIT_ANCHOR transforms (which have no renderer, so their bounds defaulted to a 1-unit box at their position). This set entry anchor to (5.5,0,0) scale 0.9 and exit anchor to (5.5,0,20) scale 0.9, breaking the validator's rule (entry at Z=0, exit at exact length, identity scale).

Evidence: before fix anchors were at (5.5,0,0)/(5.5,0,20) scale 0.9; restored from backup to (0,0,0)/(0,0,20) scale 1.

Fix: restored all section/decoration prefabs from backup, then re-ran FixObstruction with anchors excluded (name contains "anchor") and skipping objects with no renderers/colliders/particles. Updated AuditSections + ValidateSceneTrack tools.

Audit result: 20/20 section prefabs PASS; 6/6 scene sections VALID; no "refused an invalid preview section" expected.

Verified (batchmode): compile zero errors; all sections valid. Not verified: Play Mode road visibility/spawning (no Play Mode run). Check in editor: start Play Mode, confirm road under player and continuous spawning, jump/slide/collect.
---

## Current fix-pass status — 2026-11-07

The audit fix pass updated the saved runner route and key configuration. Unity batch compilation and guarded setup passed; Play Mode was not available, so this is not final gameplay acceptance.

- Two active key IDs are configured: ID 0 at (0, 1, 70) and ID 1 at (0, 1, 210). IDs 2 and 3 and four duplicate root preview pickups are inactive.
- The route startup uses connected slots 0–3. Region boundaries are Z=180 and Z=360; the final chest is at Z=500.
- Checkpoint respawns are limited to two before Game Over. Two purple Crystal pickups use the existing Crystal scoring/counter path.
- LostCave_Main is first and enabled in Build Settings; SampleScene is disabled.
- Batch compile: Unity 6000.3.24f1 exit 0, Tundra build success, zero C# errors and zero C# warnings.
- Guarded setup: exit 0, save marker present, scene reopened and validated. Scene SHA256: 3AA31ABF7E726CCC5CD6A76F22093E4178F851401F4F8102AFEDFA31973C90BA.
- No Play Mode, continuous playthrough, screenshots, FPS/timing measurements or fresh build were performed. The existing latest build at Builds/Win_20261106_231721/The Lost Cave.exe predates these changes.
- See FIX_RESULTS.md for the AUD-01–AUD-18 mapping, current evidence and manual verification list.