using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LootController : MonoBehaviour
{
    public static LootController Instance;

    [BoxGroup("Resources")] public GameObject experiencePickup, essencePickup;
    [BoxGroup("Resources")] [ReadOnly] public List<ExpPickup> expPickups = new List<ExpPickup>();
    [BoxGroup("Resources")] [ReadOnly] public List<ExpPickup> essencePickups = new List<ExpPickup>();

    [BoxGroup("Items")] public Catalogue allItems;
    [BoxGroup("Items")] [ReadOnly] public List<ItemSO> itemsInPool = new List<ItemSO>();

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            itemsInPool = new List<ItemSO>();
        }
    }
    private void Start()
    {
        itemsInPool.AddRange(allItems.weapons);
    }
    public void SpawnExperience(int expAmount, Vector2 position)
    {
        for (int i = 0; i < expPickups.Count; i++)
        {
            if (!expPickups[i].gameObject.activeInHierarchy)
            {
                expPickups[i].expAmount = expAmount;
                expPickups[i].transform.position = position;
                expPickups[i].Initialize();
                expPickups[i].gameObject.SetActive(true);
                return;
            }
        }
        GameObject obj = Instantiate(experiencePickup, position, Quaternion.identity);
        ExpPickup expPickup = obj.GetComponent<ExpPickup>();
        expPickup.expAmount = expAmount;
        expPickups.Add(expPickup);
    }
    public void SpawnEssence()
    {

    }
    public void RemoveItemFromPool(ItemSO item)
    {
        if (itemsInPool.Contains(item))
        {
            itemsInPool.Remove(item);
            InventoryController.Instance.AddEquipment(item);
        }
    }
    public List<ItemSO> GetItems(int numberToPull)
    {
        Debug.Log(numberToPull);
        if (numberToPull >= itemsInPool.Count) { return new List<ItemSO>(itemsInPool); }

        List<ItemSO> _allItems = new List<ItemSO>(itemsInPool);
        List<ItemSO> selectedItems = new List<ItemSO>();

        for (int n = 0; n < numberToPull; n++)
        {
            int totalWeight = GetItemWeight(_allItems);
            int roll = Random.Range(0, totalWeight);
            for (int i = 0; i < _allItems.Count; i++)
            {
                roll -= _allItems[i].dropWeight;
                if (roll < 0)
                {
                    selectedItems.Add(_allItems[i]);
                    _allItems.RemoveAt(i);
                    break;
                }
            }
        }
        //for (int i = 0; i < numberToPull; i++)
        //{
        //    int index = Random.Range(0, _allItems.Count);
        //    selectedItems.Add(_allItems[index]);
        //    _allItems.RemoveAt(index);
        //}
        return selectedItems;
    }
    public int GetItemWeight(List<ItemSO> items)
    {
        int totalWeight = 0;
        for (int i = 0; i < items.Count; i++)
        {
            totalWeight += items[i].dropWeight;
        }
        return totalWeight;
    }
    [Button]
    public void AttractEverything()
    {
        Player player = GameManager.Instance.player;
        for (int i = 0; i < expPickups.Count; i++)
        {
            if (expPickups[i].gameObject.activeInHierarchy)
            {
                expPickups[i].target = player;
            }
        }
    }
}