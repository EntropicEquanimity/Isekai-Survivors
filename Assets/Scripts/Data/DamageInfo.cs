using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageInfo
{
    public Entity attacker;
    public int damage;
    public float critChance;
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