using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
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
        InventoryController.Instance.AddEquipment(item);
        InterfaceController.Instance.CloseChooseItemPanel();
    }
    public void Initialize(ItemSO item, System.Action onClick)
    {
        newItem = true;
        this.item = item;
        OnClickEvent = onClick;
        for (int i = 0; i < InventoryController.Instance.equippedItems.Count; i++)
        {
            if (InventoryController.Instance.equippedItems[i].itemData == item)
            {
                newItem = false;
            }
        }
        itemImage.sprite = item.icon;
        itemTypeTint.color = item.pickupablePrefab.GetComponent<Equipment>().ItemType == ItemType.Weapon ? graphicsSettings.weaponTint : graphicsSettings.equipmentTint;
        itemName.text = item.name;
        itemDescription.text = item.itemDescription;

        GetComponent<Button>().onClick.AddListener(OnSelectCard);
    }
    private void OnDisable()
    {
        OnClickEvent = null;
        GetComponent<Button>().onClick.RemoveAllListeners();
    }
}
