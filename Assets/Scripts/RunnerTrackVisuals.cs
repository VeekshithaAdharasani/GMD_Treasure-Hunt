using UnityEngine;

[DisallowMultipleComponent]
public sealed class RunnerTrackVisuals : MonoBehaviour
{
    [Header("Saved stage decoration prefabs")]
    public GameObject forgottenEntranceDecoration;
    public GameObject crystalCavernsDecoration;
    public GameObject ancientRuinsDecoration; // Preserved scene reference for rollback.
    public GameObject treasureVaultDecoration;

    [Header("Saved stage floor materials")]
    public Material forgottenEntranceFloor;
    public Material crystalCavernsFloor;
    public Material ancientRuinsFloor;
    public Material treasureVaultFloor;
    public Material rockObstacleMaterial;
    public Material slideObstacleMaterial;

    [Header("Shared region framework")]
    public RunnerRegionConfig[] regionConfigs;
    public int variationSeed = 54017;

    public void RefreshSection(RunnerTrackSection section)
    {
        if (section == null) return;
        RunnerRegionConfig region = ResolveRegion(section.sequenceSlot);
        int stage = section.sequenceSlot < 20 ? 0 : section.sequenceSlot < 41 ? 1 : 2;
        Material floorMaterial = region != null && region.floorMaterial != null
            ? region.floorMaterial
            : stage == 0 ? forgottenEntranceFloor : stage == 1 ? crystalCavernsFloor : treasureVaultFloor;
        Transform floor = section.transform.Find("TRACK FLOOR");
        Renderer floorRenderer = floor != null ? floor.GetComponent<Renderer>() : null;
        if (floorRenderer != null && floorMaterial != null) floorRenderer.sharedMaterial = floorMaterial;

        foreach (RunnerLaneHazard hazard in section.GetComponentsInChildren<RunnerLaneHazard>(true))
        {
            Material material = hazard.action == RunnerHazardAction.Slide ? slideObstacleMaterial : rockObstacleMaterial;
            if (material == null) continue;
            foreach (Renderer renderer in hazard.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = material;
        }

        GameObject fallbackPrefab = stage == 0 ? forgottenEntranceDecoration : stage == 1 ? crystalCavernsDecoration : treasureVaultDecoration;
        Transform existing = RunnerEnvironmentAssembler.EnsureDecoration(section, region, fallbackPrefab, variationSeed);
        if (existing == null) return;

        if (stage == 0) NormalizeForgottenEntrancePath(existing);

        Transform gate = existing.Find("STAGE_GATE");
        if (gate != null)
            gate.gameObject.SetActive(section.sequenceSlot == 0 || section.sequenceSlot == 20 || section.sequenceSlot == 41);

        if (stage == 2 && section.sequenceSlot == 59 && existing.Find("FINAL_CHEST_DAIS") == null)
        {
            foreach (RunnerLaneHazard hazard in section.GetComponentsInChildren<RunnerLaneHazard>(true))
            {
                float localZ = section.transform.InverseTransformPoint(hazard.transform.position).z;
                if (hazard.laneIndex == -1 && localZ > 14f && localZ < 18f)
                    hazard.gameObject.SetActive(false);
            }
            GameObject dais = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dais.name = "FINAL_CHEST_DAIS";
            dais.transform.SetParent(existing, false);
            dais.transform.localPosition = new Vector3(0f, 0.10f, 16f);
            dais.transform.localScale = new Vector3(3.6f, 0.10f, 3.6f);
            Collider collider = dais.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }
            Renderer renderer = dais.GetComponent<Renderer>();
            if (renderer != null && slideObstacleMaterial != null) renderer.sharedMaterial = slideObstacleMaterial;
        }
    }

    private static void NormalizeForgottenEntrancePath(Transform decoration)
    {
        foreach (Transform child in decoration.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "TRACK EDGE STONE")
            {
                Vector3 position = child.localPosition;
                position.x = Mathf.Sign(position.x) * 5.15f;
                child.localPosition = position;
            }
            else if (child.name == "LANE INLAY")
            {
                Vector3 position = child.localPosition;
                position.x = Mathf.Sign(position.x) * 1.75f;
                child.localPosition = position;
            }
            else if (child.name == "LAYERED_FOLIAGE")
            {
                child.localScale = Vector3.one * 0.48f;
            }
            else if (child.name == "ROOTS_AND_VINES")
            {
                child.localScale = Vector3.one * 0.68f;
            }
        }
    }

    private RunnerRegionConfig ResolveRegion(int sequenceSlot)
    {
        if (regionConfigs == null) return null;
        for (int i = 0; i < regionConfigs.Length; i++)
        {
            RunnerRegionConfig candidate = regionConfigs[i];
            if (candidate != null && candidate.ContainsVisualSlot(sequenceSlot)) return candidate;
        }

        return null;
    }
}
