using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Beleza da imagem: pós-processamento leve (brilho, vinheta e cores) e tela de carregamento.
public partial class ArenaGame
{
    Volume postVolume;
    float splashT = 2.4f;          // tela de carregamento ao abrir o jogo
    Texture2D emblem;

    // Brilho suave nas partes claras, cantos levemente escuros e cores mais vivas.
    void SetupPostFx()
    {
        var go = new GameObject("EfeitosVisuais");
        DontDestroyOnLoad(go);
        postVolume = go.AddComponent<Volume>();
        postVolume.isGlobal = true;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(0.45f);
        bloom.threshold.Override(0.92f);
        bloom.scatter.Override(0.6f);
        var vig = profile.Add<Vignette>(true);
        vig.intensity.Override(0.22f);
        vig.smoothness.Override(0.45f);
        var col = profile.Add<ColorAdjustments>(true);
        col.saturation.Override(12f);
        col.contrast.Override(8f);
        postVolume.profile = profile;
        ApplyPostFx();
    }

    public void ApplyPostFx()
    {
        if (cam == null) return;
        var data = cam.GetUniversalAdditionalCameraData();
        if (data != null) data.renderPostProcessing = Profile.PostFx;
        if (postVolume != null) postVolume.enabled = Profile.PostFx;
    }

    // Qualidade: 60 quadros/s normalmente, 30 na qualidade baixa (esquenta menos o PC)
    public void ApplyQuality()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Profile.LowQuality ? 30 : 60;
    }

    // Contador de quadros por segundo (média de meio segundo)
    float fpsAcc, fpsShown;
    int fpsFrames;
    void TrackFps()
    {
        fpsAcc += Time.unscaledDeltaTime;
        fpsFrames++;
        if (fpsAcc >= 0.5f) { fpsShown = fpsFrames / fpsAcc; fpsAcc = 0f; fpsFrames = 0; }
    }

    void DrawFps(Rect col, float u, int fs)
    {
        if (!Profile.ShowFps) return;
        var r = new Rect(col.x + u * 0.5f, col.yMax - u * 3.5f, u * 9f, u * 3f);
        Ui.Fill(r, new Color(0f, 0f, 0f, 0.55f));
        Color c = fpsShown >= 50f ? new Color(0.5f, 1f, 0.5f) : fpsShown >= 28f ? new Color(1f, 0.9f, 0.4f) : new Color(1f, 0.45f, 0.4f);
        Ui.Text(r, $"{Mathf.RoundToInt(fpsShown)} FPS", fs - 5, c);
    }

    // Tela de carregamento: emblema 3D, nome do jogo, barra e dica
    bool DrawSplash(Rect col, float u, int fs)
    {
        if (splashT <= 0f) return false;
        if (Event.current.type == EventType.Repaint) splashT -= Time.unscaledDeltaTime;
        if (emblem == null) emblem = Resources.Load<Texture2D>("Icones/emblema");
        float k = 1f - Mathf.Clamp01(splashT / 2.4f);
        float a = Mathf.Clamp01(splashT / 0.35f);
        for (int i = 0; i < 20; i++)
        {
            float t = i / 19f;
            Ui.Fill(new Rect(col.x, col.y + col.height * t, col.width, col.height / 20f + 1f),
                Color.Lerp(new Color(0.10f, 0.16f, 0.38f, a), new Color(0.05f, 0.06f, 0.14f, a), t));
        }
        float bob = Mathf.Sin(Time.unscaledTime * 2f) * u;
        float s = col.width * 0.55f * (0.9f + 0.1f * Mathf.Clamp01(k * 3f));
        if (emblem != null)
        {
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.DrawTexture(new Rect(col.center.x - s / 2f, col.y + col.height * 0.16f + bob, s, s * 1.1f), emblem, ScaleMode.ScaleToFit);
            GUI.color = old;
        }
        Outlined(new Rect(col.x, col.y + col.height * 0.58f, col.width, u * 8f), "ARENA DE CARTAS", fs + 10, Ui.Gold, a);
        Ui.Text(new Rect(col.x, col.y + col.height * 0.66f, col.width, u * 3f), "O Reino das Cinco Ilhas", fs - 2, new Color(0.8f, 0.9f, 1f, a), TextAnchor.MiddleCenter, false);
        var bar = new Rect(col.x + col.width * 0.15f, col.y + col.height * 0.78f, col.width * 0.7f, u * 2.4f);
        Ui.Fill(new Rect(bar.x - 2, bar.y - 2, bar.width + 4, bar.height + 4), new Color(0f, 0f, 0f, 0.6f * a));
        Ui.Fill(new Rect(bar.x, bar.y, bar.width * Mathf.SmoothStep(0f, 1f, k), bar.height), new Color(0.3f, 0.85f, 1f, a));
        Ui.Text(new Rect(col.x, bar.yMax + u, col.width, u * 3f), "Carregando...", fs - 4, new Color(1f, 1f, 1f, 0.7f * a), TextAnchor.MiddleCenter, false);
        Ui.Text(new Rect(col.x, col.yMax - u * 5f, col.width, u * 3f), "Feito por Kaio Kelven  ·  Unity", fs - 6, new Color(1f, 1f, 1f, 0.45f * a), TextAnchor.MiddleCenter, false);
        return true;
    }
}
