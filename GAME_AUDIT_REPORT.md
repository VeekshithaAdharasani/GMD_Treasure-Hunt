# Treasure Hunt – The Lost Cave: Full Audit

- **Audit date:** 2026-11-07
- **Project:** `D:\veekshitha\GMDD_GAMES\Treasure Hunt - The Lost Cave`
- **Unity:** 6000.3.24f1, URP
- **Scope:** Read-only inspection of saved source, scene YAML, prefabs/configuration, Build Settings and the available Unity Editor log. No scene, script, prefab, settings, asset or backup was changed. No Play Mode, build, setup script or compile command was run for this audit.
- **Overall verdict:** **Not ready for acceptance.** Static inspection indicates the runtime track manager will reject the saved startup sections; the scene requires two keys while four active keys and four-key text/tooling remain; the chest is before the Vault boundary; and Game Over is bypassed by the current checkpoint state. Runtime/visual behavior remains unverified.

## Evidence index

- **E1 — Saved scene:** `Assets/Scenes/LostCave_Main.unity` (last saved 2026-11-07 00:03:47; 2,667,788 bytes). Scene objects, prefab overrides, active flags, positions and serialized GameManager values below were read from this file.
- **E2 — Runtime scripts:** `Assets/Scripts/RunnerTrackManager.cs`, `GameManager.cs`, `TreasureKey.cs`, `TreasureChest.cs`, `PlayerMovement.cs`, `CameraFollow.cs`, `Collectible.cs`, `RunnerTrackSection.cs`.
- **E3 — Key prefab:** `Assets/Prefabs/Runner/Pickups/Runner_TreasureKey.prefab`; active pickup prefab with `TreasureKey`, trigger `CapsuleCollider` (radius 0.8) and visual model.
- **E4 — Region and environment configuration:** `Assets/VisualPolish/EnvironmentFramework/` region configuration assets and assigned scene components. Existing section audit log `audit5.log` reports 20 passed / 0 failed section prefab validations; this validates prefab geometry/anchors only, not the saved live route or gameplay.
- **E5 — Current Unity Editor log:** `C:\Users\Anveekshith\AppData\Local\Unity\Editor\Editor.log`, timestamp 2026-11-07 13:46:04. It contains 175 repetitions of the URP additional-light shadow-atlas warning; no `error CS`, `NullReferenceException`, or `MissingReferenceException` strings were found. This is a global Editor log and is not a substitute for a fresh project compile or Play Mode Console check.
- **E6 — Previous build/logs:** `Builds/Win_20261106_231721` and prior `compile11.log` / `build6.log`. The build and explicit compile predate the latest scene and `TreasureChest.cs` timestamps, so neither verifies the current saved state.
- **E7 — Build Settings:** `ProjectSettings/EditorBuildSettings.asset`.
- **E8 — Existing project reports:** `FINAL_REPORT.md`, `MILESTONE_LOG.md`, `PROJECT_STATE.md`; they do not describe the current saved configuration consistently.

## Key inventory (saved scene)

| ID | Scene object | Position (world) | Saved active/config state | Route observation |
|---:|---|---|---|---|
| 0 | `GoldenKey_ID_0` | `(2.5, 1, 70)` | Active prefab instance; ID 0; key renderer and collider overrides enabled | Z 70 is in Level 1 (0–160). |
| 1 | `GoldenKey_ID_1` | `(-2.5, 1, 90)` | Active prefab instance; ID 1; key renderer and collider enabled | Z 90 is in Level 1 (0–160). |
| 2 | `GoldenKey_ID_2` | `(-2.5, 1, 130)` | Active scene object; ID 2 | Z 130 is in Level 1 and ID 2 is rejected when the scene's requirement is 2. |
| 3 | `GoldenKey_ID_3` | `(0, 1, 150)` | Active scene object; ID 3 | Z 150 is in Level 1 and ID 3 is rejected when the scene's requirement is 2. |

All four explicit scene pickups are active and uniquely identified in the saved scene. Player lane centers are `x = -3.5, 0, +3.5`; the four positions are offset by 1m for IDs 0–2 and centered for ID 3. The key prefab trigger radius is 0.8m. This makes overlap plausible, but actual lane reachability and collection were **not tested**. The Forgotten Entrance boundary is Z=160, so none of the four explicit keys is saved in Levels 2 or 3. The active FE_01 startup preview also contains a prefab-embedded key (ID 0); `RunnerTrackManager` disables embedded keys when it dynamically spawns a section, but its startup preview-validation path does not do that. This is a suspected extra duplicate at runtime, not Play Mode verified.

