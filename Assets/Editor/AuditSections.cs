using UnityEditor;
using UnityEngine;

public static class AuditSections
{
    [MenuItem("Tools/AuditSections")]
    public static void Run()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[]{"Assets/Prefabs/Runner/Sections"});
        int pass=0, fail=0;
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var contents = PrefabUtility.LoadPrefabContents(path);
            var section = contents.GetComponent<RunnerTrackSection>();
            if (section == null) { PrefabUtility.UnloadPrefabContents(contents); continue; }
            bool ok = true; string reason="";
            if (section.entryAnchor == null || section.exitAnchor == null) { ok=false; reason="missing anchor ref"; }
            else
            {
                if (Vector3.Distance(section.entryAnchor.localPosition, Vector3.zero) > 0.001f) { ok=false; reason="entry anchor not at 0"; }
                if (Vector3.Distance(section.exitAnchor.localPosition, new Vector3(0,0,section.sectionLength)) > 0.001f) { ok=false; reason="exit anchor not at length"; }
                if (Vector3.Distance(contents.transform.localScale, Vector3.one) > 0.001f) { ok=false; reason="root scale not identity"; }
                if (Quaternion.Angle(contents.transform.localRotation, Quaternion.identity) > 0.01f) { ok=false; reason="root rotation not identity"; }
            }
            if (ok) { pass++; Debug.Log("PASS: " + path); }
            else { fail++; Debug.LogError("FAIL: " + path + " - " + reason); }
            PrefabUtility.UnloadPrefabContents(contents);
        }
        Debug.Log("AuditSections: pass=" + pass + " fail=" + fail);
    }
}