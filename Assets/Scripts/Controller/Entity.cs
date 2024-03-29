using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine.Events;

public abstract class Entity : MonoBehaviour
{
    [BoxGroup("Entity")] [Expandable] public EntitySO baseStats;
    [BoxGroup("Entity")] [Required] public SpriteRenderer entitySpriteRenderer;
    [BoxGroup("Entity")] public SettingsSO settings;

    #region Stats
    public virtual int MaxHP { get; set; }
    public int HP { get; set; }
    public virtual int Damage { get; set; }
    public virtual float MoveSpeed { get; set; }
    public virtual int Defense { get; set; }
    public virtual float KnockBackResistance { get; set; }
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
            entitySpriteRenderer.color = Color.white;
        }
    }
    public virtual void Initialize(EntityStats entityStats)
    {
        MaxHP = entityStats.health;
        HP = MaxHP;
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
        damage = Mathf.Max(0, damage);
        HP -= damage;

        if (settings.colorFlashOnTakeDamage)
        {
            if (_damageColorCoroutine != null) { StopCoroutine(_damageColorCoroutine); }
            _damageColorCoroutine = StartCoroutine(TakeDamageColor());
        }

        DamageReport damageReport = new DamageReport()
        {
            attacker = damageInfo.attacker,
            victim = this,
            crit = isCrit,
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
        entitySpriteRenderer.color = settings.takeDamageColor;
        while (entitySpriteRenderer.color != Color.white)
        {
            entitySpriteRenderer.color = Color.Lerp(Color.white, entitySpriteRenderer.color, 0.975f);
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
    protected virtual IEnumerator DeathAnimation()
    {
        MoveSpeed = 0;
        Damage = 0;
        HP = 0;
        float fadeAmount = -0.15f;
        while (fadeAmount < 1f)
        {
            Debug.Log(fadeAmount);
            fadeAmount += Time.fixedDeltaTime * 2f;
            entitySpriteRenderer.material.SetFloat("_FadeAmount", fadeAmount);
            yield return new WaitForFixedUpdate();
        }
        gameObject.SetActive(false);
    }

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