#if UNITY_EDITOR
using System;
using System.Collections;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum TrailerPhase
{
    Idle,
    Warmup,
    AwaitingCamera,
    Rolling,
    Still,
    Cut,
    Done,
    Failed
}

public sealed class TrailerClip
{
    public string Id;
    public float Seconds;
    public int LevelUps;
    public bool Stand;
    public bool Still;
}

public sealed class IsekaiTrailerPilot : MonoBehaviour, IMoveInput
{
    #region Fields
    public static readonly TrailerClip[] Clips =
    {
        new TrailerClip { Id = "isekai_knight_late", Seconds = 30f, LevelUps = 1 },
        new TrailerClip { Id = "isekai_knight_surrounded", Seconds = 14f, Stand = true, Still = true }
    };
    static readonly string[] Loadout =
    {
        "Assets/Data Objects/Weapons/Rapier.asset",
        "Assets/Data Objects/Weapons/Pirate Cutlass.asset",
        "Assets/Data Objects/Weapons/Throwing Axe.asset"
    };
    const int WarmupLevelUps = 14;
    const float WarmupSeconds = 25f;
    const float KiteRadius = 5f;
    const float CardHover = 0.45f;
    const float PanelTimeout = 3f;
    GameManager _game;
    Player _player;
    InterfaceController _hud;
    bool _stand;
    bool _picking;
    float _turn = 1f;
    float _clock;
    float _nextFlip = 7f;
    readonly System.Collections.Generic.List<ItemSO> _loadout = new System.Collections.Generic.List<ItemSO>();
    public TrailerPhase Phase { get; private set; }
    public TrailerClip Current { get; private set; }
    public string Failure { get; private set; }
    public bool CameraRolling { get; set; }
    public bool StillTaken { get; set; }
    #endregion

    #region Unity
    void Update()
    {
        _clock += Time.deltaTime;
        if (_clock > _nextFlip)
        {
            _turn = -_turn;
            _nextFlip = _clock + UnityEngine.Random.Range(5f, 9f);
        }
        if (_player != null) _player.CanTakeDamage = false;
        if (_hud != null && !_picking && _hud.itemChoicePanelParent.gameObject.activeSelf) StartCoroutine(PickCard(Phase == TrailerPhase.Rolling));
    }
    #endregion

    #region Public Methods
    public void Begin()
    {
        Phase = TrailerPhase.Warmup;
        StartCoroutine(Run());
    }
    public Vector2 ReadMove()
    {
        if (_stand || _player == null) return Vector2.zero;
        Vector2 position = _player.transform.position;
        if (position.sqrMagnitude < 0.01f) return Vector2.right;
        Vector2 outward = position.normalized;
        Vector2 tangent = new Vector2(-outward.y, outward.x) * _turn;
        float radius = KiteRadius + 2f * Mathf.Sin(_clock * 0.37f);
        float correction = Mathf.Clamp((radius - position.magnitude) / radius, -1f, 1f);
        Vector2 drift = new Vector2(Mathf.PerlinNoise(_clock * 0.3f, 0f) - 0.5f, Mathf.PerlinNoise(0f, _clock * 0.3f) - 0.5f);
        return tangent + outward * correction * 1.4f + drift;
    }
    #endregion

