using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : Entity
{
    private Rigidbody2D _rb;
    private InterfaceController _hud;
    [SerializeField] SimpleSpriteAnimator _animator;
    [BoxGroup("ReadOnly")] [ReadOnly] public Vector2 moveVector;
    private Vector2 lastMoveVector = Vector2.zero;
    private PlayerSO playerStats;

    public override int MaxHP { get => base.MaxHP + GameManager.Instance.Health; set => base.MaxHP = value; }
    public override int Damage { get => base.Damage + GameManager.Instance.Damage; set => base.Damage = value; }
    public override int Defense { get => base.Defense + GameManager.Instance.Defense; set => base.Defense = value; }
    public override float MoveSpeed { get => base.MoveSpeed + GameManager.Instance.MoveSpeed; set => base.MoveSpeed = value; }
    public override float KnockBackResistance { get => base.KnockBackResistance + GameManager.Instance.KnockBack; set => base.KnockBackResistance = value; }

    public virtual void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _hud = GameManager.Instance.GetComponent<InterfaceController>();
        Initialize(baseStats.entityStats);
        HP = baseStats.entityStats.health;
        GetComponent<CircleCollider2D>().enabled = true;
        OnDeath += DisableWeapons;

        playerStats = (PlayerSO)baseStats;
        _animator.animationSprites = playerStats.idleAnimation;
        _animator.Restart();
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
    void DisableWeapons(DamageReport report)
    {
        foreach (Equipment weapon in InventoryController.Instance.equippedWeapons) 
        {
            if (weapon != null) { weapon.enabled = false; }
            else { Debug.LogError("Trying to disable missing weapon!"); }
        }
    }
    public override void Move()
    {
        if (moveVector != Vector2.zero && _rb.linearVelocity.magnitude < 0.5f)
        {
                _animator.animationSprites = playerStats.walkingAnimation;

            _rb.MovePosition(moveVector * (MoveSpeed) * Time.fixedDeltaTime + (Vector2)transform.position);
            if (moveVector.x != 0) entitySpriteRenderer.flipX = moveVector.x < 0;

            lastMoveVector = moveVector;
        }
        else if (lastMoveVector != moveVector)
        {
            _animator.animationSprites = playerStats.idleAnimation;
            _animator.Restart();
        }
    }
    public override void Die()
    {
        CanTakeDamage = false;
        GetComponent<CircleCollider2D>().enabled = false;
        MoveSpeed = 0;
        Damage = 0;
        HP = 0;

        _hud.ShowGameOver(InventoryController.Instance.equippedWeapons, 0f);
        GameManager.Instance.GameOver();
    }
    protected override IEnumerator DeathAnimation()
    {
        CanTakeDamage = false;
        GetComponent<CircleCollider2D>().enabled = false;
        MoveSpeed = 0;
        Damage = 0;
        HP = 0;
        yield return new WaitForFixedUpdate();
    }
}
[System.Serializable]
public class PlayerStats
{
    public float accuracy = 0;
    public float pickupRadius = 1;

    public float essenceGain = 1;
    public float goldGain = 1;
    public float xpGain = 1;
    public float luck = 0;

    public int maxWeapons = 5;
    public int maxTools = 5;
}