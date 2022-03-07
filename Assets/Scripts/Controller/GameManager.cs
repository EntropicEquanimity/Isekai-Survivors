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
    [BoxGroup("Current Session Stats")] [ReadOnly] public GameState gameState;

    [BoxGroup("Player")] [ReadOnly] public Player player;
    [BoxGroup("Player")] public GameObject selectedPlayerPrefab;

    [BoxGroup("Enemies")] public float enemyHealthScaling = 0.1f;

    [BoxGroup("Settings")] public AnimationCurve expCurve;

    private InterfaceController _interfaceController;
    public static GameManager Instance { get; private set; }

    #region Player
    public void LevelUp()
    {
        _interfaceController.OpenChooseItemPanel(LootController.Instance.GetItems(3), delegate
        {
            playerExp -= ExpRequired;
            playerLevel++;
            _interfaceController.UpdatePlayerLevel(playerLevel);

            if (playerExp >= ExpRequired) { LevelUp(); }
        });
    }
    public void PlayerDeath()
    {

    }
    #endregion

    #region Messages
    public void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Debug.LogWarning("Warning! Multiple Game Managers in scene!"); Destroy(gameObject); }

        player = Instantiate(selectedPlayerPrefab, Vector3.zero, Quaternion.identity).GetComponent<Player>();

        playerKills = 0;
        playerExp = 0;
        gameTime = 0;
        playerLevel = 0;
        gameState = GameState.Normal;

        _interfaceController = GetComponent<InterfaceController>();
        _interfaceController.UpdateExpBar(playerExp, ExpRequired);
        _interfaceController.UpdatePlayerLevel(0);
    }
    private void Start()
    {
        InventoryController.Instance.AddEquipment(player.playerData.startingWeapon);
    }
    public void Update()
    {
        if (gameState != GameState.Normal) { return; }

        if (Input.GetKey(KeyCode.Space))
        {
            Time.timeScale = 4f;
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
    public float Damage { get; set; }
    public float MoveSpeed { get; set; }
    public int Defense { get; set; }
    public int Projectiles { get; set; }
    public float PickupRadius { get; set; }
    #endregion

    #region Events 
    public UnityAction<ProjectileStats> OnProjectileDestroyed;
    #endregion
}
public enum GameState
{
    Normal,
    Paused,
    Lottery
}