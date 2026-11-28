using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Tutorial guiado pelo Capitão Tomás.
public partial class ArenaGame
{
    // ---------------------------------------------------------------- Tutorial (Capitão Tomás)

    int tutStep = -1;
    float tutT, tutChars;
    const int TutFinal = 7;
    Texture2D texMentor, texMentorTalk;

    struct TutInfo { public string text; public bool pause; public string card; public Vector2 target; public bool elixir; public float autoTime; }

    static readonly TutInfo[] TutSteps =
    {
        new TutInfo { text = "Olá! Eu sou o Capitão Tomás.\nVou te ensinar a vencer na Arena!", pause = true },
        new TutInfo { text = "Esta é a sua barra de ELIXIR. Toda carta custa elixir, e ele enche sozinho com o tempo.", pause = true, elixir = true },
        new TutInfo { text = "Segure o GUERREIRO e solte perto da ponte da esquerda.", card = "gue", target = new Vector2(-3.6f, -2.6f) },
        new TutInfo { text = "Isso! As tropas andam sozinhas e atacam o que encontram pelo caminho.", autoTime = 3.5f },
        new TutInfo { text = "Cuidado, inimigos chegando! Solte as ARQUEIRAS para defender sua torre.", card = "arq", target = new Vector2(-2.4f, -5.2f) },
        new TutInfo { text = "Feitiços caem em qualquer lugar. Jogue a BOLA DE FOGO na torre inimiga!", card = "fog", target = new Vector2(-3.6f, 6.5f) },
        new TutInfo { text = "Muito bem! Derrube as torres do inimigo. Quem derrubar a torre do REI vence. Boa sorte!", pause = true },
    };

    void SetTutStep(int s)
    {
        tutStep = s;
        tutT = 0f;
        tutChars = 0f;
        if (s >= TutFinal) return;
        var info = TutSteps[s];
        if (info.pause) { Running = false; selected = -1; dragSlot = -1; }
        if (info.card != null)
        {
            var c = System.Array.Find(deck, d => d.key == info.card);
            if (c != null) elixir[0] = Mathf.Max(elixir[0], c.cost + 0.3f);
        }
        if (s == 4)
        {
            // o boneco manda ratinhos pela esquerda
            var rat = System.Array.Find(deck, d => d.key == "rat");
            if (rat != null) SpawnTroop(rat, 1, new Vector2(-3.6f, 4.5f));
            Sfx.Play("queda", 0.6f);
        }
    }

    void UpdateTutorial(float dt)
    {
        if (Mode != MatchMode.Tutorial || tutStep < 0 || tutStep >= TutFinal) return;
        tutT += dt;
        var info = TutSteps[tutStep];
        if (info.autoTime > 0f && tutT >= info.autoTime) SetTutStep(tutStep + 1);
        // nunca deixa faltar elixir para a carta pedida
        if (info.card != null)
        {
            var c = System.Array.Find(deck, d => d.key == info.card);
            if (c != null && elixir[0] < c.cost) elixir[0] = Mathf.Min(ElixirMax, elixir[0] + dt * 2f);
        }
    }

    void TutorialCardPlayed(CardData c)
    {
        if (tutStep < 0 || tutStep >= TutFinal) return;
        var info = TutSteps[tutStep];
        if (info.card == null) return;
        bool ok = info.card == c.key || (info.card != "fog" && c.kind != CardKind.Spell) || (info.card == "fog" && c.kind == CardKind.Spell);
        if (ok) SetTutStep(tutStep + 1);
    }

    int HandSlotOf(string key)
    {
        for (int i = 0; i < 4; i++) if (deck[hand[0][i]].key == key) return i;
        return -1;
    }

    Vector2 WorldToGui(Vector2 w)
    {
        Vector3 sp = cam.WorldToScreenPoint(w);
        return new Vector2(sp.x, Screen.height - sp.y);
    }

    // Seta dourada quicando, apontando para baixo
    static void DrawArrow(Vector2 tip, float u, float a = 1f)
    {
        float bob = Mathf.Abs(Mathf.Sin(Time.time * 5f)) * u * 2f;
        tip.y -= bob;
        var dark = new Color(0.25f, 0.12f, 0f, a);
        var gold = new Color(1f, 0.82f, 0.2f, a);
        var stem = new Rect(tip.x - u * 1.1f, tip.y - u * 8f, u * 2.2f, u * 5f);
        Ui.Fill(new Rect(stem.x - 2, stem.y - 2, stem.width + 4, stem.height + 4), dark);
        Ui.Fill(stem, gold);
        var m = GUI.matrix;
        var head = new Vector2(tip.x, tip.y - u * 2.6f);
        GUIUtility.RotateAroundPivot(45f, head);
        float s = u * 3.6f;
        Ui.Fill(new Rect(head.x - s / 2f - 2, head.y - s / 2f - 2, s + 4, s + 4), dark);
        Ui.Fill(new Rect(head.x - s / 2f, head.y - s / 2f, s, s), gold);
        GUI.matrix = m;
        Ui.Fill(new Rect(tip.x - u * 2.6f, tip.y - u * 5.4f, u * 5.2f, u * 2.4f), gold);   // tampa a metade de cima do losango
        Ui.Fill(stem, gold);
    }