## Part 1 — Confirmed static findings

### 1. Critical bugs

**AUD-01 — Saved startup track sequence is invalid (Critical, SUSPECTED from scene/source).**  
**Evidence:** E1, E2. `RunnerTrackManager.Start()` collects active `RunnerTrackSection` children of the referenced `RUNNER_TRACK_RUNTIME`, sorts by Z, then requires slots to begin at 0 and be contiguous; on mismatch it logs an error and disables itself (`RunnerTrackManager.cs:51–98`). The current active preview objects are `FINAL_ROUTE_PREVIEW_SLOT_3` at Z=60 with slot 3 (FE_01), `FINAL_ROUTE_PREVIEW_SLOT_4` at Z=80 with slot 4 (FE_02), and `FINAL_ROUTE_PREVIEW_SLOT_5` at Z=100 with slot 5 (FE→CC transition). The old base sections at Z=0, 20, 40 and 60 are inactive. Therefore the first sorted active slot is 3, not 0, and the third preview uses the transition earlier than the sequence logic expects (the FE→CC transition is selected at slot 8). The player is saved at the start near Z=0 while the first active track floor is at Z=60; legacy `Cave_Path` is inactive. **Impact:** likely manager self-disables and leaves a start-area gap, stopping route spawning/progression. This is deterministic from the saved data and code, but was not observed in Play Mode. **Smallest fix:** restore active contiguous startup sections beginning with slot 0 and correct section type/order, or make the manager start from a valid empty container and spawn slot 0; validate in Play Mode before changing other systems.

**AUD-02 — Chest is before the Vault and route end (Critical, SUSPECTED from scene/source).**  
**Evidence:** E1, E2. `FINAL VAULT TREASURE CHEST` is active at `(0,0,336)`. `RunnerTrackManager` defines Level 2 ending at Z=340 and the track ending at Z=500 (`RunnerTrackManager.cs:38–41, 194–200`). The chest is therefore 4m before the Level 3 boundary and 164m before the route end. The chest's interaction can call `WinGame()` after key unlock; the route manager does not make the chest a route-end trigger. **Impact:** if reachable, the player can finish before entering the Vault rather than at the final objective. No runtime interaction was tested. **Smallest fix:** move the existing chest/interaction point to the end of the Level 3 route and verify it is reachable after the Vault transition.

### 2. Gameplay bugs

**AUD-03 — Game Over is unreachable with the saved checkpoint state (High, SUSPECTED from source/config).**  
**Evidence:** E1, E2. The scene serializes `hasCheckpoint: 1`; `GameManager.hasCheckpoint` defaults true and `StartFreshGame()` sets it true (`GameManager.cs:35, 156–160`). Checkpoint progression only sets it true (`:117–124`). At health <= 0, `TakeDamage()` respawns whenever this flag is true and only shows Game Over in the `else` branch (`:290–297`). No code path clears the flag. **Impact:** ordinary lethal damage repeatedly respawns at the checkpoint instead of reaching Game Over. **Smallest fix:** define when a retry/checkpoint is exhausted and clear the flag before the terminal hit, or use the intended health/Game Over rule; preserve a separate deliberate checkpoint retry if desired. Runtime behavior not tested.

**AUD-04 — Possible duplicated embedded key on startup preview (High, SUSPECTED from prefab/source).**  
**Evidence:** E1–E3. The active slot-3 FE_01 preview contains an embedded key. `SpawnNextSection()` deactivates embedded `TreasureKey` children after instantiation (`RunnerTrackManager.cs:146–149`), but `Start()` validates existing preview sections without applying that suppression. If FE_01 preview remains active, it can add a second ID 0 alongside `GoldenKey_ID_0`. **Smallest fix:** apply the same embedded-key suppression to startup previews, or remove/disable that embedded prefab pickup while keeping the four explicit scene pickups. Actual runtime duplicate count not tested.

### 3. Player and camera issues

**AUD-05 — Start-area arch colliders lie in both outer lane centers (High, SUSPECTED from scene geometry).**  
**Evidence:** E1. `Arch_Pillar_Left_01` and `Arch_Pillar_Right_01` are active at `(-3.5,2,8)` and `(3.5,2,8)` with scale `(0.8,4,1.2)` and enabled non-trigger BoxColliders. These X coordinates are exactly the two outer lane centers. The active `Arch_Top_01` spans above them at Z=8. Its colliders may block the outer lanes near the start; collider world bounds depend on the component's local size and have not been tested against the player. **Smallest fix:** if Play Mode confirms a lane collision, make these decorative arch colliders non-blocking or move the arch outside the runner envelope while preserving its appearance. Do not change player/camera until tested.

