# Keys, Track and Collectibles Fix Report

## Existing gameplay changes reviewed

The targeted gameplay changes from the previous pass are listed in the earlier sections of this report: two authored keys (IDs 0 and 1), bright key visuals and pickup burst, three-lane coin trail scheduling, power-up scheduling, and Level 1 lane/width adjustments. These notes describe the earlier pass; follow-up Unity status and Play Mode evidence are recorded below.

## Git dependency repair

The four manifest Git dependencies all failed with the same Unity Package Manager Git error:

```text
Error when executing git command. error: daemon terminated
fatal: unable to access '<repository URL>/': getaddrinfo() thread failed to start
```

Affected manifest entries and pinned lock revisions:

| Package | Original Git reference | Lock hash | Cached package version |
|---|---|---|---|
| `com.alexeyperov.unity-open-mcp-bridge` | `https://github.com/AlexeyPerov/unity-open-mcp.git?path=packages/bridge#bridge-v1.6.0` | `51defe28353b119c9b5722260e0314b0b5186303` | 1.6.0 |
| `com.alexeyperov.unity-open-mcp-verify` | `https://github.com/AlexeyPerov/unity-open-mcp.git?path=packages/verify#verify-v1.6.0` | `51defe28353b119c9b5722260e0314b0b5186303` | 1.6.0 |
| `com.besty.unity-skills` | `https://github.com/Besty0728/Unity-Skills.git?path=/SkillsForUnity` | `8fe4ad601457b7b30c5b8c0878831476d6b7a86e` | 2.9.1 |
| `com.gamelovers.mcp-unity` | `https://github.com/CoderGamester/mcp-unity.git` | `d572dc88f1b982a0fdfc09cd14cfdfd760b658e8` | 1.5.0 |

To avoid network Git resolution, `Packages/manifest.json` now points only these four entries to workspace copies under `Packages/LocalPackages/`. Unity rewrote their lock `version` values to the local file paths. Their package.json versions match the cached versions above. No other manifest dependency entries were changed. Original manifest and lock backups are in `.DependencyBackup_20261107/`.

## Unity and gameplay verification

- Unity 6.3 LTS (6000.3.24f1) was launched for `D:\Desktop\TreasureHunt` and the `Assets/Scenes/LostCave_Main.unity` scene opened.
- Unity 6.3 LTS (6000.3.24f1) loaded `LostCave_Main` and entered Play Mode after its import/shader work settled. The run proceeded through Forgotten Entrance, Crystal Caverns, and Ancient Treasure Vault, reaching 99% of the final region before the run was stopped.
- The HUD read `0/2` in Crystal Caverns and `1/2` in Ancient Treasure Vault through the final stage. It never reached `2/2`; the on-screen prompt still asked for one golden key. The final treasure therefore remained locked. Runtime evidence confirms the two-key objective is still failing; this pass did not establish which key ID was missed.
- Score multiplier and Speed Boost activation messages were observed. Coin pickups/trails and the remaining Coin Magnet, Shield and Treasure Bonus effects were not confirmed. Crystal count reached 77, so the intended coin/crystal balance is not established.
- Level 1 was traversed without a fall, and the game transitioned through all three region titles. The Forgotten Entrance view was heavily blocked by foliage, and the changing sections were not inspected closely enough in Play Mode to verify consistent width. The later visible track looked aligned, but this is not proof of every Level 1 segment.
- The chest-unlock condition, complete coin distribution, all five power-up effects, key pickup effect visibility, HUD progression to 2/2, and full Level 1 track width remain unverified or failed as stated above.
- A Unity status message during import read `MCP Unity Failed to start WebSocket server: Port 8090 is already in use`. Unity proceeded to Play Mode despite this, but the port conflict remains an editor-side issue.

## Earlier verification status (superseded by follow-up below)

At the time of the earlier run, only one key had registered and the chest remained locked. The follow-up below identifies the startup relocation and duplicate-key selection defects and records the current test limitation. Coins, power-ups, foliage and full Level 1 width still need the separate verification described above.

## Key 1 follow-up diagnosis (2026-11-07)

- Runtime tracing found the main route bug in `RunnerTrackManager.EnsureRequiredKeys()`: it was moving the serialized keys at `(0, 1.2, 60)` and `(0, 1.2, 200)` to Z=100 and Z=600. That placed Key 1 in the final region instead of Crystal Caverns, and placed Key 0 on the left lane. The forced movement and ID reassignment are removed.
- The manager also found four `TreasureKey` components under `RUNNER_GAMEPLAY_PICKUPS`, with duplicate IDs. Choosing the first match was nondeterministic. Selection now accepts only ID 0 at authored Z=60 and ID 1 at authored Z=200; other legacy keys are disabled. Both authored objects remain under the non-recyclable pickup owner and retain their saved coordinates and IDs.
- Both authored keys have equally configured point lights; pickup code emits the sparkle burst, disables the collider and removes the collected key only after `GameManager.CollectTreasureKey()` accepts its ID.
- A diagnostic run reached the final chest with `collected=0` after Unity recompiled scripts while Play Mode was already running. The hot reload reset `GameManager.Instance`; the log later recorded `gameManagerPresent=False`. That run is invalid for judging normal collection and does not establish the HUD/chest result.
- Unity 6.3 LTS successfully compiled `Assembly-CSharp.dll` after the targeted edits. A clean Play Mode run with the final assembly, HUD progression, and chest unlock was not completed in this pass. Do not treat key pickup or final unlock as verified. Remaining verification is to start a fresh run after compilation settles and confirm `0/2 → 1/2 → 2/2`, plus chest unlock.

### Files changed in the Key 1 follow-up

