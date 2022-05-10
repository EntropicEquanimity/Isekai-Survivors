using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
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
        GameState = GameState.Lottery;
        _interfaceController.OpenChooseItemPanel(LootController.Instance.GetItems(LootChoices), delegate
        {
            playerExp -= ExpRequired;
            playerLevel++;
            _interfaceController.UpdatePlayerLevel(playerLevel);
            GameState = GameState.Normal;

            if (playerExp >= ExpRequired) { LevelUp(); }
        });
    }
    public void PlayerDeath()
    {
        GameState = GameState.GameOver;
        if (Revives > 0)
        {

        }
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
        InventoryController.Instance.AddEquipment(player.playerData.startingWeapon);
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
    public int Projectiles { get => Mathf.RoundToInt(projectiles.Value); }
    public int PierceCount { get => Mathf.RoundToInt(pierceCount.Value); }
    public float MoveSpeed { get => moveSpeed.Value; }
    public int Defense { get => Mathf.RoundToInt(projectiles.Value); }
    public int Health { get => Mathf.RoundToInt(health.Value); }

    public float PickupRadius { get => pickupRadius.Value; }

    public int LootChoices { get => Mathf.RoundToInt(lootChoices.Value); }
    public int Revives { get => Mathf.RoundToInt(revives.Value); }

    [Foldout("Stats")] public CharacterStat damage;
    [Foldout("Stats")] public CharacterStat knockBack;
    [Foldout("Stats")] public CharacterStat duration;
    [Foldout("Stats")] public CharacterStat size;
    [Foldout("Stats")] public CharacterStat speed;
    [Foldout("Stats")] public CharacterStat critChance;
    [Foldout("Stats")] public CharacterStat critDamage;
    [Foldout("Stats")] public CharacterStat projectiles;
    [Foldout("Stats")] public CharacterStat pierceCount;
    [Foldout("Stats")] public CharacterStat moveSpeed;
    [Foldout("Stats")] public CharacterStat defense;
    [Foldout("Stats")] public CharacterStat health;
    [Foldout("Stats")] public CharacterStat pickupRadius;

    [Foldout("Stats")] public CharacterStat lootChoices = new CharacterStat() { BaseValue = 1 };
    [Foldout("Stats")] public CharacterStat revives = new CharacterStat() { BaseValue = 0 };

    public void ResetGameStats()
    {
        damage = new CharacterStat() { BaseValue = settings.selectedPlayerCharacter.entityStats.damage };
        knockBack = new CharacterStat();
        duration = new CharacterStat();
        size = new CharacterStat();
        speed = new CharacterStat();
        critChance = new CharacterStat();
        critDamage = new CharacterStat();
        projectiles = new CharacterStat();
        pierceCount = new CharacterStat();
        moveSpeed = new CharacterStat();
        defense = new CharacterStat();
        health = new CharacterStat();
        pickupRadius = new CharacterStat() { BaseValue = settings.selectedPlayerCharacter.playerStats.pickupRadius };
    }
    #endregion

    #region Events 
    public UnityAction<ProjectileStats> OnProjectileDestroyed;
    #endregion
}
public enum GameState
{
    Normal,
    Paused,
    Lottery,
    GameOver
}