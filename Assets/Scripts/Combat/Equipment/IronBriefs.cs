using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IronBriefs : Equipment
{
    public Material invulnerabilityMaterial;
    public Material defaultMaterial;
    [ReadOnly] public bool equipped = false;
    private Player player;
    public override string Name => "Iron Briefs";

    public override ItemType ItemType => ItemType.Tool;

    protected override string EffectPrefabName => throw new System.NotImplementedException();

    public override List<ItemStats> UpgradeValues => new List<ItemStats>
    {
        new ItemStats(){ cooldown = -0.25f},
        new ItemStats(){ duration = 0.05f},
        new ItemStats(){ cooldown = -0.25f},
        new ItemStats(){ duration = 0.05f},
        new ItemStats(){ cooldown = -0.25f},
        new ItemStats(){ duration = 0.05f},
        new ItemStats(){ cooldown = -0.25f},
        new ItemStats(){ duration = 0.1f, cooldown = -0.5f}
    };

    public override void TickCooldown(float time)
    {
        if (CurrentCooldown > 0f) { CurrentCooldown -= time; }
    }
    public override void OnEquip()
    {
        player = GameManager.Instance.player;
        player.OnAttacked += TryToTriggerInvulnerability;
    }
    private void TryToTriggerInvulnerability(DamageInfo damageInfo)
    {
        if (damageInfo.damage > 0 && CurrentCooldown <= 0f)
        {
            StartCoroutine(HandleInvulnerability(itemData.itemStats.duration));
            CurrentCooldown = Cooldown;
        }
    }
    private IEnumerator HandleInvulnerability(float duration)
    {
        player.CanTakeDamage = false;
        player.entitySpriteRenderer.material = invulnerabilityMaterial;
        yield return new WaitForSeconds(duration);
        player.entitySpriteRenderer.material = defaultMaterial;
        player.CanTakeDamage = true;
    }
    public override void UseItem()
    {

    }

    public override void StopItem()
    {

    }
}
