using Blondie.Combat;
using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The visual effects of the Region: it listens to the <see cref="Feedback"/> cues and answers each with a flipbook
    /// from the Gigapack sheets, drawn by one fixed pool of <see cref="PoolSize"/> sprites (no allocation while playing;
    /// a kind that is busy, or an effect arriving when the pool is full, is dropped, so a horde fight with hundreds of
    /// deaths stays cheap). Effects play only where Jack's side can see (fog of war) and sit on whole pixels, so they
    /// stay on the 16 px grid. It also flashes hit units (<see cref="VfxHitFlashes"/>), shakes the camera for Jack's own
    /// hits (<see cref="VfxCameraShake"/>), leaves the wisp's trail, pulses where corruption spreads and light where it is
    /// purified, marks spawn circles and alarm riders while they are seen, and celebrates a capture.
    /// </summary>
    [RegionSystem]
    public sealed class VfxPlayer : MonoBehaviour
    {
        public const int PoolSize = 128;
        public const string PrefabResource = "Vfx Sprite";

        private const int SortingOrder = 40;
        private const float PixelSize = 1f / 16f;
        private const float TrailEvery = 0.1f;
        private const float PurifyEvery = 0.15f;
        private const float MarkRidersEvery = 1.2f;
        private const float MarkCirclesEvery = 2.5f;
        private const int Celebrations = 8;

        private static readonly Color Dim = new(1f, 1f, 1f, 0.8f);

        private readonly VfxSprite[] _live = new VfxSprite[PoolSize];
        private readonly VfxSprite[] _idle = new VfxSprite[PoolSize];
        private readonly int[] _liveOfKind = new int[System.Enum.GetValues(typeof(VfxKind)).Length];
        private int _liveCount, _idleCount;

        private readonly VfxHitFlashes _flashes = new();
        private readonly float[] _celebrateAt = new float[Celebrations];
        private readonly Vector3[] _celebrateWhere = new Vector3[Celebrations];
        private readonly VfxKind[] _celebrateKind = new VfxKind[Celebrations];
        private int _celebrating;

        private bool _ready;
        private bool _trailing;
        private float _trailAt, _purifyAt, _ridersAt, _circlesAt;
        private CellLayer _corruption;
        private CharacterControl _jackBody;
        private CombatUnit _jackUnit;

        public static VfxPlayer Current { get; private set; }

        /// <summary>Effects playing now.</summary>
        public int ActiveCount => _liveCount;

        public int ActiveOf(VfxKind kind) => _liveOfKind[(int)kind];

        /// <summary>Effects started, and effects dropped for being unseen or for want of room.</summary>
        public int Spawned { get; private set; }
        public int DroppedOutOfSight { get; private set; }
        public int DroppedByCap { get; private set; }

        public VfxHitFlashes Flashes => _flashes;

        /// <summary>True while the wisp's trail is being laid (from the wisp cue until a revive or possession).</summary>
        public bool IsTrailing => _trailing;

        /// <summary>The effects playing now, for tests.</summary>
        public System.Collections.Generic.IEnumerable<VfxSprite> Live
        {
            get { for (int i = 0; i < _liveCount; i++) yield return _live[i]; }
        }

        private void Awake()
        {
            Current = this;
            gameObject.AddComponent<VfxCameraShake>();
            var prefab = Resources.Load<GameObject>(PrefabResource);
            for (int i = 0; i < PoolSize; i++)
            {
                GameObject go = prefab != null ? Instantiate(prefab, transform) : NewSprite();
                go.name = "Vfx " + i;
                go.transform.SetParent(transform, false);
                VfxSprite sprite = go.GetComponent<VfxSprite>();
                go.SetActive(false);
                _idle[_idleCount++] = sprite;
            }
        }

        // Without the prefab (never in the game): the same thing built in code.
        private static GameObject NewSprite()
        {
            var go = new GameObject("Vfx");
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<VfxSprite>();
            return go;
        }

        private void OnEnable()
        {
            Feedback.Raised += OnCue;
            RegionEntities.Damaged += OnDamaged;
            RegionEvents.Ended += OnEnded;
        }

        private void OnDisable()
        {
            Feedback.Raised -= OnCue;
            RegionEntities.Damaged -= OnDamaged;
            RegionEvents.Ended -= OnEnded;
            if (_corruption != null) _corruption.Changed -= OnCorruption;
            if (RegionBootstrap.Current != null) RegionBootstrap.Current.Populated -= OnPopulated;
            _flashes.Clear();
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void Start()
        {
            RegionGrid grid = RegionGrid.Current;
            if (grid != null)
            {
                _corruption = grid.Layer(RegionCorruption.LayerName);
                _corruption.Changed += OnCorruption;
            }
            RegionBootstrap bootstrap = RegionBootstrap.Current;
            if (bootstrap == null || bootstrap.IsPopulated) _ready = true;
            else bootstrap.Populated += OnPopulated;
        }

        private void OnPopulated() => _ready = true;

        private void OnEnded(RegionEnd end) => _trailing = false;

        // ---- Seeing ----

        /// <summary>Whether Jack's side can see <paramref name="position"/>: effects play only there (everywhere without fog of war).</summary>
        public static bool IsSeen(Vector3 position)
        {
            RegionVision vision = RegionVision.Current;
            if (vision == null || !RegionVision.IsOn || !vision.IsReady) return true;
            return vision.CanSee(Side.Yours, position);
        }

        // ---- Playing ----

        /// <summary>
        /// Plays <paramref name="kind"/> at <paramref name="position"/> (snapped to a whole pixel). Unless
        /// <paramref name="always"/> or <paramref name="ignoreSight"/>, nothing plays where Jack's side cannot see (the
        /// wisp's own trail is Jack's, wherever it floats). A busy kind or a full pool drops it,
        /// except that an <paramref name="always"/> effect takes the place of the oldest one.
        /// </summary>
        public bool Play(VfxKind kind, Vector3 position, bool always = false, Color? tint = null, bool ignoreSight = false)
        {
            if (!always && !ignoreSight && !IsSeen(position)) { DroppedOutOfSight++; return false; }
            VfxClip clip = VfxClips.Of(kind);
            if (clip == null) return false;
            if (_liveOfKind[(int)kind] >= clip.Cap) { DroppedByCap++; return false; }
            if (_idleCount == 0)
            {
                if (!always) { DroppedByCap++; return false; }
                Release(0);
            }
            VfxSprite sprite = _idle[--_idleCount];
            position.x = Mathf.Round(position.x / PixelSize) * PixelSize;
            position.y = Mathf.Round(position.y / PixelSize) * PixelSize;
            position.z = 0f;
            sprite.Begin(clip, position, tint ?? Color.white, SortingOrder);
            _live[_liveCount++] = sprite;
            _liveOfKind[(int)kind]++;
            Spawned++;
            return true;
        }

        private void Release(int index)
        {
            VfxSprite sprite = _live[index];
            _liveOfKind[(int)sprite.Kind]--;
            sprite.End();
            _idle[_idleCount++] = sprite;
            _liveCount--;
            _live[index] = _live[_liveCount];
            _live[_liveCount] = null;
        }

        // ---- Cues ----

        private void OnCue(FeedbackCue cue)
        {
            if (!_ready) return;
            long start = VfxCost.Start();
            Vector3 at = cue.Position;
            switch (cue.Kind)
            {
                case Cue.Death:
                    if (cue.HasPosition) Play(VfxKind.DeathSplat, at);
                    break;
                case Cue.Summon:
                    if (cue.HasPosition) Play(VfxKind.Summon, at);
                    break;
                case Cue.Wisp:
                    _trailing = true;
                    _trailAt = Time.time;
                    if (cue.HasPosition) Play(VfxKind.WispBurst, at);
                    break;
                case Cue.Revive:
                    _trailing = false;
                    if (cue.HasPosition) Play(VfxKind.Revive, at);
                    break;
                case Cue.Possess:
                    _trailing = false;
                    if (cue.HasPosition) Play(VfxKind.Possess, at);
                    break;
                case Cue.CorruptionSpread:
                    if (cue.HasPosition) Play(VfxKind.CorruptionPulse, at);
                    break;
                case Cue.Alarm:
                    if (cue.HasPosition) Play(VfxKind.Alert, at + Vector3.up);
                    break;
                case Cue.Rider:
                    if (cue.HasPosition) { Play(VfxKind.RiderArrives, at); Play(VfxKind.Alert, at + Vector3.up); }
                    break;
                case Cue.Wave:
                    if (cue.HasPosition) { Play(VfxKind.Wave, at); Play(VfxKind.Alert, at + Vector3.up); }
                    break;
                case Cue.Capture:
                    Celebrate();
                    break;
            }
            VfxCost.Stop(start);
        }

        // Corruption going down is a priest (or the debug menu) purifying: a light on the cell, thinned out.
        private void OnCorruption(Int2 cell, byte before, byte after)
        {
            if (!_ready || after >= before || Time.unscaledTime - _purifyAt < PurifyEvery) return;
            long start = VfxCost.Start();
            Vector3 at = RegionGrid.Current.CentreOf(cell);
            if (Play(VfxKind.Purify, at)) _purifyAt = Time.unscaledTime;
            VfxCost.Stop(start);
        }

        // ---- Hits ----

        private void OnDamaged(RegionEntity unit, DamageResult result)
        {
            // Construction wears a site down with no attacker: that is not a hit.
            if (!_ready || result.Info?.Source == null || result.Dealt <= 0f) return;
            long start = VfxCost.Start();
            GameObject body = unit.GameObject;
            Vector3 at = body.transform.position;
            if (IsSeen(at))
            {
                if (IsJack(result.Info.Source)) VfxCameraShake.Kick();
                if (_flashes.HasRoom) _flashes.Start(body.GetComponentInChildren<SpriteRenderer>(), Time.time);
            }
            VfxCost.Stop(start);
        }

        private bool IsJack(CombatUnit source)
        {
            CharacterControl body = Player.Character;
            if (body == null) return false;
            if (body != _jackBody)
            {
                _jackBody = body;
                Damageable damageable = body.GetComponent<Damageable>();
                _jackUnit = damageable != null ? damageable.Unit : null;
            }
            return _jackUnit != null && source == _jackUnit;
        }

        // ---- Celebration ----

        // A capture: fireworks around the camera over a second or so, and a crown over Jack.
        private void Celebrate()
        {
            Camera cam = Camera.main;
            Vector3 centre = cam != null ? cam.transform.position : Vector3.zero;
            Vector3 spot = Player.Character != null ? Player.Character.transform.position : centre;
            Play(VfxKind.Crown, spot + new Vector3(0f, 1.5f, 0f), true);
            for (int i = 0; i < Celebrations; i++)
            {
                _celebrateAt[i] = Time.time + i * 0.15f;
                _celebrateWhere[i] = centre + new Vector3(Random.Range(-5f, 5f), Random.Range(-3f, 3f), 0f);
                _celebrateKind[i] = i % 2 == 0 ? VfxKind.FireworkGold : VfxKind.FireworkGreen;
            }
            _celebrating = Celebrations;
        }

        // ---- Each frame ----

        private void Update()
        {
            long start = VfxCost.Start();
            float dt = Time.deltaTime;
            for (int i = _liveCount - 1; i >= 0; i--)
                if (!_live[i].Tick(dt)) Release(i);
            _flashes.Tick(Time.time);
            Mark();
            if (_celebrating > 0) TickCelebration();
            VfxCost.Stop(start);
        }

        private void TickCelebration()
        {
            float now = Time.time;
            int left = 0;
            for (int i = 0; i < Celebrations; i++)
            {
                if (_celebrateAt[i] < 0f) continue;
                if (now >= _celebrateAt[i])
                {
                    Play(_celebrateKind[i], _celebrateWhere[i], true);
                    _celebrateAt[i] = -1f;
                }
                else left++;
            }
            _celebrating = left;
        }

        // The wisp's trail, the alarm riders and the discovered spawn circles: marked while they are seen.
        private void Mark()
        {
            float now = Time.time;
            if (_trailing && now - _trailAt >= TrailEvery)
            {
                _trailAt = now;
                RegionSouls souls = RegionSouls.Current;
                if (souls != null && souls.Wisp != null) Play(VfxKind.WispTrail, souls.Wisp.transform.position, false, Dim, true);
            }

            if (now - _ridersAt >= MarkRidersEvery)
            {
                _ridersAt = now;
                RegionAlarms alarms = RegionAlarms.Current;
                if (alarms != null)
                    for (int i = 0; i < alarms.Riders.Count; i++)
                    {
                        AlarmRider rider = alarms.Riders[i];
                        if (rider != null && rider.IsRiding) Play(VfxKind.Alert, rider.transform.position + Vector3.up);
                    }
            }

            if (now - _circlesAt >= MarkCirclesEvery)
            {
                _circlesAt = now;
                VigilanceDirector director = VigilanceDirector.Current;
                if (director != null)
                    for (int i = 0; i < director.Circles.Count; i++)
                    {
                        VigilanceSpawnCircle circle = director.Circles[i];
                        if (circle != null && circle.IsAlive && circle.Discovered) Play(VfxKind.SpawnCircle, circle.transform.position);
                    }
            }
        }
    }
}