    void DrawTutorial(Rect col, float u, int fs, float panelTop)
    {
        if (tutStep >= TutFinal) return;
        if (texMentor == null)
        {
            texMentor = Resources.Load<Texture2D>("Tutorial/mentor");
            texMentorTalk = Resources.Load<Texture2D>("Tutorial/mentor_fala");
        }
        var info = TutSteps[tutStep];
        if (Event.current.type == EventType.Repaint) tutChars += Time.deltaTime * 45f;
        int shown = Mathf.Min(info.text.Length, Mathf.FloorToInt(tutChars));
        bool typing = shown < info.text.Length;

        // Destaque (escurece o resto)
        Rect focus = Rect.zero;
        int slot = info.card != null ? HandSlotOf(info.card) : -1;
        if (info.elixir) focus = new Rect(col.x, panelTop + u * 13.8f, col.width, u * 5.6f);
        else if (slot >= 0) focus = new Rect(handRects[slot].x - u, handRects[slot].y - u * 2.5f, handRects[slot].width + u * 2f, handRects[slot].height + u * 3.5f);
        var dim = new Color(0f, 0f, 0f, 0.5f);
        if (info.pause || focus != Rect.zero)
        {
            if (focus == Rect.zero) Ui.Fill(col, dim);
            else
            {
                Ui.Fill(new Rect(col.x, col.y, col.width, focus.y - col.y), dim);
                Ui.Fill(new Rect(col.x, focus.yMax, col.width, col.yMax - focus.yMax), dim);
                Ui.Fill(new Rect(col.x, focus.y, focus.x - col.x, focus.height), dim);
                Ui.Fill(new Rect(focus.xMax, focus.y, col.xMax - focus.xMax, focus.height), dim);
                float p = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
                var gl = new Color(1f, 0.85f, 0.3f, 0.5f + 0.5f * p);
                Ui.Fill(new Rect(focus.x, focus.y, focus.width, 3), gl);
                Ui.Fill(new Rect(focus.x, focus.yMax - 3, focus.width, 3), gl);
                Ui.Fill(new Rect(focus.x, focus.y, 3, focus.height), gl);
                Ui.Fill(new Rect(focus.xMax - 3, focus.y, 3, focus.height), gl);
            }
        }

        // Alvo na arena + mãozinha mostrando o movimento de arrastar
        if (info.card != null && Running)
        {
            Vector2 t = WorldToGui(info.target);
            float pulse = 1f + 0.2f * Mathf.Sin(Time.time * 5f);
            float r = u * 5f * pulse;
            Ui.Disc(new Rect(t.x - r, t.y - r * 0.6f, r * 2f, r * 1.2f), new Color(1f, 0.9f, 0.3f, 0.35f));
            Ui.Disc(new Rect(t.x - r * 0.6f, t.y - r * 0.36f, r * 1.2f, r * 0.72f), new Color(1f, 1f, 1f, 0.35f));
            DrawArrow(new Vector2(t.x, t.y - r * 0.5f), u);
            if (slot >= 0)
            {
                float k = Mathf.Repeat(Time.time * 0.6f, 1f);
                float e = k < 0.75f ? Mathf.SmoothStep(0f, 1f, k / 0.75f) : 1f;
                Vector2 from = handRects[slot].center;
                Vector2 hp = Vector2.Lerp(from, t, e);
                float ha = k < 0.85f ? 0.9f : 0.9f * (1f - (k - 0.85f) / 0.15f);
                var hr = new Rect(hp.x - u * 2.2f, hp.y - u * 1.2f, u * 4.4f, u * 4.4f);
                Ui.Disc(new Rect(hr.x - 2, hr.y - 2, hr.width + 4, hr.height + 4), new Color(0.1f, 0.1f, 0.15f, ha));
                Ui.Disc(hr, new Color(1f, 0.92f, 0.82f, ha));
                Ui.Fill(new Rect(hp.x - u * 0.6f, hp.y - u * 3.4f, u * 1.2f, u * 2.8f), new Color(1f, 0.92f, 0.82f, ha));  // dedo
            }
        }
        else if (info.elixir) DrawArrow(new Vector2(col.center.x, focus.y - u * 0.5f), u);

        // Capitão + balão de fala
        float boxH = u * 17f;
        // o balão nunca cobre o alvo: alvo embaixo -> balão em cima, alvo em cima -> balão no meio
        float by = info.pause ? col.y + col.height * 0.33f
            : info.card != null && info.target.y > 0f ? col.y + col.height * 0.5f : col.y + col.height * 0.13f;
        if (info.elixir) by = panelTop - boxH - u * 10f;
        var mentorR = new Rect(col.x + u, by - u * 3f, u * 15f, boxH + u * 4f);
        var bubble = new Rect(mentorR.xMax - u, by, col.xMax - mentorR.xMax - u, boxH);
        Ui.Fill(new Rect(bubble.x - 3, bubble.y - 3, bubble.width + 6, bubble.height + 6), new Color(0.1f, 0.1f, 0.2f, 0.9f));
        Ui.Fill(bubble, new Color(0.98f, 0.97f, 0.92f));
        Ui.Fill(new Rect(bubble.x, bubble.y, bubble.width, u * 3.4f), new Color(0.15f, 0.6f, 0.55f));
        Ui.Text(new Rect(bubble.x + u * 1.5f, bubble.y, bubble.width, u * 3.4f), "Capitão Tomás", fs - 3, Color.white, TextAnchor.MiddleLeft);
        // rabinho do balão
        var m = GUI.matrix;
        var tail = new Vector2(bubble.x, bubble.y + boxH * 0.6f);
        GUIUtility.RotateAroundPivot(45f, tail);
        Ui.Fill(new Rect(tail.x - u * 1.4f, tail.y - u * 1.4f, u * 2.8f, u * 2.8f), new Color(0.98f, 0.97f, 0.92f));
        GUI.matrix = m;
        Ui.Text(new Rect(bubble.x + u * 1.5f, bubble.y + u * 3.8f, bubble.width - u * 3f, boxH - u * 9f),
            info.text.Substring(0, shown), fs - 2, new Color(0.15f, 0.12f, 0.2f), TextAnchor.UpperLeft, false);

        bool talk = typing && Mathf.Repeat(Time.time * 6f, 1f) < 0.5f;
        var tex = talk && texMentorTalk != null ? texMentorTalk : texMentor;
        float bounce = typing ? Mathf.Abs(Mathf.Sin(Time.time * 10f)) * u * 0.6f : 0f;
        if (tex != null) GUI.DrawTexture(new Rect(mentorR.x, mentorR.y - bounce, mentorR.width, mentorR.height), tex, ScaleMode.ScaleToFit);

        // Passos
        for (int i = 0; i < TutSteps.Length; i++)
            Ui.Disc(new Rect(bubble.x + u * 1.5f + i * u * 2.2f, bubble.yMax - u * 3.2f, u * 1.4f, u * 1.4f),
                i <= tutStep ? new Color(0.15f, 0.6f, 0.55f) : new Color(0.75f, 0.75f, 0.75f));

        if (info.pause)
        {
            // Toque em qualquer lugar: completa o texto ou avança
            var b = new Rect(bubble.xMax - u * 17f, bubble.yMax - u * 5.6f, u * 16f, u * 4.8f);
            Ui.Panel(b, typing ? new Color(0.5f, 0.5f, 0.55f) : Ui.Orange);
            Ui.Text(b, typing ? "Pular" : "Continuar", fs - 2, Color.white);
            if (!typing)
                Ui.Text(new Rect(col.x, bubble.yMax + u * 1.5f, col.width, u * 3f), "toque para continuar", fs - 5,
                    new Color(1f, 1f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.time * 4f)), TextAnchor.MiddleCenter, false);
            var ev = Event.current;
            if (ev.type == EventType.MouseDown && col.Contains(ev.mousePosition))
            {
                ev.Use();
                if (typing) tutChars = info.text.Length;
                else
                {
                    Sfx.Play("botao");
                    Running = true;
                    SetTutStep(tutStep + 1);
                }
            }
        }
        if (!info.pause && tutStep < TutFinal)
        {
            var skip = new Rect(col.xMax - u * 13f, col.y + u * 8f, u * 12f, u * 4f);
            if (Ui.Button(skip, new Color(0.3f, 0.3f, 0.38f), "Pular tutorial", fs - 5)) SetTutStep(TutFinal);
        }
    }
}
