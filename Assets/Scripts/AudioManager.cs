using UnityEngine;

public static class AudioManager
{
    private static AudioSource source;
    private static readonly System.Collections.Generic.Dictionary<string, AudioClip> cache =
        new System.Collections.Generic.Dictionary<string, AudioClip>();

    private static AudioSource Source
    {
        get
        {
            if (source == null)
            {
                GameObject go = GameObject.Find("AudioManager");
                if (go == null) go = new GameObject("AudioManager");
                source = go.GetComponent<AudioSource>();
                if (source == null) source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                Object.DontDestroyOnLoad(go);
            }
            return source;
        }
    }

    public static void Play(string clipName, float volume = 1f)
    {
        if (string.IsNullOrEmpty(clipName)) return;
        AudioClip clip = Load(clipName);
        if (clip == null) return;
        Source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    public static void PlayAt(string clipName, Vector3 position, float volume = 1f)
    {
        if (string.IsNullOrEmpty(clipName)) return;
        AudioClip clip = Load(clipName);
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, Mathf.Clamp01(volume));
    }

    private static AudioClip Load(string clipName)
    {
        if (cache.TryGetValue(clipName, out AudioClip cached)) return cached;
        AudioClip clip = Resources.Load<AudioClip>("Audio/" + clipName);
        cache[clipName] = clip;
        return clip;
    }
}