using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageInfo
{
    public Entity attacker;
    public int damage;
    public float critChance;
    public bool unblockable;

    public DamageInfo(Entity attacker, int damage, float critChance, bool unblockable)
    {
        this.attacker = attacker;
        this.damage = damage;
        this.critChance = critChance;
        this.unblockable = unblockable;
    }
    public DamageInfo(int damage, bool unblockable = false) 
    {
        this.damage = damage;
        attacker = null;
        critChance = 0;
        this.unblockable = unblockable;
    }
}
public struct DamageReport
{
    public Entity attacker;
    public Entity victim;
    public int damageDealt;
    public bool crit;
    public int damageBlocked;
    public bool isDead;
}