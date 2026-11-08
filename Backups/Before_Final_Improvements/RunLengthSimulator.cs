using UnityEditor;
using UnityEngine;

public static class RunLengthSimulator
{
    [MenuItem("Tools/SimulateRunLength")]
    public static void Run()
    {
        int slots = 25;
        float sectionLen = 20f;
        float trackLen = slots * sectionLen;
        // speed ramp 8 -> 15 over track
        float time = 0f;
        float z = 0f;
        float dt = 1f;
        while (z < trackLen)
        {
            float speed = Mathf.Clamp(8f + z / 500f, 8f, 15f);
            z += speed * dt;
            time += dt;
        }
        Debug.Log("RunLengthSimulator: slots=" + slots + " trackLen=" + trackLen + " estimatedSeconds=" + time + " minutes=" + (time/60f));
    }
}