- `Assets/Scripts/RunnerTrackManager.cs` — stopped relocating/reassigning authored keys; restricted the two required IDs to their authored Z locations and disables legacy/duplicate keys.
- `Assets/Scripts/TreasureKey.cs` — retained trigger and bounds-based pickup handling; added a one-time diagnostic when the player passes without collecting.
- `Assets/Scenes/LostCave_Main.unity` — keeps the authored keys at ID 0 `(0, 1.2, 60)` and ID 1 `(0, 1.2, 200)`, with matching gold point lights assigned.
- `KEYS_TRACK_COLLECTIBLES_FIX_REPORT.md` — this report.

### Test results for the follow-up

| Check | Result |
|---|---|
| Unity editor opened the project | Passed (Unity 6.3 LTS, 6000.3.24f1) |
| Final scripts compiled | Passed (`Assembly-CSharp.dll` build success in Editor.log) |
| Authored IDs and positions | Static scene check passed; unchanged IDs and coordinates listed above |
| Clean Play Mode pickup/HUD/chest sequence | Not verified. Compilation/domain reload occurred during an active run and reset the `GameManager.Instance` singleton; that run is invalid. A clean restart and end-to-end run remain required. |

## Console warnings and key visual follow-up (2026-11-07)

### Exact sources and fixes

- **Particle duration warning:** The project-owned write was `Assets/Scripts/TreasureKey.cs` in `TreasureKey.Awake()`: the fallback path added a `ParticleSystem` and then wrote `main.duration = 0.65f` while Unity could already be playing the new system. The code now calls `Stop(true, StopEmittingAndClear)` before changing duration, sets `playOnAwake = false`, and stops/clears the system before continuing setup. This prevents changing duration on a live particle system.
- **Material/submesh warning:** Unity's stack ends in URP's `InstanceCullingBatcherBurst.ProcessRenderer` → `InstanceCullingBatcher.BuildBatch` → `GPUResidentBatcher.UpdateRendererInstancesAndBatches` → `GPUResidentDrawer.PostPostLateUpdateStatic`; it does not include a project script or GameObject name. A targeted scene/prefab renderer audit found nine offending renderers in these three Forgotten Entrance decoration prefabs and six direct preview-scene renderers in `LostCave_Main.unity`. In all cases, the mesh had one submesh and the renderer had two shared materials. The correction keeps the first/valid material on each one-submesh renderer; the extra material had no submesh to render. The corrected meshes are `AncientCaveEntrance_Architecture`, `AncientCaveEntrance_RootsAndVines`, and `AncientCaveEntrance_MossAndRubble` in `RunnerDecoration_AncientCaveEntrance.prefab`, `RunnerDecoration_ForgottenGateway.prefab`, and `RunnerDecoration_LostJunglePath.prefab`, plus their six direct preview copies in the scene.
- **Key appearance:** `Runner_TreasureKey.prefab`'s `Key Bow` used a sphere mesh, so it read as a cylinder/primitive rather than a key bow. It now uses `Key_Ring`. Both authored scene keys also have the missing ring/bow mesh and visual-child scale compensation for their parent's vertical squash. `_EMISSION` is enabled on `M_Collectible_KeyGold`; the existing gold base and emission colors are retained. The IDs, root positions, colliders, and pickup registration code were not changed in this follow-up.

### Unity checks

- Unity 6.3 LTS (6000.3.24f1) opened the project, reloaded the edited scene, completed domain reload/script compilation, and entered Play Mode. Editor.log contains a successful Tundra build and no C# compilation errors.
- A fresh Play Mode run began at **0/2**. Key 0 registered and the HUD reached **1/2**. The run transitioned from Forgotten Entrance to Crystal Caverns, where the HUD remained **1/2**. The run did not reach **2/2** or unlock the final chest.
- At runtime, `TreasureKey_1` was present in the hierarchy with key ID 1, an enabled trigger collider, `Golden Key Visual`, `Key Collection Sparkles`, and `GoldenKey_ID_1_Glow`. The earlier runtime diagnostic captured its authored world position `(0, 1.2, 200)`, active state, non-recyclable ownership, and valid `GameManager` registration. That run recorded the player passing at X=3.50 while the key was at X=0, with no collider-bounds overlap. This supports a lane miss as the observed cause; it does not indicate that the key was disabled or rejected by the manager. The final run still did not prove Key 1 collection, so the end-to-end two-key/chest result remains unverified and unresolved.
- The target particle-duration and material/submesh messages were not visible in the final Play Mode Console after the changes. An unrelated URP warning about additional punctual-light shadow maps remains.
- The Forgotten Entrance view remains heavily obscured by foliage. The full route, all three Level 1 widths, all power-up effects, and the final chest were not fully tested in this console/key-focused follow-up.

### Files changed in this follow-up

- `Assets/Scripts/TreasureKey.cs`
- `Assets/Prefabs/Runner/Pickups/Runner_TreasureKey.prefab`
- `Assets/VisualPolish/M_Collectible_KeyGold.mat`
- `Assets/VisualPolish/Key_Ring.asset` was used as an existing mesh; its mesh data was not changed.
- `Assets/VisualPolish/ForgottenEntrance/RunnerDecoration_AncientCaveEntrance.prefab`
- `Assets/VisualPolish/ForgottenEntrance/RunnerDecoration_ForgottenGateway.prefab`
- `Assets/VisualPolish/ForgottenEntrance/RunnerDecoration_LostJunglePath.prefab`
- `Assets/Scenes/LostCave_Main.unity`
- This report (backed up at `Backups/Before_ConsoleWarnings_20261107/KEYS_TRACK_COLLECTIBLES_FIX_REPORT.md` before appending this section).
