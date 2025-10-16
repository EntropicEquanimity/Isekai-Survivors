using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;

public class InventoryController : MonoBehaviour
{
    [BoxGroup("Settings")] public SessionSettingsSO settings;

    [BoxGroup("Inventory")] public GameObject itemSlotPrefab;
    [BoxGroup("Inventory")] public Transform weaponSlotsParent, equipmentSlotsParent;
    [BoxGroup("Inventory")] [ReadOnly] public List<ItemSlot> weaponItemSlots, toolItemSlots;
    [BoxGroup("Inventory")] [ReadOnly] public List<Equipment> equippedItems = new List<Equipment>();

    public UnityAction OnInventoryChange;

    public static InventoryController Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        weaponItemSlots = new List<ItemSlot>();
        toolItemSlots = new List<ItemSlot>();

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
            toolItemSlots.Add(itemSlot);
            itemSlot.transform.SetParent(equipmentSlotsParent);
            itemSlot.transform.localScale = Vector3.one;
        }
    }
    public void AddEquipment(Equipment equipment)
    {
        if (equipment.ItemType == ItemType.Weapon && MaxWeaponsEquipped()) { Destroy(equipment.gameObject); return; }

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

        if (equipment.ItemType == ItemType.Weapon && MaxWeaponsEquipped())
        {
            LootController.Instance.RemoveAllUnusedWeaponsFromPool();
        }
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
            for (int i = 0; i < toolItemSlots.Count; i++)
            {
                if (toolItemSlots[i].Empty)
                {
                    toolItemSlots[i].Initialize(equipment);
                    return;
                }
            }
            ItemSlot itemSlot = Instantiate(itemSlotPrefab).GetComponent<ItemSlot>();
            toolItemSlots.Add(itemSlot);
            itemSlot.Initialize(equipment);
            itemSlot.transform.SetParent(equipmentSlotsParent);
            itemSlot.transform.localScale = Vector3.one;
        }
    }
    #endregion

    #region Getters
    public int MaxWeapons => weaponItemSlots.Count;
    public int MaxTools => toolItemSlots.Count;
    public bool MaxWeaponsEquipped()
    {
        for (int i = 0; i < weaponItemSlots.Count; i++)
        {
            if (weaponItemSlots[i].Empty) { return false; }
        }
        return true;
    }
    public bool MaxToolsEquipped()
    {
        for (int i = 0; i < toolItemSlots.Count; i++)
        {
            if (toolItemSlots[i].Empty) { return false; }
        }
        return false;
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
