using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine.Events;

public abstract class Entity : MonoBehaviour
{
    [BoxGroup("Entity")] public EntitySO baseStats;
    [BoxGroup("Entity")] [Required] public SpriteRenderer entitySpriteRenderer;
    [BoxGroup("Entity")] public SettingsSO settings;

    #region Stats
    public virtual int MaxHP { get; set; }
    public int HP { get; set; }
    public virtual int Damage { get; set; }
    public virtual float MoveSpeed { get; set; }
    public virtual int Defense { get; set; }
    public virtual float KnockBackResistance { get; set; }
    public int Revives { get; protected set; }
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
        Revives = 0;

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
                damageReport.attacker?.OnKill?.Invoke(damageReport);
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
        AddKnockback(direction, force);
        return TakeDamage(damageInfo);
    }
    public void AddKnockback(Vector2 direction, float force, bool ignoreKnockbackResistance = false)
    {
        if (ignoreKnockbackResistance) { transform.DOMove(transform.position + ((Vector3)direction * force), 0.1f); }
        else { transform.DOMove(transform.position + ((Vector3)direction * force / (KnockBackResistance + 1f)), 0.1f); }
    }
    public virtual void Revive()
    {
        Initialize(baseStats.entityStats);
        HP = Mathf.RoundToInt(MaxHP * 0.2f);
        GetComponent<CircleCollider2D>().enabled = true;
    }
    #region Invulnerability
    private const string BarrierEffectPath = "Barrier";
    private Coroutine _invulnerabilityCoroutine;
    float _invulnDuration = 0f;
    public virtual void Invulnerable(float duration)
    {
        if (_invulnerabilityCoroutine == null) {
            _invulnerabilityCoroutine = StartCoroutine(InvulnerableCoroutine(duration));
        }
        else if(_invulnDuration < duration){
            _invulnDuration = duration;
        }

        IEnumerator InvulnerableCoroutine(float duration)
        {
            SpriteRenderer sr = ObjectPool.Instance.Spawn("Barrier", transform.position, Quaternion.identity, transform).GetComponent<SpriteRenderer>();
            CanTakeDamage = false;
            _invulnDuration = duration;
            while (_invulnDuration > 0f) 
            {
                _invulnDuration -= Time.deltaTime;
                Color color = sr.color;
                color = new Color(color.r, color.g, color.b, Mathf.Min(0.5f, _invulnDuration / 3f));
                sr.color = color;
                
                yield return null;
            }
            ObjectPool.Instance.Despawn(sr.gameObject);
            CanTakeDamage = true;
            _invulnerabilityCoroutine = null;
        }
    }
    #endregion
    public abstract void Move();
    public abstract void Die();
    protected virtual IEnumerator DeathAnimation()
    {
        CanTakeDamage = false;
        MoveSpeed = 0;
        Damage = 0;
        HP = 0;
        _initialized = false;
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
    public int dodgeChance;

    [Tooltip("0 for no resistance at all.")] public int knockBackResistance;
}