using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(InterfaceController))]
public class GameManager : MonoBehaviour
{
    [BoxGroup("Current Session Stats")] [ReadOnly] public int playerLevel;
    [BoxGroup("Current Session Stats")] [ReadOnly] [SerializeField] protected int playerExp;
    [BoxGroup("Current Session Stats")] [ReadOnly] [SerializeField] protected int playerKills;
    [BoxGroup("Current Session Stats")] [ReadOnly] [SerializeField] protected int playerEssence;
    [BoxGroup("Current Session Stats")] [ReadOnly] public float gameTime = 0;
    [BoxGroup("Current Session Stats")] [ReadOnly] public GameState GameState;
    [BoxGroup("Current Session Stats")] [ReadOnly] public Player player;

    [BoxGroup("Settings")] public SessionSettingsSO settings;
    [BoxGroup("Settings")] public AnimationCurve expCurve;

    [BoxGroup("Enemies")] public float enemyHealthScaling = 0.1f;

    private InterfaceController _interfaceController;
    public static GameManager Instance { get; private set; }

    #region Player
    public void LevelUp()
    {
        if (playerExp < ExpRequired) { return; }
        GameState = GameState.Lottery;
        _interfaceController.OpenChooseItemPanel(LootController.Instance.GetItems(LootChoices), delegate
        {
            playerExp -= ExpRequired;
            playerLevel++;
            _interfaceController.UpdateExpBar(playerExp, ExpRequired);
            _interfaceController.UpdatePlayerLevel(playerLevel);
            GameState = GameState.Normal;
            foreach(var hit in Physics2D.OverlapCircleAll(player.transform.position, 2f, LayerMask.GetMask("Enemy"))) { hit.GetComponent<Entity>().AddKnockback(hit.transform.position - player.transform.position, 1f); }

            if (playerExp >= ExpRequired) { DelayedAction(0.1f, LevelUp); }
        });
    }
    #endregion

