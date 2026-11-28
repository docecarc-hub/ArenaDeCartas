using System.Collections.Generic;
using UnityEngine;

// Replay: grava a partida (posição e desenho de cada tropa, torre e projétil, 10 vezes por segundo)
// e reproduz em cima da própria arena quando você toca em "Replay" na tela de resultado.
public partial class ArenaGame
{
    struct Snap
    {
        public Sprite s;
        public Vector3 p, sc;
        public bool flip;
        public Color c;
        public int order;
    }

    struct ReplayFrame
    {
        public Snap[] snaps;
        public float time;
        public int crownsBlue, crownsRed;
    }

    const float ReplayStep = 0.1f;
    readonly List<ReplayFrame> replay = new List<ReplayFrame>();
    readonly List<SpriteRenderer> replayPool = new List<SpriteRenderer>();
    readonly List<GameObject> replayHidden = new List<GameObject>();
    readonly List<Snap> snapBuf = new List<Snap>();
    float recT, replayPos, replaySpeed = 1f;
    bool replaying;
    Transform replayRoot;

    public bool HasReplay => replay.Count > 20;

    void ClearReplay()
    {
        replay.Clear();
        recT = 0f;
        replaying = false;
    }

    // Chamado a cada quadro durante a partida
    void RecordReplay(float dt)
    {
        if (!Running || replaying || Mode == MatchMode.Tutorial || matchRoot == null) return;
        recT -= dt;
        if (recT > 0f) return;
        recT = ReplayStep;
        if (replay.Count > 4000) return;   // limite de segurança (mais de 6 minutos)

        snapBuf.Clear();
        foreach (var e in entities)
        {
            if (e == null || !e.Alive || e.Body == null || e.Body.sprite == null) continue;
            Add(e.Body);
        }
        foreach (var pr in matchRoot.GetComponentsInChildren<Projectile>())
        {
            var sr = pr.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null) Add(sr);
        }
        replay.Add(new ReplayFrame { snaps = snapBuf.ToArray(), time = timeLeft, crownsBlue = crowns[0], crownsRed = crowns[1] });

        void Add(SpriteRenderer sr)
        {
            var t = sr.transform;
            snapBuf.Add(new Snap { s = sr.sprite, p = t.position, sc = t.lossyScale, flip = sr.flipX, c = sr.color, order = sr.sortingOrder });
        }
    }

    // Esconde a batalha de verdade e começa a reproduzir
    public void StartReplay()
    {
        if (!HasReplay || matchRoot == null) return;
        replayHidden.Clear();
        foreach (Transform child in matchRoot)
        {
            if (child.GetComponent<Entity>() == null && child.GetComponent<Projectile>() == null && child.name != "Particula"
                && child.name != "TextoFlutuante") continue;
            if (!child.gameObject.activeSelf) continue;
            child.gameObject.SetActive(false);
            replayHidden.Add(child.gameObject);
        }
        if (replayRoot == null)
        {
            replayRoot = new GameObject("Replay").transform;
            replayRoot.SetParent(matchRoot);
        }
        replaying = true;
        replayPos = 0f;
        replaySpeed = 1f;
        Sfx.Play("botao");
    }

    void StopReplay()
    {
        replaying = false;
        foreach (var sr in replayPool) if (sr != null) sr.enabled = false;
        foreach (var go in replayHidden) if (go != null) go.SetActive(true);
        replayHidden.Clear();
    }

    void UpdateReplay(float dt)
    {
        replayPos += dt * replaySpeed / ReplayStep;
        if (replayPos >= replay.Count - 1) replayPos = replay.Count - 1;
        var f = replay[Mathf.Clamp((int)replayPos, 0, replay.Count - 1)];
        while (replayPool.Count < f.snaps.Length)
        {
            var go = new GameObject("ReplaySprite");
            go.transform.SetParent(replayRoot);
            replayPool.Add(go.AddComponent<SpriteRenderer>());
        }
        for (int i = 0; i < replayPool.Count; i++)
        {
            var sr = replayPool[i];
            if (i >= f.snaps.Length) { sr.enabled = false; continue; }
            var s = f.snaps[i];
            sr.enabled = true;
            sr.sprite = s.s;
            sr.flipX = s.flip;
            sr.color = s.c;
            sr.sortingOrder = s.order;
            sr.transform.position = s.p;
            sr.transform.localScale = s.sc;
        }
    }

    // Barra do replay: tempo, coroas, velocidade e sair
    void DrawReplayHud(Rect col, float u, int fs)
    {
        var f = replay[Mathf.Clamp((int)replayPos, 0, replay.Count - 1)];
        var top = new Rect(col.x, col.y, col.width, u * 8f);
        Ui.Fill(top, new Color(0f, 0f, 0f, 0.6f));
        Ui.Text(new Rect(top.x, top.y + u * 0.5f, top.width, u * 4f), "REPLAY", fs + 3, new Color(1f, 0.4f, 0.4f));
        int sec = Mathf.Max(0, Mathf.CeilToInt(f.time));
        Ui.Text(new Rect(top.x, top.y + u * 4.2f, top.width, u * 3.5f),
            $"{sec / 60}:{sec % 60:00}     Coroas  {f.crownsBlue} x {f.crownsRed}", fs - 2, Color.white);

        var bar = new Rect(col.x + u * 2f, col.yMax - u * 14f, col.width - u * 4f, u * 1.4f);
        Ui.Fill(bar, new Color(0f, 0f, 0f, 0.6f));
        Ui.Fill(new Rect(bar.x, bar.y, bar.width * (replayPos / Mathf.Max(1, replay.Count - 1)), bar.height), Ui.Gold);

        float bw = (col.width - u * 8f) / 3f, by = col.yMax - u * 10.5f;
        if (Ui.Button(new Rect(col.x + u * 2f, by, bw, u * 7f), new Color(0.3f, 0.34f, 0.45f), "Do início", fs - 2))
            replayPos = 0f;
        if (Ui.Button(new Rect(col.x + u * 4f + bw, by, bw, u * 7f), Ui.Teal, $"Velocidade x{replaySpeed:0}", fs - 3))
            replaySpeed = replaySpeed >= 4f ? 1f : replaySpeed * 2f;
        if (Ui.Button(new Rect(col.x + u * 6f + bw * 2f, by, bw, u * 7f), Ui.Orange, "Sair", fs))
            StopReplay();
        if (replayPos >= replay.Count - 1)
            Ui.Text(new Rect(col.x, col.center.y - u * 4f, col.width, u * 8f), "Fim do replay", fs + 6, Color.white);
    }
}
