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

    public override int MaxHP { get => base.MaxHP + GameManager.Instance.Health; set => base.MaxHP = value; }
    public override int Damage { get => base.Damage + GameManager.Instance.Damage; set => base.Damage = value; }
    public override int Defense { get => base.Defense + GameManager.Instance.Defense; set => base.Defense = value; }
    public override float MoveSpeed { get => base.MoveSpeed + GameManager.Instance.MoveSpeed; set => base.MoveSpeed = value; }
    public override float KnockBackResistance { get => base.KnockBackResistance + GameManager.Instance.KnockBack; set => base.KnockBackResistance = value; }

    public virtual void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _hud = GameManager.Instance.GetComponent<InterfaceController>();
        Initialize(playerData.entityStats);
    }
    public virtual void Update()
    {
        moveVector = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
    }
    public virtual void FixedUpdate()
    {
        Move();

        _hud.UpdateHealthBar(HP, MaxHP);
    }
    public override void Move()
    {
        if (moveVector != Vector2.zero && _rb.velocity.magnitude < 0.5f)
        {
            _rb.MovePosition(moveVector * (MoveSpeed + GameManager.Instance.MoveSpeed) * Time.fixedDeltaTime + (Vector2)transform.position);
            if (moveVector.x != 0) entitySpriteRenderer.flipX = moveVector.x < 0;
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