**AUD-06 — Camera and movement behavior not verified (Low, NOT TESTED).**  
**Evidence:** E2 only. Static scripts define automatic forward motion, 3-lane switching, jump/slide, follow-camera behavior and collision handling. No actual movement, camera clipping/jitter, input sequence or 20-second no-input run was performed. No defect is asserted from source alone. Check the specified lane sequence, jump/slide state reset, camera clearance, restart and checkpoint retry in Play Mode.

### 4. UI and menu issues

**AUD-07 — Two active overlay Canvases share sorting order (Medium, SUSPECTED visual overlap).**  
**Evidence:** E1. The saved scene has two active `Canvas` objects; both use Screen Space Overlay with sorting order 0, plus one EventSystem. This may be intentional separation of menu/HUD layers, but same-order overlays can overlap. No screenshot or click test was captured. **Smallest fix:** inspect in Game view at 1920×1080 and 1280×720; only adjust anchors/order if overlap is visible.

**AUD-08 — UI and copy disagree on 2 versus 4 keys (High, SUSPECTED runtime presentation; static mismatch confirmed).**  
**Evidence:** E1, E2. LostCave_Main serializes `requiredTreasureKeys: 2` and the HUD/final-stat counter reads that field dynamically, so the active scene is configured for `0/2`. The intro text in the scene says “collect all four golden keys” (scene around line 42027); `GameManager.ShowLockedTreasure()` displays “You need all four golden keys…” (`GameManager.cs:303–307`). Consequently, after two keys the scene-level chest predicate allows unlock while the locked message and intro claim four are required. **Smallest fix:** retain the approved two-key design and update all active text/setup validation to two; see AUD-09 for rejected IDs and older tooling. No UI was viewed at runtime.

### 5. Environment and visual issues

**AUD-09 — Actual runtime visual quality not verified (Medium, NOT TESTED).**  
**Evidence:** E1, E4. Region configuration assets are assigned and refer to Forgotten Entrance, Crystal Caverns and Ancient Treasure Vault; the framework covers slots 0–8, 9–17 and 18–24 with transition slots 8 and 17. Existing `audit5.log` validated 20/20 section prefabs. This does not prove those sections spawn or that the actual gameplay camera sees the intended environments. No audit screenshots were found under `Docs`; no Play Mode view was captured. Verify route seams, background coverage, camera obstruction, lighting and objective visibility once route startup is corrected.

**AUD-10 — URP drops many additional-light shadow maps (Medium, REPRODUCED in Editor.log).**  
**Evidence:** E5. The exact warning begins: `Too many additional punctual lights shadows to look good, URP removed [N] shadow maps to make the others fit in the shadow atlas...`; it repeats 175 times in the current Editor log, with reported removals varying (examples: 56, 50, 44, down to 2). No C# compilation or null-reference errors were present in that log. **Impact:** some point/spot lights lose shadows; can increase rendering cost and make lighting inconsistent. **Smallest fix:** reduce the number of shadow-casting additional lights, prefer non-shadow decorative lights, or adjust light type/settings after visually checking the three regions. No lighting changes were made.

### 6. Collectibles and power-up issues

**AUD-11 — Four explicit keys conflict with the two-key acceptance rule (High, SUSPECTED collection outcome; static mismatch confirmed).**  
**Evidence:** E1–E3. Four active, unique scene objects are present at the coordinates in the key inventory. `GameManager.CollectTreasureKey()` rejects IDs `<0` or `>= requiredTreasureKeys` (`GameManager.cs:257–270`); with the scene value 2, IDs 2 and 3 are rejected as out of range. All four are before the Z=160 Level 1 boundary, so the accepted IDs 0 and 1 are the only intended two-key requirement, both in Forgotten Entrance; IDs 2 and 3 cannot advance the HUD. This is inconsistent with both the visible objects and the prior “four keys across stages” configuration. **Smallest fix:** keep exactly two active, reachable unique keys with IDs 0 and 1 and remove/disable IDs 2 and 3 from the active scene; ensure the first preview does not add a duplicate ID 0. Collection/reachability not tested.