    #region Messages
    public void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Debug.LogWarning("Warning! Multiple Game Managers in scene!"); Destroy(gameObject); }

        player = Instantiate(settings.selectedPlayerCharacter.characterPrefab, Vector3.zero, Quaternion.identity).GetComponent<Player>();
        playerKills = 0;
        playerExp = 0;
        gameTime = 0;
        playerLevel = 0;
        GameState = GameState.Normal;

        _interfaceController = GetComponent<InterfaceController>();
        _interfaceController.UpdateExpBar(playerExp, ExpRequired);
        _interfaceController.UpdatePlayerLevel(0);

        ResetGameStats();
    }
    private void Start()
    {
        PlayerSO p = (PlayerSO)player.baseStats;
        InventoryController.Instance.AddEquipment(p.startingWeapon);
        ObjectPool.Instance.CreatePool("Barrier", Resources.Load("Barrier") as GameObject, 1);
    }
    public void Update()
    {
        if (GameState != GameState.Normal) { return; }

        if (Input.GetKey(KeyCode.LeftShift))
        {
            Time.timeScale = 2f;
        }
        else if (Time.timeScale > 0)
        {
            Time.timeScale = 1f;
        }
        gameTime += Time.deltaTime;
    }
    #endregion

    #region Mutators
    public int PlayerExperience
    {
        get => playerExp;
        set
        {
            playerExp = value;
            if (playerExp >= ExpRequired) { LevelUp(); }
            _interfaceController.UpdateExpBar(playerExp, ExpRequired);
        }
    }
    public int PlayerKills
    {
        get => playerKills;
        set
        {
            playerKills = value;
            _interfaceController.UpdateKills(playerKills);
        }
    }
    public int PlayerEssence { get => playerEssence; set => playerEssence += value; }
    public int ExpRequired => Mathf.RoundToInt(expCurve.Evaluate(playerLevel));
    #endregion

    #region Stats
    public int Damage { get => Mathf.RoundToInt(damage.Value); }
    public float KnockBack { get => knockBack.Value; }
    public float Duration { get => duration.Value; }
    public float Size { get => size.Value; }
    public float Speed { get => speed.Value; }
    public float CritChance { get => critChance.Value; }
    public float CritDamage { get => critDamage.Value; }
    public float Cooldown { get => cooldown.Value; }
    public int Projectiles { get => Mathf.RoundToInt(projectiles.Value); }
    public int Pierce { get => Mathf.RoundToInt(pierce.Value); }

    public float MoveSpeed { get => moveSpeed.Value; }
    public int Defense { get => Mathf.RoundToInt(defense.Value); }
    public int Health { get => Mathf.RoundToInt(health.Value); }
    public float Armor { get => Mathf.Max(0f, armor.Value); }
    public float Dodge { get => Mathf.Max(0f, dodge.Value); }

    public float DamageMultiplier { get => damageMultiplier.Value + 1f; }
    public float PickupRadius { get => Mathf.Max(0f, pickupRadius.Value); }
    public float XpGain { get => Mathf.Max(0f, pickupRadius.Value); }
    public float GoldGain { get => Mathf.Max(0f, pickupRadius.Value); }
    public float EssenceGain { get => Mathf.Max(0f, essenceGain.Value); }
    public int Luck { get => Mathf.RoundToInt(luck.Value); }

    public int LootChoices { get => Mathf.RoundToInt(lootChoices.Value); }
    public int LootRerolls { get => Mathf.RoundToInt(lootRerolls.Value); }
    public int LootBanishes { get => Mathf.RoundToInt(lootBanish.Value); }
    public int Revives { get => Mathf.RoundToInt(revives.Value); }

    [FoldoutGroup("Stats")] public EntityStat damage;
    [FoldoutGroup("Stats")] public EntityStat knockBack;
    [FoldoutGroup("Stats")] public EntityStat duration;
    [FoldoutGroup("Stats")] public EntityStat size;
    [FoldoutGroup("Stats")] public EntityStat speed;
    [FoldoutGroup("Stats")] public EntityStat critChance;
    [FoldoutGroup("Stats")] public EntityStat critDamage;
    [FoldoutGroup("Stats")] public EntityStat cooldown;
    [FoldoutGroup("Stats")] public EntityStat projectiles;
    [FoldoutGroup("Stats")] public EntityStat pierce;
    [FoldoutGroup("Stats")] public EntityStat moveSpeed;
    [FoldoutGroup("Stats")] public EntityStat defense;
    [FoldoutGroup("Stats")] public EntityStat health;
    [FoldoutGroup("Stats")] public EntityStat dodge;
    [FoldoutGroup("Stats")] public EntityStat armor;
    [FoldoutGroup("Stats")] public EntityStat damageMultiplier;

    [FoldoutGroup("Stats")] public EntityStat pickupRadius;
    [FoldoutGroup("Stats")] public EntityStat xpGain;
    [FoldoutGroup("Stats")] public EntityStat goldGain;
    [FoldoutGroup("Stats")] public EntityStat essenceGain;
    [FoldoutGroup("Stats")] public EntityStat luck;

    [FoldoutGroup("Stats")] public EntityStat lootChoices = new EntityStat() { BaseValue = 3 };
    [FoldoutGroup("Stats")] public EntityStat lootRerolls = new EntityStat() { BaseValue = 0 };
    [FoldoutGroup("Stats")] public EntityStat lootBanish = new EntityStat() { BaseValue = 0 };
    [FoldoutGroup("Stats")] public EntityStat revives = new EntityStat() { BaseValue = 0 };

    public Rarity LuckRoll => Item.GetRarityFromLuck(Luck);
    public void ResetGameStats()
    {
        PlayerStats playerStats = settings.selectedPlayerCharacter.playerStats;
        damage = new EntityStat();
        knockBack = new EntityStat();
        duration = new EntityStat();
        size = new EntityStat();
        speed = new EntityStat();
        critChance = new EntityStat();
        critDamage = new EntityStat();
        cooldown = new EntityStat();
        projectiles = new EntityStat();
        pierce = new EntityStat();

        moveSpeed = new EntityStat();
        defense = new EntityStat();
        health = new EntityStat();
        dodge = new EntityStat();

        pickupRadius = new EntityStat() { BaseValue = playerStats.pickupRadius };
        xpGain = new EntityStat() { BaseValue = playerStats.xpGain };
        goldGain = new EntityStat() { BaseValue = playerStats.goldGain };
        essenceGain = new EntityStat() { BaseValue = playerStats.essenceGain };
        luck = new EntityStat() { BaseValue = playerStats.luck };
    }
    #endregion

    #region Events 
    public UnityAction<ProjectileStats> OnProjectileDestroyed;
    #endregion

    IEnumerator DoWithDelay(float time, Action onComplete)
    {
        yield return new WaitForSeconds(time);
        onComplete.Invoke();
    }
    public void DelayedAction(float time, Action onComplete)
    {
        StartCoroutine(DoWithDelay(time, onComplete));
    }
}
public enum GameState
{
    Normal,
    Paused,
    Lottery,
    GameOver
}