using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine.Events;

public abstract class Entity : MonoBehaviour
{
    [BoxGroup("Stats")] [Expandable] public EntitySO baseStats;
    [Required] public SpriteRenderer entitySprite;

    [BoxGroup("Settings")] public bool flashRedOnTakeDamage = false;

    #region Stats
    public int MaxHP { get; set; }
    public int HP { get; set; }
    public int Damage { get; set; }
    public float MoveSpeed { get; set; }
    public int Defense { get; set; }
    public int KnockBackResistance { get; set; }
    #endregion

    private bool _initialized = false;
    public bool CanTakeDamage { get; set; }
    public bool IsDead => HP <= 0;
    public virtual void OnEnable()
    {
        if (!_initialized) { Initialize(baseStats.entityStats); }

        if (_damageColorCoroutine != null)
        {
            StopAllCoroutines();
            entitySprite.color = Color.white;
        }
    }
    public virtual void Initialize(EntityStats entityStats)
    {
        MaxHP = entityStats.health;
        HP = entityStats.health;
        Damage = entityStats.damage;
        MoveSpeed = entityStats.moveSpeed;
        Defense = entityStats.defense;
        KnockBackResistance = entityStats.knockBackResistance;

        CanTakeDamage = true;

        _initialized = true;
    }
    public DamageReport TakeDamage(DamageInfo damageInfo)
    {
        if (!CanTakeDamage || IsDead) { return new DamageReport(); }
        OnAttacked?.Invoke(damageInfo);
        if (!CanTakeDamage) { return new DamageReport(); }

        bool isCrit = Random.Range(0f, 1f) < damageInfo.critChance;
        int damage = isCrit == true ? damageInfo.damage * 2 : damageInfo.damage;
        damage -= Defense;
        HP -= damage;

        if (flashRedOnTakeDamage)
        {
            if (_damageColorCoroutine != null) { StopCoroutine(_damageColorCoroutine); }
            _damageColorCoroutine = StartCoroutine(TakeDamageColor());
        }

        DamageReport damageReport = new DamageReport()
        {
            attacker = damageInfo.attacker,
            victim = this,
            damageDealt = damage,
            damageBlocked = Defense,
            isDead = false
        };

        if (HP <= 0)
        {
            damageReport.isDead = true;
            OnDeath?.Invoke(damageReport);
            if (HP <= 0)
            {
                damageReport.attacker.OnKill?.Invoke(damageReport);
                Die();
            }
        }
        OnTakeDamage?.Invoke(damageReport);
        return damageReport;
    }
    private Coroutine _damageColorCoroutine;
    private IEnumerator TakeDamageColor()
    {
        entitySprite.color = Color.red;
        while (entitySprite.color != Color.white)
        {
            entitySprite.color = Color.Lerp(Color.white, entitySprite.color, 0.96f);
            yield return new WaitForEndOfFrame();
        }
    }
    public DamageReport TakeDamageWithForce(DamageInfo damageInfo, Vector2 direction, float force)
    {
        transform.DOMove(transform.position + ((Vector3)direction * force / Mathf.Max(1, KnockBackResistance)), 0.1f);
        return TakeDamage(damageInfo);
    }

    public abstract void Move();
    public abstract void Die();

    #region Events
    public UnityAction<DamageInfo> OnAttacked;
    public UnityAction<DamageReport> OnTakeDamage;
    public UnityAction<DamageReport> OnKill;
    public UnityAction<DamageReport> OnDeath;
    #endregion
}
[System.Serializable]
public struct EntityStats
{
    public int health;
    public int damage;
    public float moveSpeed;
    public int defense;
    [Tooltip("0 for no resistance at all.")] public int knockBackResistance;
}