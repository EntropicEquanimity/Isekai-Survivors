using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : Entity
{
    public PlayerSO playerData;
    private Rigidbody2D _rb;
    private InterfaceController _hud;
    [BoxGroup("ReadOnly")] [ReadOnly] public Vector2 moveVector;

    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _hud = GameManager.Instance.GetComponent<InterfaceController>();
        Initialize(playerData.entityStats);
    }
    void Update()
    {
        moveVector = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
    }
    private void FixedUpdate()
    {
        Move();

        _hud.UpdateHealthBar(HP, MaxHP);
    }

    public override void Move()
    {
        if (moveVector != Vector2.zero && _rb.velocity.magnitude < 0.5f)
        {
            _rb.MovePosition(moveVector * MoveSpeed * Time.fixedDeltaTime + (Vector2)transform.position);
            if (moveVector.x != 0) entitySprite.flipX = moveVector.x < 0;
        }
    }
    public override void Die()
    {
        CanTakeDamage = false;
        throw new System.NotImplementedException();
    }
}
[System.Serializable]
public class PlayerStats
{
    public ItemStats baseWeaponStats;
    public float pickupRadius = 1f;
    public float essenceGain = 0;
    public float goldGain = 0;
}