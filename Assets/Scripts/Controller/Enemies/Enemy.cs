using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : Entity
{
    [BoxGroup("Drops")] public Vector2Int expDrop;
    [BoxGroup("Drops")] public int essenceDrop;

    [BoxGroup("Enemy Behavior")] public bool followTargetPositionInstead = false;
    [BoxGroup("Enemy Behavior")] public float attackCooldown = 0.5f;
    [BoxGroup("Enemy Behavior")] public OnBecameInvisibleBehavior onBecameInvisibleBehavior;

    [ReadOnly] [BoxGroup("Read Only")] public Player target;
    [ReadOnly] [BoxGroup("Read Only")] public float range;
    [ReadOnly] [BoxGroup("Read Only")] [ShowIf("followTargetPositionInstead")] public Vector3 targetPosition;

    private Rigidbody2D _rb;
    private float _lastTimeDamageDealt;
    private float _timeBeforeDisabling;
    private bool _invisible;
    public override void Initialize(EntityStats entityStats)
    {
        base.Initialize(entityStats);
        range = GetComponent<CircleCollider2D>().radius * transform.localScale.magnitude;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        GetComponent<CircleCollider2D>().enabled = true;
        _rb = GetComponent<Rigidbody2D>();
        entitySpriteRenderer.material.SetFloat("_FadeAmount", -0.15f);
    }
    public virtual void Start()
    {
        target = GameManager.Instance.player;
        _rb.mass = KnockBackResistance;
    }
    public virtual void FixedUpdate()
    {
        if(target == null) { return; }
        Move();
        Attack();
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
        if (followTargetPositionInstead)
        {
            _rb.MovePosition(MoveSpeed * Time.fixedDeltaTime * (targetPosition - transform.position).normalized + transform.position);
            entitySpriteRenderer.flipX = target.transform.position.x < transform.position.x;
            return;
        }
        if (HP <= 0 || DistanceToTarget < range) { return; }

        _rb.MovePosition(MoveSpeed * Time.fixedDeltaTime * DirectionToTarget + (Vector2)transform.position);
        entitySpriteRenderer.flipX = target.transform.position.x < transform.position.x;
    }
    public virtual void Attack()
    {
        if (DistanceToTarget <= range && AttackOffCooldown)
        {
            target.TakeDamage(new DamageInfo() { damage = Damage });
            _lastTimeDamageDealt = Time.realtimeSinceStartup;
        }
    }
    //public virtual void OnCollisionEnter2D(Collision2D collision)
    //{
    //    if (collision.transform.CompareTag("Player"))
    //    {
    //        collision.transform.GetComponent<Player>().TakeDamage(new DamageInfo() { damage = Damage, critChance = 0 });
    //        _lastTimeDamageDealt = Time.realtimeSinceStartup;
    //    }
    //}
    //public virtual void OnCollisionStay2D(Collision2D collision)
    //{
    //    if (collision.transform.CompareTag("Player") && Time.realtimeSinceStartup - _lastTimeDamageDealt > 0.5f)
    //    {
    //        collision.transform.GetComponent<Player>().TakeDamage(new DamageInfo() { damage = Damage, critChance = 0 });
    //        _lastTimeDamageDealt = Time.realtimeSinceStartup;
    //    }
    //}
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
            entitySpriteRenderer.material.SetFloat("_FadeAmount", fadeAmount);
            yield return new WaitForFixedUpdate();
        }
        gameObject.SetActive(false);
        EnemyManager.Instance.EnemyDeath(this);
    }
    public Vector2 DirectionToTarget => (target.transform.position - transform.position).normalized;
    public float DistanceToTarget => Vector2.Distance(transform.position, target.transform.position);
    public bool AttackOffCooldown => Time.realtimeSinceStartup - _lastTimeDamageDealt > attackCooldown;
}
public enum OnBecameInvisibleBehavior
{
    Disable,
    TeleportToOtherSide
}