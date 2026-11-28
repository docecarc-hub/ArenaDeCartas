using UnityEngine;

// Flecha / magia / bala de canhão que voa até o alvo e causa dano ao chegar.
public class Projectile : MonoBehaviour
{
    Entity target;
    float damage;
    bool rotate;
    float speed = 11f;
    float splash;
    int team;
    ArenaGame game;
    public float slow;      // segundos de lentidão no alvo (Torre de Gelo)

    public void Init(Entity target, float damage, bool rotate, float speed, float splash, int team, ArenaGame game)
    {
        this.target = target;
        this.damage = damage;
        this.rotate = rotate;
        this.speed = speed;
        this.splash = splash;
        this.team = team;
        this.game = game;
    }

    void Update()
    {
        if (target == null || !target.Alive)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 dest = (Vector2)target.transform.position + Vector2.up * 0.4f;
        Vector2 pos = transform.position;
        Vector2 dir = dest - pos;

        if (rotate && dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        transform.position = Vector2.MoveTowards(pos, dest, speed * Time.deltaTime);

        if (((Vector2)transform.position - dest).sqrMagnitude < 0.04f)
        {
            if (slow > 0f) target.slowT = Mathf.Max(target.slowT, slow);
            if (splash > 0f) game.Splash(team, target.transform.position, splash, damage);
            else target.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
