using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class EquipmentPickup : Pickup
{
    private SpriteRenderer _sr;
    private Equipment _equipment;
    private CircleCollider2D _collider;
    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _equipment = GetComponent<Equipment>();
        _collider = GetComponent<CircleCollider2D>();
    }
    public override void Initialize()
    {
        _sr.sprite = item.icon;
        _collider.enabled = true;
        _sr.enabled = true;
    }

    public override void OnPickup()
    {
        transform.SetParent(GameManager.Instance.player.transform);
        transform.localPosition = Vector3.zero;
        _sr.enabled = false;
        _collider.enabled = false;
        this.enabled = false;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            OnTrigger(collision.GetComponent<Player>());
        }
    }

    public override void OnTrigger(Player player)
    {
        Debug.Log("Picking up: " + _equipment);
        InterfaceController.Instance.OpenChooseItemPanel(new List<ItemSO>() { _equipment.itemData });
        Destroy(gameObject);
    }
}
