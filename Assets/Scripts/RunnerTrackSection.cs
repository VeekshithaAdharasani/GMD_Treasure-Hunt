using System.Linq;
using UnityEngine;

public enum RunnerBiome
{
    ForgottenEntrance,
    CrystalCaverns,
    AncientRuins,
    LostTemple,
    FinalTreasureVault,
    TransitionToCrystalCaverns,
    TransitionToAncientRuins,
    TransitionToLostTemple,
    TransitionToFinalTreasureVault,
    TransitionToForgottenEntrance
}

[DisallowMultipleComponent]
public sealed class RunnerTrackSection : MonoBehaviour
{
    public RunnerBiome biome;
    [Min(1f)] public float sectionLength = 20f;
    public int sequenceSlot;
    public Transform entryAnchor;
    public Transform exitAnchor;

    public float StartZ => entryAnchor != null ? entryAnchor.position.z : transform.position.z;
    public float EndZ => exitAnchor != null ? exitAnchor.position.z : transform.position.z + sectionLength;

    public bool Validate(float laneSpacing, out string error)
    {
        if (sectionLength <= 0f) { error = name + ": section length must be positive."; return false; }
        if (entryAnchor == null || exitAnchor == null) { error = name + ": entry and exit anchors are required."; return false; }
        if (Vector3.Distance(entryAnchor.localPosition, Vector3.zero) > 0.001f ||
            Vector3.Distance(exitAnchor.localPosition, new Vector3(0f, 0f, sectionLength)) > 0.001f)
        { error = name + ": anchors do not match the standard 0-to-length connection points."; return false; }
        if (Vector3.Distance(transform.localScale, Vector3.one) > 0.001f || Quaternion.Angle(transform.localRotation, Quaternion.identity) > 0.01f)
        { error = name + ": section root scale and rotation must be identity."; return false; }
        Transform floorTransform = transform.Find("TRACK FLOOR");
        BoxCollider floor = floorTransform != null ? floorTransform.GetComponent<BoxCollider>() : null;
        if (floor == null) { error = name + ": TRACK FLOOR BoxCollider is missing."; return false; }
        Vector3 floorSize = Vector3.Scale(floor.size, floor.transform.lossyScale);
        if (floorSize.x < laneSpacing * 2f + 2f || floorSize.z < sectionLength - 0.01f)
        { error = name + ": floor is too narrow or shorter than the declared section length."; return false; }
        RunnerLaneHazard[] hazards = GetComponentsInChildren<RunnerLaneHazard>(true);
        foreach (RunnerLaneHazard hazard in hazards)
        {
            if (hazard.laneIndex < -1 || hazard.laneIndex > 1)
            { error = name + ": obstacle has an invalid lane index."; return false; }
            Obstacle damage = hazard.GetComponent<Obstacle>() ?? hazard.GetComponentInChildren<Obstacle>(true) ?? hazard.GetComponentInParent<Obstacle>();
            bool hasCollider = hazard.GetComponent<Collider>() != null || hazard.GetComponentInChildren<Collider>(true) != null ||
                               (damage != null && (damage.GetComponent<Collider>() != null || damage.GetComponentInChildren<Collider>(true) != null));
            bool hasDamage = damage != null;
            if (!hasCollider || !hasDamage)
            { error = name + ": marked obstacle is missing its collider or damage component."; return false; }
        }
        foreach (var row in hazards.GroupBy(h => h.patternRow))
        {
            int occupiedLanes = row.Select(h => h.laneIndex).Distinct().Count();
            if (occupiedLanes >= 3)
            { error = name + ": obstacle row " + row.Key + " blocks all three lanes."; return false; }
        }
        error = null;
        return true;
    }
}
