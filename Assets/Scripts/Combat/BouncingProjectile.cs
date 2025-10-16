using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class BouncingProjectile : Projectile
{
    [BoxGroup("Bounce")] public int pierceCountBeforeBouncing = 0;

    public override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);
        if (projectileStats.pierceCount - durabilityRemaining >= pierceCountBeforeBouncing + 1)
        {
            Vector2 dir = (transform.position - collision.transform.position).normalized;
            if (_rb != null)
            {
                _rb.linearVelocity = (dir * projectileStats.weaponStats.speed);
                transform.up = dir;
            }
        }
    }
}
