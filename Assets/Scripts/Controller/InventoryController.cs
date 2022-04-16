using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using NaughtyAttributes;

public class InventoryController : MonoBehaviour
{
    [BoxGroup("Settings")] public SessionSettingsSO settings;

    [BoxGroup("Inventory")] public GameObject itemSlotPrefab;
    [BoxGroup("Inventory")] public Transform weaponSlotsParent, equipmentSlotsParent;
    [BoxGroup("Inventory")] public List<ItemSlot> weaponItemSlots, equipmentItemSlots;

    public UnityAction OnInventoryChange;

    [BoxGroup("UI")] public List<ItemSlot> itemSlots = new List<ItemSlot>();

    public List<Equipment> equippedItems = new List<Equipment>();
    public static InventoryController Instance;
    private void Awake()
    {
        if (Instance == null) Instance = this;

        weaponItemSlots = new List<ItemSlot>();
        equipmentItemSlots = new List<ItemSlot>();

        for (int i = 0; i < settings.selectedPlayerCharacter.playerStats.maxWeapons; i++)
        {
            ItemSlot itemSlot = Instantiate(itemSlotPrefab).GetComponent<ItemSlot>();
            weaponItemSlots.Add(itemSlot);
            itemSlot.transform.SetParent(weaponSlotsParent);
            itemSlot.transform.localScale = Vector3.one;
        }
        for (int i = 0; i < settings.selectedPlayerCharacter.playerStats.maxTools; i++)
        {
            ItemSlot itemSlot = Instantiate(itemSlotPrefab).GetComponent<ItemSlot>();
            equipmentItemSlots.Add(itemSlot);
            itemSlot.transform.SetParent(equipmentSlotsParent);
            itemSlot.transform.localScale = Vector3.one;
        }
    }
    public void AddEquipment(Equipment equipment)
    {
        for (int i = 0; i < this.equippedItems.Count; i++)
        {
            if (this.equippedItems[i].Name == equipment.Name)
            {
                this.equippedItems[i].Upgrade();
                Destroy(equipment.gameObject);
                return;
            }
        }
        this.equippedItems.Add(equipment);
        equipment.OnEquip();
        AddEquipmentUI(equipment);

        OnInventoryChange?.Invoke();
    }
    public void AddEquipment(ItemSO item)
    {
        Equipment equipment = Instantiate(item.pickupablePrefab).GetComponent<Equipment>();
        equipment.GetComponent<EquipmentPickup>().OnPickup();
        AddEquipment(equipment);
    }
    public void RemoveEquipment(Equipment equipment)
    {
        if (equippedItems.Contains(equipment))
        {
            equipment.UnEquip();
        }
        else
        {
            Debug.Log("Player does not possess this piece of equipment!");
        }
    }
    public bool HasItem(ItemSO item)
    {
        for (int i = 0; i < equippedItems.Count; i++)
        {
            if (equippedItems[i].Name == item.name)
            {
                return true;
            }
        }
        return false;
    }
    #region Inventory UI
    public void AddEquipmentUI(Equipment equipment)
    {
        if (equipment.ItemType == ItemType.Weapon)
        {
            for (int i = 0; i < weaponItemSlots.Count; i++)
            {
                if (weaponItemSlots[i].Empty)
                {
                    weaponItemSlots[i].Initialize(equipment);
                    return;
                }
            }
            ItemSlot itemSlot = Instantiate(itemSlotPrefab).GetComponent<ItemSlot>();
            weaponItemSlots.Add(itemSlot);
            itemSlot.Initialize(equipment);
            itemSlot.transform.SetParent(weaponSlotsParent);
            itemSlot.transform.localScale = Vector3.one;
        }
        else
        {
            for (int i = 0; i < equipmentItemSlots.Count; i++)
            {
                if (equipmentItemSlots[i].Empty)
                {
                    equipmentItemSlots[i].Initialize(equipment);
                    return;
                }
            }
            ItemSlot itemSlot = Instantiate(itemSlotPrefab).GetComponent<ItemSlot>();
            equipmentItemSlots.Add(itemSlot);
            itemSlot.Initialize(equipment);
            itemSlot.transform.SetParent(equipmentSlotsParent);
            itemSlot.transform.localScale = Vector3.one;
        }
    }
    #endregion
    private void Start()
    {
        for (int i = 0; i < equippedItems.Count; i++)
        {
            equippedItems[i].OnEquip();
        }
    }
    public void FixedUpdate()
    {
        for (int i = 0; i < equippedItems.Count; i++)
        {
            equippedItems[i].TickCooldown(Time.fixedDeltaTime);
        }
    }
}
