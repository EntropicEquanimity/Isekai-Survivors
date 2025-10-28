using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ExplodingEnemy : Enemy
{
    public GameObject explosionEffect;
    public float explosionRadius = 1;
    public int explosionDamage = 30;
    public bool explodeWhenWithinRange = false;
    public float triggerRadiusForExplosion = 0.5f;
    bool _startingExplosion = false;

    public override void Die()
    {
        if (_startingExplosion) { return; }
        _startingExplosion = true;
        base.Die();
        GameManager.Instance.DelayedAction(0.25f, () => Explode(transform.position, new DamageInfo(this, explosionDamage, 0, true)));
    }

    public override void Initialize(EntityStats entityStats)
    {
        base.Initialize(entityStats);
        _startingExplosion = false;
    }

    public void Explode(Vector2 location, DamageInfo damageInfo)
    {
        GameObject go = Instantiate(explosionEffect, location, Quaternion.identity);
        go.transform.localScale = Vector3.one * (explosionRadius + 0.5f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(location, explosionRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].CompareTag("Player") || hits[i].CompareTag("Enemy")) 
            {
                hits[i].GetComponent<Entity>().TakeDamage(damageInfo);
                //Debug.Log(report.victim + " taking " + report.damageDealt + " from " + report.attacker);
            }
        }
    }
    private void Update()
    {
        if(IsDead) return;
        Collider2D player = Physics2D.OverlapCircle(transform.position, triggerRadiusForExplosion, LayerMask.GetMask("Player"));
        if (player != null)
        {
            MoveSpeed = 0;
            transform.DOScale(transform.localScale * 1.2f, 0.4f);
            GameManager.Instance.DelayedAction(0.3f, () => Die());
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
