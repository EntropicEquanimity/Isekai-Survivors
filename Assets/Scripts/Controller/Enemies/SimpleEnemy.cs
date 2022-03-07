using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class SimpleEnemy : Entity
{
    public Vector2Int expDrop;
    public int essenceDrop;

    public Player target;
    private Rigidbody2D _rb;

    public OnBecameInvisibleBehavior onBecameInvisibleBehavior;
    private float _lastTimeDamageDealt;
    private float _timeBeforeDisabling;
    private bool _invisible;
    public override void OnEnable()
    {
        base.OnEnable();
        GetComponent<CircleCollider2D>().enabled = true;
        _rb = GetComponent<Rigidbody2D>();
        entitySprite.material.SetFloat("_FadeAmount", -0.15f);
    }
    private void Start()
    {
        target = GameManager.Instance.player;
        _rb.mass = KnockBackResistance;
    }
    public void FixedUpdate()
    {
        Move();

        if (_invisible)
        {
            _timeBeforeDisabling -= Time.deltaTime;
            if (_timeBeforeDisabling <= 0f)
            {
                switch (onBecameInvisibleBehavior)
                {
                    case OnBecameInvisibleBehavior.Disable:
                        gameObject.SetActive(false);
                        break;
                    case OnBecameInvisibleBehavior.TeleportToOtherSide:
                        _rb.MovePosition(Vector2.Distance(target.transform.position, transform.position) * (target.transform.position - transform.position).normalized);
                        break;
                }
                _timeBeforeDisabling = 5f;
            }
        }
    }
    public void OnBecameInvisible()
    {
        _invisible = true;
    }
    public void OnBecameVisible()
    {
        _timeBeforeDisabling = 5f;
        _invisible = false;
    }
    public override void Move()
    {
        if (HP <= 0) { return; }

        _rb.MovePosition(DirectionToTarget * MoveSpeed * Time.fixedDeltaTime + (Vector2)transform.position);

        entitySprite.flipX = target.transform.position.x < transform.position.x;
    }
    public virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.CompareTag("Player"))
        {
            collision.transform.GetComponent<Player>().TakeDamage(new DamageInfo() { damage = Damage, critChance = 0 });
            _lastTimeDamageDealt = Time.realtimeSinceStartup;
        }
    }
    public virtual void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.transform.CompareTag("Player") && Time.realtimeSinceStartup - _lastTimeDamageDealt > 0.5f)
        {
            collision.transform.GetComponent<Player>().TakeDamage(new DamageInfo() { damage = Damage, critChance = 0 });
            _lastTimeDamageDealt = Time.realtimeSinceStartup;
        }
    }
    public override void Die()
    {
        GameManager.Instance.PlayerKills++;
        int exp = Random.Range(expDrop.x, expDrop.y + 1);
        if (exp > 0) { LootController.Instance.SpawnExperience(exp, transform.position); }
        StartCoroutine(DeathAnimation());
    }
    public IEnumerator DeathAnimation()
    {
        GetComponent<CircleCollider2D>().enabled = false;
        MoveSpeed = 0;
        Damage = 0;
        HP = 0;
        float fadeAmount = -0.15f;
        while (fadeAmount < 1f)
        {
            fadeAmount += Time.fixedDeltaTime * 2f;
            entitySprite.material.SetFloat("_FadeAmount", fadeAmount);
            yield return new WaitForFixedUpdate();
        }
        gameObject.SetActive(false);
    }
    public Vector2 DirectionToTarget => (target.transform.position - transform.position).normalized;
}
public enum OnBecameInvisibleBehavior
{
    Disable,
    TeleportToOtherSide
}