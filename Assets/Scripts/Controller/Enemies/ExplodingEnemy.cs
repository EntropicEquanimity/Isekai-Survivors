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

    public override void Die()
    {
        base.Die();

        GameManager.Instance.DelayedAction(0.25f, delegate 
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].CompareTag("Player") || hits[i].CompareTag("Enemy")) { hits[i].GetComponent<Entity>().TakeDamage(new DamageInfo() { damage = explosionDamage, attacker = this }); }
            }
        });
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
