using UnityEngine;

public enum RunnerHazardAction
{
    Dodge,
    Jump,
    Slide
}

[DisallowMultipleComponent]
public sealed class RunnerLaneHazard : MonoBehaviour
{
    [Range(-1, 1)] public int laneIndex;
    [Min(0)] public int patternRow;
    public RunnerHazardAction action = RunnerHazardAction.Dodge;
}