    #region Private Methods
    IEnumerator Run()
    {
        _game = GameManager.Instance;
        _hud = InterfaceController.Instance;
        _player = _game != null ? _game.player : null;
        if (_player == null || _hud == null || EnemyWaveManager.Instance == null)
        {
            Fail("Game scene is not ready.");
            yield break;
        }
        _player.MoveInput = this;
        for (int i = 0; i < Loadout.Length; i++)
        {
            ItemSO item = AssetDatabase.LoadAssetAtPath<ItemSO>(Loadout[i]);
            if (item == null)
            {
                Fail("Missing loadout item " + Loadout[i]);
                yield break;
            }
            _loadout.Add(item);
            if (!InventoryController.Instance.HasItem(item)) InventoryController.Instance.AddWeapon(item);
        }
        LevelSO level = EnemyWaveManager.Instance.sessionSettings.selectedLevel;
        _game.gameTime = Mathf.Max(0, level.enemyWaves.Count - 2) * level.waveDuration;
        for (int i = 0; i < WarmupLevelUps && Failure == null; i++) yield return LevelUp();
        yield return Hold(WarmupSeconds);
        yield return Settle();
        for (int c = 0; c < Clips.Length && Failure == null; c++)
        {
            TrailerClip clip = Clips[c];
            Current = clip;
            _stand = clip.Stand;
            _turn = c % 2 == 0 ? 1f : -1f;
            Phase = TrailerPhase.AwaitingCamera;
            while (!CameraRolling) yield return null;
            Phase = TrailerPhase.Rolling;
            float gap = clip.Seconds / (clip.LevelUps + 1);
            for (int l = 0; l < clip.LevelUps && Failure == null; l++)
            {
                yield return Hold(gap);
                yield return LevelUp();
            }
            yield return Hold(gap);
            yield return Settle();
            if (clip.Still)
            {
                StillTaken = false;
                Phase = TrailerPhase.Still;
                while (!StillTaken) yield return null;
                Phase = TrailerPhase.Rolling;
                yield return Hold(1f);
            }
            Phase = TrailerPhase.Cut;
            while (CameraRolling) yield return null;
        }
        if (Failure == null) Phase = TrailerPhase.Done;
    }
    IEnumerator LevelUp()
    {
        _game.PlayerExperience = _game.ExpRequired;
        float waited = 0f;
        while (!_picking && waited < PanelTimeout)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        if (!_picking)
        {
            Fail("The level-up cards never opened.");
            yield break;
        }
        while (_picking) yield return null;
    }
    IEnumerator PickCard(bool show)
    {
        _picking = true;
        GameObject panel = _hud.itemChoicePanelParent.gameObject;
        int count = 0;
        for (int i = 0; i < _hud.itemCards.Count; i++) if (_hud.itemCards[i].gameObject.activeSelf) count++;
        if (show)
        {
            yield return Hold(0.8f);
            for (int i = 0; i < count; i++)
            {
                Pointer(_hud.itemCards[i].gameObject, ExecuteEvents.pointerEnterHandler);
                yield return Hold(CardHover);
                Pointer(_hud.itemCards[i].gameObject, ExecuteEvents.pointerExitHandler);
            }
        }
        int pick = ChooseCard(count);
        GameObject target = pick >= 0 ? _hud.itemCards[pick].gameObject : SkipButton();
        if (target == null && count > 0) target = _hud.itemCards[0].gameObject;
        if (target != null)
        {
            Pointer(target, ExecuteEvents.pointerEnterHandler);
            yield return Hold(show ? 0.5f : 0.05f);
            Pointer(target, ExecuteEvents.pointerClickHandler);
        }
        float waited = 0f;
        while (panel.activeSelf && waited < PanelTimeout)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        if (panel.activeSelf) Fail("The level-up cards never closed.");
        yield return Hold(0.2f);
        _picking = false;
    }
    int ChooseCard(int count)
    {
        for (int i = 0; i < count; i++) if (_loadout.Contains(_hud.itemCards[i].item)) return i;
        for (int i = 0; i < count; i++) if (!IsWeapon(_hud.itemCards[i].item)) return i;
        return -1;
    }
    static bool IsWeapon(ItemSO item)
    {
        if (item == null || item.pickupablePrefab == null) return false;
        Equipment equipment = item.pickupablePrefab.GetComponent<Equipment>();
        return equipment != null && equipment.ItemType == ItemType.Weapon;
    }
    GameObject SkipButton()
    {
        foreach (Button button in _hud.itemChoicePanelParent.GetComponentsInChildren<Button>())
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null && label.text.Trim() == "Skip") return button.gameObject;
        }
        return null;
    }
    static IEnumerator Hold(float seconds)
    {
        float left = seconds;
        while (left > 0f)
        {
            left -= Time.unscaledDeltaTime;
            yield return null;
        }
    }
    IEnumerator Settle()
    {
        while (_picking) yield return null;
    }
    static void Pointer<T>(GameObject target, ExecuteEvents.EventFunction<T> handler) where T : IEventSystemHandler
    {
        if (target == null || EventSystem.current == null) return;
        ExecuteEvents.Execute(target, new PointerEventData(EventSystem.current), handler);
    }
    void Fail(string reason)
    {
        Failure = reason;
        Phase = TrailerPhase.Failed;
        Debug.LogError("[Trailer] " + reason);
    }
    #endregion
}
#endif