**AUD-12 — Purple crystal type/pickup absent (Medium, SUSPECTED requirement gap).**  
**Evidence:** E2/E3. `Collectible.TreasureType` contains only `BlueCrystal`, `GoldCrystal`, and `Coin`; pickup assets in the runner pickup folder include coin, blue crystal and key, with no distinct purple-crystal type/prefab found. If purple crystals remain part of the acceptance requirement, they are not represented by the current typed pickup system. **Smallest fix:** only if purple is a required collectible, reuse the existing collectible behavior with a distinct visual/configuration rather than introducing a new currency system. No pickup was collected.

**AUD-13 — Five power-ups exist in source but runtime placement/effects unverified (Medium, NOT TESTED).**  
**Evidence:** E2. `Collectible.PowerUpType` declares CoinMagnet, Shield, SpeedBoost, ScoreMultiplier and TreasureBonus. This is the five-power-up set; Key Finder is absent. No pickup/effect/duration/restart behavior was tested for this saved scene. Do not infer working effects from enum declarations.

### 7. Level progression issues

**AUD-14 — Runtime stage progression depends on the invalid startup route (Critical, SUSPECTED).**  
**Evidence:** E1/E2/E4. The source boundaries are Forgotten Entrance `<160`, Crystal Caverns `<340`, Ancient Treasure Vault to 500 (`RunnerTrackManager.cs:38–41, 194–200`). Region configs match the intended slot ranges, and transition prefabs passed static section validation, but the saved startup preview fails the manager's contiguous-from-zero requirement (AUD-01). In addition, `RunnerTrackVisuals` / `RunnerWorldAtmosphere` contain legacy fallback boundary values (6/12 slots and Z=380/780); assigned region configuration appears to supersede those fallbacks, so this is a dependency risk rather than a confirmed active defect. **Smallest fix:** fix startup first, then verify one complete transition in Play Mode; update fallback constants only if configuration can be absent or tests show they are used.

### 8. Treasure and victory issues

**AUD-15 — Chest requirement, key wording and route placement disagree (Critical, SUSPECTED runtime outcome).**  
**Evidence:** E1/E2. `TreasureChest` checks `GameManager.HasAllTreasureKeys` and requires E within range (`TreasureChest.cs:28–68`). That predicate uses the scene-overridden value 2, while the message says four. The chest at Z=336 is before Level 3 and the Z=500 route end. Four keys are configured, but only IDs 0 and 1 pass the current range check. Therefore saved configuration cannot satisfy both the stated four-key HUD/story and the two-key requirement. **Smallest fix:** maintain the two-key acceptance rule consistently, use only IDs 0 and 1, correct all messaging, move the chest to the real end of Level 3, then test locked/unlocked E interaction and Victory.

**AUD-16 — Older editor scripts and verifier retain four-key assumptions (Medium, SUSPECTED future regression).**  
**Evidence:** `Assets/Editor/RunnerConversionSetup.cs:498,512`, `Assets/Editor/RunnerAdventureBuilder.cs:468`, `Assets/Editor/RunnerVisualPolishSetup.cs:445`, `Assets/Editor/LostCaveEnvironmentSetup.cs:209`, `Assets/Scripts/M6PlayModeVerifierRunner.cs:90`, `Assets/Scripts/RunnerVisualPolishVerifier.cs:120`. These setup/verification paths still write or demand four keys and some text refers to older stage names. They are not run in this audit, but rerunning them could restore the rejected four-key rule or fail a valid two-key scene. **Smallest fix:** update the active validators/builders to two or clearly prevent obsolete tools from being used; do not run them as-is.

### 9. Console errors and warnings

**AUD-17 — Current saved-state Play Mode Console status unavailable (Low, NOT TESTED).**  
**Evidence:** E5/E6. The Editor log contains 0 `error CS`, 0 `NullReferenceException`, and 0 `MissingReferenceException` matches, plus one `Tundra build success` marker. This is not a fresh compile-only result for the latest saved script/scene, and there was no current Play Mode Console capture. The Editor log shows the 175 URP warnings described in AUD-10. No claim is made that current runtime has no errors.

**AUD-18 — Build is not from current saved state (High, REPRODUCED by timestamps).**  
**Evidence:** E1, E2, E6. The last successful Windows build is `Builds/Win_20261106_231721`; it predates the current scene save (Nov 7 00:03:47) and `TreasureChest.cs` (Nov 6 23:28:40). The build succeeded at that earlier state, but does not verify or include the later changes. Build Settings do place `LostCave_Main.unity` first and enabled; `SampleScene.unity` is also enabled second. **Smallest fix:** after route/key/chest corrections and testing, create a fresh build; disable SampleScene only if it is not intended as a distributable scene. No build was produced during this audit.

### 10. Recommended fixes (ordered; no changes made)

