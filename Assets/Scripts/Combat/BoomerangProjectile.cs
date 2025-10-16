using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoomerangProjectile : Projectile
{
    private Vector2 startingPosition;
    private Player player;
    public TrailRenderer trailRenderer;
    [ReadOnly] public bool isReturning;
    public override void Initialize(ProjectileStats projectileStats, Equipment damageSource)
    {
        base.Initialize(projectileStats, damageSource);
        startingPosition = transform.position;
        player = GameManager.Instance.player;
        trailRenderer.Clear();
    }
    public override void FixedUpdate()
    {
        base.FixedUpdate();
        _rb.linearDamping = Vector2.Distance(transform.position, player.transform.position) / 100f;

        _rb.AddForce((player.transform.position - transform.position).normalized * projectileStats.weaponStats.speed);
    }
}
