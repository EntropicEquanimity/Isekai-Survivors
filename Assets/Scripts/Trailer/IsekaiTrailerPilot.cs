#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

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
        new TrailerClip { Id = "isekai_swarm", Seconds = 28f },
        new TrailerClip { Id = "isekai_levelup", Seconds = 26f, LevelUps = 2 },
        new TrailerClip { Id = "isekai_surrounded", Seconds = 14f, Stand = true, Still = true }
    };
    const int WarmupLevelUps = 6;
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
    public TrailerPhase Phase { get; private set; }
    public TrailerClip Current { get; private set; }
    public string Failure { get; private set; }
    public bool CameraRolling { get; set; }
    public bool StillTaken { get; set; }
    #endregion

    #region Unity
    void Update()
    {
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
        float correction = Mathf.Clamp((KiteRadius - position.magnitude) / KiteRadius, -1f, 1f);
        return tangent + outward * correction;
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
        int pick = count > 1 ? UnityEngine.Random.Range(0, count) : 0;
        if (count > 0)
        {
            Pointer(_hud.itemCards[pick].gameObject, ExecuteEvents.pointerEnterHandler);
            yield return Hold(show ? 0.5f : 0.05f);
            Pointer(_hud.itemCards[pick].gameObject, ExecuteEvents.pointerClickHandler);
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
