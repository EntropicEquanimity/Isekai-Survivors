using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using DG.Tweening;

public class Projectile : MonoBehaviour
{
    [ReadOnly] public ProjectileStats projectileStats;
    [ReadOnly] public List<Enemy> targets;
    [ReadOnly] public Equipment damageSource;
    [ReadOnly] public int durabilityRemaining;
    [ReadOnly] public float durationRemaining;
    public LayerMask enemyLayer;
    public ClearTargetsFlag clearTargets;
    public float rotationSpeed = -360;
    public bool rotatesOnAxis = false;
    public bool rotationSpeedAffectedByWeaponSpeed = false;
    public float constantDamageIntervals = 0.5f;
    protected Tween _rotateTween;
    protected Rigidbody2D _rb;
    private float _constantDamageCooldown;

    public virtual void Initialize(ProjectileStats projectileStats, Equipment damageSource)
    {
        _rb = GetComponent<Rigidbody2D>();
        this.projectileStats = projectileStats;
        this.damageSource = damageSource;
        durationRemaining = projectileStats.weaponStats.duration;
        durabilityRemaining = projectileStats.pierceCount;

        if (!rotatesOnAxis)
        {
            transform.up = projectileStats.direction;
        }
        if (_rb != null && projectileStats.direction != Vector2.zero)
        {
            _rb.AddForce(projectileStats.direction * projectileStats.weaponStats.speed, ForceMode2D.Impulse);
        }
    }
    public void ShowDamageNumber(int damage, bool crit, Vector2 pos)
    {
        NumberPopupManager.Instance.DamageNumber(damage, crit, pos);
    }
    public virtual void FixedUpdate()
    {
        if (durationRemaining > 0f) { durationRemaining -= Time.fixedDeltaTime; }
        if (durationRemaining <= 0f) { gameObject.SetActive(false); }

        if (rotatesOnAxis)
        {
            float speed = rotationSpeedAffectedByWeaponSpeed == true ? rotationSpeed * projectileStats.weaponStats.speed : rotationSpeed;
            transform.RotateAround(transform.position, Vector3.forward, speed * Time.fixedDeltaTime);
            //_rotateTween = transform.DORotate(new Vector3(0, 0, speed), 1f).SetEase(Ease.Linear).SetLoops(-1, LoopType.Incremental);
            //if (clearTargets == ClearTargetsFlag.OnRotate360) { _rotateTween.onStepComplete += delegate { targets.Clear(); }; }
        }
        if (projectileStats.constantDamage)
        {
            if (_constantDamageCooldown >= 0f) { _constantDamageCooldown -= Time.fixedDeltaTime; }

            if (_constantDamageCooldown <= 0f)
            {
                List<Collider2D> targets = new List<Collider2D>();
                ContactFilter2D filter = new ContactFilter2D();
                filter.layerMask = enemyLayer;
                GetComponent<Collider2D>().OverlapCollider(filter, targets);

                foreach (var target in targets)
                {
                    Enemy enemy = target.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        DamageReport report = enemy.TakeDamageWithForce(new DamageInfo()
                        {
                            attacker = GameManager.Instance.player,
                            damage = Mathf.RoundToInt(projectileStats.weaponStats.damage),
                            critChance = projectileStats.weaponStats.critChance
                        }, (enemy.transform.position - damageSource.transform.position).normalized, projectileStats.weaponStats.knockBack);
                        damageSource.DamageRecord.AddStats(report);
                        if (report.victim != null)
                        {
                            ShowDamageNumber(report.damageDealt, report.crit, report.victim.transform.position);
                        }
                    }
                }
                _constantDamageCooldown = constantDamageIntervals;
            }
        }
        if (_rb.velocity.magnitude <= 0.1f && clearTargets == ClearTargetsFlag.OnBoomerangReturn) { targets.Clear(); }
    }
    public virtual void OnDisable()
    {
        if (_rotateTween != null) { DOTween.Kill(_rotateTween); }
        targets.Clear();
        transform.rotation = Quaternion.Euler(0, 0, 0);
    }
    private void OnBecameInvisible()
    {
        gameObject.SetActive(false);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy != null)
        {
            durabilityRemaining--;
            if (targets.Contains(enemy)) { return; }
            if (clearTargets != ClearTargetsFlag.Always) { targets.Add(enemy); }
            DamageReport report = enemy.TakeDamageWithForce(new DamageInfo()
            {
                attacker = GameManager.Instance.player,
                damage = Mathf.RoundToInt(projectileStats.weaponStats.damage),
                critChance = projectileStats.weaponStats.critChance
            }, (collision.transform.position - damageSource.transform.position).normalized, projectileStats.weaponStats.knockBack);

            if (report.victim != null)
            {
                ShowDamageNumber(report.damageDealt, report.crit, report.victim.transform.position);
            }
            damageSource.DamageRecord.AddStats(report);
        }
        if (durabilityRemaining <= 0) { GameManager.Instance.OnProjectileDestroyed?.Invoke(projectileStats); gameObject.SetActive(false); }
    }
}
public struct ProjectileStats
{
    public ItemStats weaponStats;
    public Vector2 direction;
    public int pierceCount;
    public bool bounces;
    public bool constantDamage;

    public ProjectileStats(ItemStats weaponStats, Vector2 direction, int pierceCount, bool bounces, bool constantDamage)
    {
        this.weaponStats = weaponStats;
        this.direction = direction;
        this.pierceCount = pierceCount;
        this.bounces = bounces;
        this.constantDamage = constantDamage;
    }
}
public enum ClearTargetsFlag
{
    Never,
    Always,
    OnRotate360,
    OnBounce,
    OnBoomerangReturn
}