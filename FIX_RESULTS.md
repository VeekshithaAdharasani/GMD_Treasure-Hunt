# Fix Pass Results — 2026-11-07

Scope: static/source corrections and guarded scene setup for the current two-key, three-level runner. No Play Mode session was run in this pass. Every row is therefore UNVERIFIED in gameplay unless a prior log or timestamp is explicitly identified.

| Audit | Finding status | Change and evidence | Verification |
|---|---|---|---|
| AUD-01 | NOT REPRODUCED in Play Mode; invalid saved startup order was confirmed from scene data | Activated the four connected startup instances at slots 0–3, kept their existing prefabs/anchors, and let the manager continue from slot 4. | Guarded setup validated prefab identity, section geometry, slot order and seams, saved, then reopened the scene. Runtime spawning is UNVERIFIED. |
| AUD-02 | NOT REPRODUCED as “chest before the Vault”; local/world position confusion confirmed | Chest was local Z=336 under objective parent world Z=159, so its saved world Z was 495. The existing parent was translated 5 units so the chest is at route end Z=500. | Chest trigger, lid, particles, light and saved world position validated after scene reload. Reachability in Play Mode is UNVERIFIED. |
| AUD-03 | NOT REPRODUCED in Play Mode; unlimited retry path was confirmed in source | Added a serialized limit of two checkpoint respawns, then Game Over after retries are exhausted; restart resets retries. | Code compiled; death/retry/Game Over/Restart behavior is UNVERIFIED. |
| AUD-04 | NOT REPRODUCED in Play Mode; duplicate pickups confirmed in saved scene | Startup and dynamically spawned section keys are suppressed. Four root preview pickups (two ID 0 at Z=60, two ID 1 at Z=200) are disabled, not deleted. | Reopened scene contains only the intended active IDs 0 and 1. Runtime duplicate visibility/collection is UNVERIFIED. |
| AUD-05 | NOT REPRODUCED | Start arch colliders were left unchanged because no Play Mode collision was observed. | Lane collision behavior UNVERIFIED. |
| AUD-06 | NOT REPRODUCED | Camera and player controls were left unchanged. | Camera follow, lane movement, jump, slide and drift checks UNVERIFIED. |
| AUD-07 | NOT REPRODUCED | Canvas sorting/layout was left unchanged. | 1920x1080 and 1280x720 overlap checks UNVERIFIED. |
| AUD-08 | NOT REPRODUCED in Play Mode; static 2-versus-4 wording mismatch confirmed | Main-scene intro text now says both keys; GameManager locked text is dynamic from requiredTreasureKeys; alternate working scene requirement is 2. Saved main-scene scan found no old four-key intro or /4 counter copy. | Scene data check passed. HUD and chest message behavior UNVERIFIED. |
| AUD-09 | NOT REPRODUCED | No visual changes were made in this pass. | Gameplay-camera environment quality and screenshots UNVERIFIED. |
| AUD-10 | REPRODUCED in a prior Editor.log; not retested here | No lighting/shadow changes were made without a visual test. | Shadow-atlas warning rate and visual impact remain UNVERIFIED. |
| AUD-11 | NOT REPRODUCED in Play Mode; four explicit IDs were confirmed in saved scene | requiredTreasureKeys is 2. ID 0 is active at (0, 1, 70); ID 1 is active at (0, 1, 210); IDs 2 and 3 are inactive. | IDs, positions, enabled triggers/renderers and two-key scene state validated after reload. Collection/HUD progression UNVERIFIED. |
| AUD-12 | NOT REPRODUCED in Play Mode; purple variant absence confirmed in source | Added a purple Crystal variant using the existing BlueCrystal score/counter behavior and existing purple material; placed two pickups at X=±3.5, Z=210. | Collider, collectible configuration and renderer validated after reload. Visual appearance and collection UNVERIFIED. |
| AUD-13 | NOT REPRODUCED | Existing five power-ups were preserved and untouched. | Pickup placement, effect duration, reset and collection remain UNVERIFIED in this pass. |
| AUD-14 | NOT REPRODUCED in Play Mode; boundary/config mismatch confirmed in source | Runner boundaries/checkpoints now match the 25-slot sequence at Z=180 and Z=360; transition/catalog references were validated. | Serialized setup and references passed guarded validation. Continuous transitions are UNVERIFIED. |
| AUD-15 | NOT REPRODUCED in Play Mode | Chest still uses GameManager’s same unique-key requirement, now 2; chest is at Z=500 inside the final route end. | References, trigger and final position validated after reload. Lock-at-0/1, unlock-at-2, opening and Victory are UNVERIFIED. |
| AUD-16 | NOT REPRODUCED | RunnerConversionSetup result text and RunnerVisualPolishVerifier two-key wording were corrected; active alternate scene requirement set to 2. | Old M6 verifier and ThreeAreaGameSetup remain legacy five-stage/free-roam tools and were not run; invoking them is not part of this verification. |
| AUD-17 | NOT REPRODUCED in Play Mode | Final Unity batch compile completed with Tundra success, exit code 0, zero C# errors and zero C# warnings. | Play Mode Console status remains UNVERIFIED. Unity logged a non-blocking licensing access-token message and Curl error 42. |
| AUD-18 | REPRODUCED by saved timestamps | No build was produced because full Play Mode acceptance was unavailable. | Existing latest build is Builds/Win_20261106_231721/The Lost Cave.exe from 2026-11-06; it predates the 2026-11-07 scene changes and is not a build of this fixed state. |

## Saved-state validation

- Unity 6000.3.24f1 compile: exit 0, Tundra build success; 0 C# errors and 0 C# warnings.
- Guarded setup: exit 0; AUDIT_FIX_PASS_SCENE_SAVED emitted after save and independent scene reload.
- Startup slots 0–3, route seams, explicit key IDs, key renderers/triggers, chest references/trigger, purple pickup references, and Build Settings order were validated by the setup.
- LostCave_Main is the first enabled Build Settings scene; SampleScene is disabled.
- LostCave_Main SHA256: 3AA31ABF7E726CCC5CD6A76F22093E4178F851401F4F8102AFEDFA31973C90BA
- EditorBuildSettings.asset SHA256: 3D8109DD2B93D00F0EE19ACA0FC0BEFCC635AF7D31C2465E8D30E9567C76CECB
- No Play Mode, full continuous run, screenshots, performance/FPS measurements, or fresh Windows build were performed.

Manual acceptance still needed: start/menu flow; movement/camera; obstacle damage and bounded retries; key and crystal collection; transitions and region visibility; power-ups; chest lock/unlock and Victory; Restart/Home/Pause; Console; screenshots; and an updated Windows build.