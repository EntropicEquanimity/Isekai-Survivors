using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lantern : Equipment
{
    [SerializeField] [ReadOnly] private Projectile _lanternLight;

    [MinValue(0.5f)] public float rotationSpeed = 1f;


    public override string Name => "Lantern";

    public override ItemType ItemType => ItemType.Weapon;

    protected override string EffectPrefabName => "Lantern_Light";

    public override List<ItemUpgradeStats> ItemUpgrades => new List<ItemUpgradeStats>()
    {
        new(ItemStatType.Damage, Rarity.Common, 100, 1.5f),
        new(ItemStatType.Size, Rarity.Common, 100, 0.1f),
        new(ItemStatType.Knockback, Rarity.Rare, 25, 0.1f),
    };

    public override float BaseCooldown => throw new System.NotImplementedException();

    public override void OnEquip()
    {
        RecalculateItemStats();
        SpawnLanternLight();
    }
    public override void StopItem()
    {
        _lanternLight.gameObject.SetActive(false);
    }
    public override void UseItem()
    {
        SpawnLanternLight();
    }
    public override void TickCooldown(float time)
    {

    }
    private void SpawnLanternLight()
    {
        if (_lanternLight != null) { _lanternLight.gameObject.SetActive(false); _lanternLight = null; }
        Projectile projectile = GetPrefab().GetComponent<Projectile>();
        projectile.transform.SetParent(GameManager.Instance.player.transform);
        projectile.transform.localPosition = Vector3.zero;
        projectile.transform.localScale = Vector3.one * Size;
        projectile.Initialize(new ProjectileStats(GetEquipmentStats(), Vector3.zero, true), this);
        _lanternLight = projectile;
    }

    private Vector2 direction = Vector2.zero;
    private void LateUpdate()
    {
        if (_lanternLight != null)
        {
            if (GameManager.Instance.player.moveVector != Vector2.zero)
            {
                direction = GameManager.Instance.player.moveVector;
            }
        }
    }
    private void Update()
    {
        if (_lanternLight != null)
        {
            _lanternLight.transform.rotation = Quaternion.Slerp(_lanternLight.transform.rotation, Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90), Time.deltaTime * rotationSpeed * Speed);
        }
    }
}
