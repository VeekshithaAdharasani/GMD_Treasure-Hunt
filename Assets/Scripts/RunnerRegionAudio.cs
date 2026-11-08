using UnityEngine;

public class RunnerRegionAudio : MonoBehaviour
{
    public AudioSource ambienceSource;
    public AudioSource musicSource;
    private string currentRegion;

    private void Awake()
    {
        if (ambienceSource == null) ambienceSource = gameObject.AddComponent<AudioSource>();
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        ambienceSource.loop = true;
        musicSource.loop = true;
        ambienceSource.volume = 0.25f;
        musicSource.volume = 0.18f;
        ambienceSource.playOnAwake = false;
        musicSource.playOnAwake = false;
    }

    private void Update()
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player == null) return;
        float z = player.transform.position.z;
        string region = z < 160f ? "forest" : z < 340f ? "cave" : "vault";
        if (region == currentRegion) return;
        currentRegion = region;
        string amb = region == "forest" ? "amb_forest" : region == "cave" ? "amb_cave" : "amb_vault";
        string mus = region == "forest" ? "mus_forest" : region == "cave" ? "mus_cave" : "mus_vault";
        AudioClip ac = Resources.Load<AudioClip>("Audio/" + amb);
        AudioClip mc = Resources.Load<AudioClip>("Audio/" + mus);
        if (ac != null) { ambienceSource.clip = ac; ambienceSource.Play(); }
        if (mc != null) { musicSource.clip = mc; musicSource.Play(); }
    }
}