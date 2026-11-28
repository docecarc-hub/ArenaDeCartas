using UnityEngine;

// Pequeno kit de interface (IMGUI) com o visual do jogo: painéis, botões, textos com sombra e discos.
public static class Ui
{
    static Texture2D circle;
    static GUIStyle label;

    public static readonly Color Bg = new Color(0.09f, 0.18f, 0.27f);
    public static readonly Color Dark = new Color(0.05f, 0.11f, 0.18f);
    public static readonly Color Teal = new Color(0.10f, 0.62f, 0.62f);
    public static readonly Color Orange = new Color(1.00f, 0.55f, 0.12f);
    public static readonly Color Elixir = new Color(0.82f, 0.25f, 0.88f);
    public static readonly Color Gold = new Color(1.00f, 0.82f, 0.25f);

    static Texture2D diamonds;

    // Fundo com padrão de losangos (repete na tela inteira)
    public static void DiamondBackground(Rect r, float cell)
    {
        if (diamonds == null) diamonds = MakeDiamonds(64);
        Fill(r, new Color(0.10f, 0.22f, 0.36f));
        GUI.DrawTextureWithTexCoords(r, diamonds, new Rect(0, 0, r.width / cell, r.height / cell));
    }

    static Texture2D MakeDiamonds(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear
        };
        var px = new Color[size * size];
        float h = size / 2f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Mathf.Abs(x + 0.5f - h) + Mathf.Abs(y + 0.5f - h);   // distância "losango"
            float edge = Mathf.Abs(d - h);
            Color c = d < h ? new Color(0.14f, 0.30f, 0.47f) : new Color(0.11f, 0.25f, 0.40f);
            if (edge < 1.5f) c = new Color(0.07f, 0.16f, 0.27f);
            else if (d < h && d > h - 4f) c = Color.Lerp(c, new Color(0.2f, 0.4f, 0.6f), 0.5f);
            px[y * size + x] = c;
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    public static Texture2D CircleTex
    {
        get
        {
            if (circle == null) circle = MakeCircle(64);
            return circle;
        }
    }

    // Área "de celular" (9:16) centralizada na tela. Tudo é desenhado dentro dela.
    public static Rect Column()
    {
        float W = Screen.width, H = Screen.height;
        float w = Mathf.Min(W, H * 9f / 16f);
        return new Rect((W - w) / 2f, 0f, w, H);
    }

    public static Color Shade(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

    public static void Fill(Rect r, Color c)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    public static void Disc(Rect r, Color c)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, CircleTex);
        GUI.color = old;
    }

    public static void Panel(Rect r, Color c)
    {
        Fill(r, Shade(c, 0.5f));
        var inner = new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 6);
        Fill(inner, c);
        Fill(new Rect(inner.x, inner.y, inner.width, inner.height * 0.45f), new Color(1f, 1f, 1f, 0.12f));
    }

    public static float FontScale = 1f;   // tamanho do texto (acessibilidade)

    public static void Text(Rect r, string text, int size, Color color,
        TextAnchor align = TextAnchor.MiddleCenter, bool bold = true)
    {
        if (label == null)
            label = new GUIStyle(GUI.skin.label) { wordWrap = true, clipping = TextClipping.Overflow };

        label.fontSize = Mathf.Max(7, Mathf.RoundToInt(size * FontScale));
        label.alignment = align;
        label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;

        label.normal.textColor = new Color(0f, 0f, 0f, 0.75f);
        GUI.Label(new Rect(r.x + 1, r.y + 2, r.width, r.height), text, label);
        label.normal.textColor = color;
        GUI.Label(r, text, label);
    }

    public static bool Button(Rect r, Color c, string text, int size)
    {
        bool hover = GUI.enabled && r.Contains(Event.current.mousePosition);
        Panel(r, hover ? Shade(c, 1.12f) : c);
        Text(r, text, size, Color.white);
        bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
        if (clicked) Sfx.Play("botao");
        return clicked;
    }

    public static void Bar(Rect r, float t, Color fill)
    {
        Fill(r, new Color(0f, 0f, 0f, 0.55f));
        Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(t), r.height), fill);
    }

    static Texture2D MakeCircle(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        float rad = size / 2f;
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(rad, rad));
            px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(rad - d));
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }
}
