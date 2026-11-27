using System.Collections.Generic;
using UnityEngine;

// Sons e música. Arquivos em Assets/Resources/Sons/<nome>.wav
public static class Sfx
{
    static AudioSource music;
    static AudioSource[] voices;
    static int nextVoice;
    static string currentMusic;
    static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    static readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();

    public static float MusicVolume { get; private set; } = 0.5f;
    public static float SfxVolume { get; private set; } = 0.8f;

    public static void Init(GameObject host)
    {
        if (music != null) return;
        MusicVolume = PlayerPrefs.GetFloat("volMusica", 0.5f);
        SfxVolume = PlayerPrefs.GetFloat("volEfeitos", 0.8f);
        music = host.AddComponent<AudioSource>();
        music.loop = true;
        music.playOnAwake = false;
        voices = new AudioSource[12];
        for (int i = 0; i < voices.Length; i++)
        {
            voices[i] = host.AddComponent<AudioSource>();
            voices[i].playOnAwake = false;
        }
        music.volume = MusicVolume;
    }

    static AudioClip Clip(string name)
    {
        if (!clips.TryGetValue(name, out var c))
        {
            c = Resources.Load<AudioClip>("Sons/" + name);
            clips[name] = c;
        }
        return c;
    }

    // Toca um efeito. minGap evita repetir o mesmo som muitas vezes no mesmo instante.
    public static void Play(string name, float volume = 1f, float pitchJitter = 0.06f, float minGap = 0.06f)
    {
        if (voices == null || SfxVolume <= 0f) return;
        if (lastPlayed.TryGetValue(name, out float t) && Time.unscaledTime - t < minGap) return;
        var clip = Clip(name);
        if (clip == null) return;
        lastPlayed[name] = Time.unscaledTime;
        var v = voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Length;
        v.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        v.PlayOneShot(clip, volume * SfxVolume);
    }

    public static void Music(string name)
    {
        if (music == null || currentMusic == name) return;
        currentMusic = name;
        var clip = Clip(name);
        if (clip == null) return;
        music.clip = clip;
        music.Play();
    }

    public static void SetVolumes(float musicVol, float sfxVol)
    {
        MusicVolume = musicVol;
        SfxVolume = sfxVol;
        if (music != null) music.volume = musicVol;
        PlayerPrefs.SetFloat("volMusica", musicVol);
        PlayerPrefs.SetFloat("volEfeitos", sfxVol);
    }
}
