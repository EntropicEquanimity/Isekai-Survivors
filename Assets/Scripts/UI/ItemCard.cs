using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class ItemCard : MonoBehaviour
{
    [BoxGroup("Required")] public Image itemImage;
    [BoxGroup("Required")] public Image itemTypeTint;
    [BoxGroup("Required")] public TMP_Text itemName;
    [BoxGroup("Required")] public TMP_Text itemDescription;
    [BoxGroup("Required")] public GraphicsSettingsSO graphicsSettings;

    [BoxGroup("ReadOnly")] [ReadOnly] public bool newItem = true;
    [BoxGroup("ReadOnly")] [ReadOnly] public ItemSO item;
    private System.Action OnClickEvent;

    public void OnSelectCard()
    {
        OnClickEvent?.Invoke();
        //InventoryController.Instance.ButtonPress(item);
        InterfaceController.Instance.CloseChooseItemPanel();
    }
    public void Initialize(ItemSO item, System.Action onClick)
    {
        newItem = true;
        this.item = item;
        OnClickEvent = onClick;
        ItemStats itemStats = new ItemStats();
        Rarity rarity = GameManager.Instance.LuckRoll;

        if (InventoryController.Instance.HasItem(item))
        {
            newItem = false;
            Equipment equipment = InventoryController.Instance.GetItem(item);
            itemStats = equipment.GetUpgradeStats(rarity, 2);
            itemDescription.text = equipment.BuildLevelUpStatsString(itemStats);
            OnClickEvent += () => equipment.Upgrade(itemStats);
        }
        itemImage.sprite = item.icon;
        //itemTypeTint.color = item.pickupablePrefab.GetComponent<Equipment>().ItemType == ItemType.Weapon ? graphicsSettings.weaponTint : graphicsSettings.equipmentTint;
        itemTypeTint.color = Item.GetColorFromRarity(rarity);
        itemName.text = item.name;

        if (newItem) 
        {
            itemDescription.text = item.itemDescription;
            itemTypeTint.color = Color.gray;
            OnClickEvent += ()=> InventoryController.Instance.AddEquipment(item);
        }

        GetComponent<Button>().onClick.AddListener(OnSelectCard);
    }
    private void OnDisable()
    {
        OnClickEvent = null;
        GetComponent<Button>().onClick.RemoveAllListeners();
    }
}
