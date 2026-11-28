using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Partículas: poeira, marcas no chão, pedrinhas e faíscas.
public partial class ArenaGame
{
    // ---------------------------------------------------------------- Partículas e marcas no chão

    class Particle
    {
        public Transform t;
        public SpriteRenderer sr;
        public Vector2 v;
        public float life, max, grav, spin, size, z;
        public Color c;
        public bool ground;    // marca no chão: só desbota
    }
    readonly List<Particle> particles = new List<Particle>();

    Particle Spawn(Vector2 pos, Sprite s, Color c, float size, float life, int order)
    {
        if (matchRoot == null) return null;
        // Qualidade baixa: metade das partículas e no máximo 40 ao mesmo tempo (PCs mais fracos)
        if (Profile.LowQuality && order > -800 && (particles.Count > 40 || Random.value < 0.5f)) return null;
        var go = new GameObject("Particula");
        go.transform.SetParent(matchRoot);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * size;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.color = c;
        sr.sortingOrder = order;
        var p = new Particle { t = go.transform, sr = sr, life = life, max = life, size = size, c = c };
        particles.Add(p);
        return p;
    }

    // Poeira quando a tropa cai na arena
    public void Dust(Vector2 pos, float size)
    {
        for (int i = 0; i < 7; i++)
        {
            float a = i / 7f * Mathf.PI * 2f + Random.value * 0.4f;
            var p = Spawn(pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.5f) * 0.2f * size, CircleSprite,
                new Color(0.9f, 0.85f, 0.75f, 0.7f), Random.Range(0.25f, 0.4f) * size, Random.Range(0.4f, 0.6f), 2300);
            if (p == null) return;
            p.v = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.5f) * Random.Range(1.2f, 2f) * size;
            p.grav = -0.6f;
        }
    }

    // Marca escura no chão depois de explosões (some aos poucos)
    public void Scorch(Vector2 pos, float radius)
    {
        var p = Spawn(pos, CircleSprite, new Color(0.08f, 0.06f, 0.05f, 0.38f), 1f, 7f, -850);
        if (p == null) return;
        p.t.localScale = new Vector3(radius * 2f, radius * 1.3f, 1f);
        p.ground = true;
        for (int i = 0; i < 5; i++)
        {
            var q = Spawn(pos + Random.insideUnitCircle * radius * 0.8f, CircleSprite, new Color(0.08f, 0.06f, 0.05f, 0.3f), 1f, 7f, -849);
            if (q == null) continue;
            q.t.localScale = new Vector3(radius * 0.5f, radius * 0.3f, 1f) * Random.Range(0.6f, 1.2f);
            q.ground = true;
        }
    }

    // Pedrinhas voando (torres atingidas, explosões)
    public void Debris(Vector2 pos, int n, Color c)
    {
        for (int i = 0; i < n; i++)
        {
            var p = Spawn(pos + Random.insideUnitCircle * 0.3f, SquareSprite, Color.Lerp(c, Color.black, Random.value * 0.3f),
                Random.Range(0.08f, 0.16f), Random.Range(0.5f, 0.8f), 2700);
            if (p == null) return;
            p.v = new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(2.5f, 4.5f));
            p.grav = 11f;
            p.spin = Random.Range(-720f, 720f);
            p.z = pos.y - 0.4f;
        }
    }

    // Faíscas no golpe corpo a corpo
    public void Sparks(Vector2 pos)
    {
        for (int i = 0; i < 5; i++)
        {
            var p = Spawn(pos + Vector2.up * 0.4f, SquareSprite, new Color(1f, 0.95f, 0.5f, 1f), 0.08f, 0.22f, 2800);
            if (p == null) return;
            p.v = Random.insideUnitCircle.normalized * Random.Range(2.5f, 4f);
            p.t.localScale = new Vector3(0.18f, 0.05f, 1f);
            p.t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(p.v.y, p.v.x) * Mathf.Rad2Deg);
        }
    }

    void UpdateParticles(float dt)
    {
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var p = particles[i];
            if (p.t == null) { particles.RemoveAt(i); continue; }
            p.life -= dt;
            if (p.life <= 0f) { Destroy(p.t.gameObject); particles.RemoveAt(i); continue; }
            float k = p.life / p.max;
            if (p.ground)
            {
                p.sr.color = new Color(p.c.r, p.c.g, p.c.b, p.c.a * Mathf.Clamp01(k * 3f));
                continue;
            }
            p.v.y -= p.grav * dt;
            Vector2 np = (Vector2)p.t.position + p.v * dt;
            if (p.grav > 5f && np.y < p.z) { np.y = p.z; p.v *= 0.4f; p.v.y = Mathf.Abs(p.v.y); }   // quica no chão
            p.t.position = np;
            if (p.spin != 0f) p.t.Rotate(0f, 0f, p.spin * dt);
            p.sr.color = new Color(p.c.r, p.c.g, p.c.b, p.c.a * Mathf.Clamp01(k * 2f));
            if (p.grav < 0f) p.t.localScale = Vector3.one * p.size * (1.6f - 0.6f * k);
        }
    }
}
