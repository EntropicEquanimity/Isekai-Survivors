using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplodingEnemy : Enemy
{
    public GameObject explosionEffect;
    public float explosionRadius = 1;
    public int explosionDamage = 30;
    public override void Die()
    {
        base.Die();
        Instantiate(explosionEffect, transform.position, Quaternion.identity);
        Collider2D player = Physics2D.OverlapCircle(transform.position, explosionRadius, LayerMask.GetMask("Player"));
        if (player != null)
        {
            player.GetComponent<Player>().TakeDamage(new DamageInfo()
            {
                damage = explosionDamage,
                attacker = this,
            }
            );
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
