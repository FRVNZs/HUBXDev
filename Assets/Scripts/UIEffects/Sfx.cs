using UnityEngine;

/// <summary> анал UI-звука. Click Ч главный, режет Hover; Hover не трогает играющий Click.</summary>
public enum SfxChannel { Click, Hover }

/// <summary>
/// ≈диный проигрыватель UI-звуков. ƒ¬ј AudioSource (клик / ховер), чтобы задать приоритет:
///  - Play(Click): останавливает ховер-канал (клик –≈∆≈“ ховер) и стартует на своЄм канале
///    (новый клик режет предыдущий клик Ч наложени€ кликов нет).
///  - Play(Hover): если клик ещЄ звучит Ч Ќ≈ играет (ховер не глушит клик); иначе стартует
///    на своЄм канале (новый ховер режет предыдущий ховер).
/// “ак клик всегда главный, а быстрый перелЄт мышью не даЄт Ђкашиї из наложенных тонов.
/// </summary>
public static class Sfx
{
    private static AudioSource clickSrc, hoverSrc;

    private static void Ensure()
    {
        if (clickSrc != null) return;
        var go = new GameObject("SfxHost");
        UnityEngine.Object.DontDestroyOnLoad(go);
        clickSrc = MakeCh(go, "Click");
        hoverSrc = MakeCh(go, "Hover");
    }

    private static AudioSource MakeCh(GameObject parent, string name)
    {
        var ch = new GameObject(name).transform;
        ch.SetParent(parent.transform, false);
        var s = ch.gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.spatialBlend = 0f;   // 2D
        s.loop = false;
        return s;
    }

    /// <summary>channel ќЅя«ј“≈Ћ≈Ќ Ч компил€тор укажет все места, где надо выбрать приоритет.</summary>
    public static void Play(AudioClip clip, float volume, SfxChannel channel)
    {
        if (clip == null) return;
        Ensure();
        float v = Mathf.Clamp01(volume);
        if (channel == SfxChannel.Click)
        {
            if (hoverSrc.isPlaying) hoverSrc.Stop();      // клик режет ховер
            clickSrc.clip = clip; clickSrc.volume = v; clickSrc.Play();
        }
        else
        {
            if (clickSrc.isPlaying) return;               // ховер не глушит играющий клик
            hoverSrc.clip = clip; hoverSrc.volume = v; hoverSrc.Play();
        }
    }
}