1. **Repair the startup track configuration first.** Ensure active sections begin at slot 0, are contiguous in Z/slot order, and use FE prefabs through slot 7; keep FE→CC transition at slot 8. Confirm a valid floor begins at the player's Z=0 start. Then test track generation and stage progress.
2. **Make the approved two-key rule internally consistent.** Keep IDs 0 and 1; remove/disable IDs 2 and 3 from the active route, suppress any embedded preview key, make the HUD/story/locked text and all active builders/verifiers say 2, and confirm unique collection increments 0/2 → 1/2 → 2/2.
3. **Move the final chest/interaction to the actual end of the Vault route**, not Z=336, and test locked interaction, E unlock, opening and Victory.
4. **Restore a real Game Over path** after the intended checkpoint retries/health are exhausted; test Game Over and Restart reset.
5. **Test start-area arch collision and camera clearance.** Only then change collider behavior or position if the outer lanes are blocked.
6. **Run a continuous Play Mode route** and verify lane changes, jump/slide, all pickups, five power-ups, damage cooldown, stage transitions, HUD, pause/menu, checkpoint, Game Over, chest and Victory.
7. **Address only confirmed presentation issues:** canvas overlap at target resolutions, environment camera views, and URP shadow warnings. Do not tune lighting or rebuild environment until visually evaluated.
8. **Compile and build from the final saved state**, confirm `LostCave_Main` is first enabled scene, and update the delivery report with that build's results.

## Part 2 — Runtime test results

No Play Mode was entered for this audit. The Editor was closed, and this pass used saved files/logs only. All items below are **NOT TESTED** as runtime behavior; static observations are listed as context, not as passes.

| # | Runtime check | Result | Evidence / reason |
|---:|---|---|---|
| 1 | Main menu layout, buttons, Start, nothing runs before Start, Exit | NOT TESTED | No Play Mode UI interaction. Serialized references exist, but click behavior is unverified. |
| 2 | Player start center, 20-second drift, Restart/checkpoint start | NOT TESTED | Saved player start is near `(0,1,0)`; drift/restart not run. |
| 3 | Left/left/right/right/right/right/left lane sequence and lane centers | NOT TESTED | Scripted lane width is 3.5; no input sequence executed. |
| 4 | Jump/slide arc, collider changes, lane changes mid-action, stuck states | NOT TESTED | No Play Mode movement test. |
| 5 | Auto-run speed/ramp and camera follow/jitter/clipping | NOT TESTED | No gameplay-camera run or frame observation. |
| 6 | Coin, blue/purple crystal, key collection in all lanes; hidden/unreachable items | NOT TESTED | Four explicit keys statically found; no trigger overlap tested. No distinct purple pickup type found. |
| 7 | Chest after 0, 1, 2 keys and correct locked/unlocked message | NOT TESTED | Scene requires 2; message says 4; source predicate observed but no interactions performed. |
| 8 | Obstacles, damage cooldown/invulnerability, fairness/safe lanes | NOT TESTED | Collider and code inspection only; no obstacle course played. |
| 9 | Five power-ups, effect duration/reset/visual distinction | NOT TESTED | Five enum values exist; no pickup effects tested. |
| 10 | HUD health/score/coins/crystals/keys/progress/region text | NOT TESTED | Scene/source references inspected; no live HUD observed. |
| 11 | Three regions/transitions, atmosphere blending, gaps/flashes, timing | NOT TESTED | Region config and prefab validation exist; no continuous route run. |
| 12 | Chest at route end, approach visibility, interaction, animation, victory | NOT TESTED | Saved transform is before Level 3; no approach or interaction run. |
| 13 | Victory/Game Over/Pause/Restart/Home screens, counts, overlaps/resets | NOT TESTED | UI references are serialized; no button or state testing. Game Over code path is suspect (AUD-03). |
| 14 | Intro, checkpoints and Retry behavior | NOT TESTED | Static checkpoint code inspected; no checkpoint/retry observed. |

## What could not be tested

- Any Play Mode behavior, including menu flow, movement, camera, collisions, pickup triggers, key counting, power-up effects, stage transitions, chest interaction, Victory, Game Over, Restart, Pause and checkpoints.
- Actual camera screenshots for the three regions/transitions or the two UI resolutions (1920×1080 and 1280×720). No audit screenshots were created.
- Current Play Mode Console and live missing-reference state.
- A fresh compilation or Windows build after the latest scene/script changes. The previous successful build is stale.
- Whether the route-manager startup failure, embedded duplicate key and arch collider interference manifest exactly as inferred at runtime.


