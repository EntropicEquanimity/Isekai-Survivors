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
    [BoxGroup("Inventory")] [ReadOnly] public List<Equipment> equippedWeapons {  get; private set; }
    [BoxGroup("Inventory")] [ReadOnly] public List<Equipment> equippedEquipment { get; private set; }
    [BoxGroup("Inventory")] [ReadOnly] public List<Equipment> equippedArtifacts { get; private set; }
    [BoxGroup("Inventory")] [ReadOnly] public List<Equipment> allEquippedItems { get; private set; }

    public UnityAction OnInventoryChange;

    public static InventoryController Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        weaponItemSlots = new List<ItemSlot>();
        toolItemSlots = new List<ItemSlot>();

        equippedWeapons = new List<Equipment>();
        equippedEquipment = new List<Equipment>(); 
        equippedArtifacts = new List<Equipment>();
        allEquippedItems = new List<Equipment>();

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
    #region Equipment
    public void AddItem(ItemSO item, ItemType type)
    {
        if (item == null) return;
        if (type == ItemType.Weapon) { AddWeapon(item); }
    }
    public void UpgradeWeapon(Equipment equipment, ItemStats upgradeStats)
    {
        if (!HasItem(equipment.itemData)){ Debug.LogError(equipment.itemData.name + " is no longer equipped!"); return; }

        equipment.Upgrade(upgradeStats);
    }
    public void AddWeapon(Equipment weapon)
    {
        if (weapon.ItemType == ItemType.Weapon && MaxWeaponsEquipped()) { Destroy(weapon.gameObject); return; }

        for (int i = 0; i < this.equippedWeapons.Count; i++)
        {
            if (this.equippedWeapons[i].Name == weapon.Name)
            {
                Destroy(weapon.gameObject);
                return;
            }
        }
        this.equippedWeapons.Add(weapon);
        allEquippedItems.Add(weapon);
        weapon.OnEquip();
        AddEquipmentUI(weapon);

        OnInventoryChange?.Invoke();

        if (weapon.ItemType == ItemType.Weapon && MaxWeaponsEquipped()) { LootController.Instance.RemoveEquipmentTypeFromList(ItemType.Weapon); }
    }
    public void AddWeapon(ItemSO item)
    {
        Equipment equipment = Instantiate(item.pickupablePrefab).GetComponent<Equipment>();
        equipment.GetComponent<EquipmentPickup>().OnPickup();
        AddWeapon(equipment);
    }
    public void RemoveWeapon(Equipment equipment)
    {
        if (equippedWeapons.Contains(equipment)) { equipment.UnEquip(); }
        else { Debug.Log("Player does not possess this piece of equipment!"); }
    }
    public bool HasItem(ItemSO item)
    {
        for (int i = 0; i < allEquippedItems.Count; i++)
        {
            if (allEquippedItems[i].itemData == item)
            {
                return true;
            }
        }
        return false;
    }
    public Equipment GetWeapon(ItemSO item)
    {
        for (int i = 0; i < equippedWeapons.Count; i++)
        {
            if (equippedWeapons[i].itemData == item)
            {
                return equippedWeapons[i];
            }
        }
        return null;
    }
    public void ButtonPress(ItemSO item, ItemStats upgradeStats = null)
    {
        if (HasItem(item)) { UpgradeWeapon(GetWeapon(item), upgradeStats); }
        else { AddWeapon(item); }
    }
    #endregion

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
        for (int i = 0; i < equippedWeapons.Count; i++)
        {
            equippedWeapons[i].OnEquip();
        }
    }
    public void FixedUpdate()
    {
        if(GameManager.Instance.GameState != GameState.Normal) { return; }
        for (int i = 0; i < equippedWeapons.Count; i++)
        {
            equippedWeapons[i].TickCooldown(Time.fixedDeltaTime);
        }
    }
}
