using System.Collections.Generic;
using System.Diagnostics;
using Blondie.Combat;
using Blondie.Controllers;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Shared flipbook animator for living units: one update loop drives idle, walk, attack, hurt and death clips for
    /// every tracked unit, with cached frames and no per-frame allocations.
    /// </summary>
    [RegionSystem]
    public sealed class UnitFlipbookAnimator : MonoBehaviour
    {
        private readonly List<AnimatedUnit> _units = new();
        private readonly Dictionary<Damageable, AnimatedUnit> _byDamageable = new();
        private readonly Dictionary<CombatUnit, AnimatedUnit> _byCombatUnit = new();
        private readonly List<UnitAnimationCorpse> _corpses = new();

        public static UnitFlipbookAnimator Current { get; private set; }

        public int TrackedCount => _units.Count;

        private void OnEnable()
        {
            Current = this;
            RegionEntities.Entered += OnEntered;
            IReadOnlyList<RegionEntity> living = RegionEntities.Living;
            for (int i = 0; i < living.Count; i++) TryTrack(living[i]);
        }

        private void OnDisable()
        {
            RegionEntities.Entered -= OnEntered;
            for (int i = 0; i < _units.Count; i++) _units[i].Dispose();
            _units.Clear();
            _byDamageable.Clear();
            _byCombatUnit.Clear();
            if (Current == this) Current = null;
        }

        private void OnEntered(RegionEntity entity) => TryTrack(entity);

        private void Update()
        {
            long start = Stopwatch.GetTimestamp();
            float dt = Time.deltaTime;
            float now = Time.time;

            for (int i = _units.Count - 1; i >= 0; i--)
            {
                AnimatedUnit unit = _units[i];
                if (!unit.IsUsable)
                {
                    RemoveAt(i);
                    continue;
                }

                if (!unit.IsActiveInHierarchy && unit.Damageable.IsDead)
                {
                    RemoveAt(i);
                    continue;
                }

                unit.Step(dt, now);
            }

            for (int i = 0; i < _corpses.Count; i++)
            {
                UnitAnimationCorpse corpse = _corpses[i];
                if (corpse == null || !corpse.IsPlaying) continue;
                corpse.Tick(dt, now);
            }

            UnitAnimationCost.Add(Stopwatch.GetTimestamp() - start);
        }

        private void TryTrack(RegionEntity entity)
        {
            if (entity == null || entity.Damageable == null || entity.GameObject == null) return;
            if (!ShouldAnimate(entity.Kind)) return;
            if (_byDamageable.ContainsKey(entity.Damageable)) return;

            WalkAnimation binding = entity.GameObject.GetComponentInChildren<WalkAnimation>(true);
            if (binding == null) return;

            SpriteRenderer renderer = binding.Renderer;
            if (renderer == null) return;

            UnitAnimationSet set = UnitFlipbookTable.Resolve(binding, renderer.sprite);
            if (set.Idle.Length == 0 && set.Walk.Length == 0 && set.Attack.Length == 0 && set.Hurt.Length == 0 && set.Death.Length == 0 && set.Mine.Length == 0) return;

            var unit = new AnimatedUnit(this, binding, entity.Damageable, renderer, set);
            unit.Subscribe();
            _units.Add(unit);
            _byDamageable[entity.Damageable] = unit;
            if (entity.Damageable.Unit != null) _byCombatUnit[entity.Damageable.Unit] = unit;
        }

        private static bool ShouldAnimate(UnitKind kind) =>
            kind is UnitKind.Character or UnitKind.Demon or UnitKind.Mortal or UnitKind.Wildlife;

        private void RemoveAt(int index)
        {
            AnimatedUnit unit = _units[index];
            unit.Dispose();
            if (unit.Damageable != null) _byDamageable.Remove(unit.Damageable);
            if (unit.CombatUnit != null && _byCombatUnit.TryGetValue(unit.CombatUnit, out AnimatedUnit found) && ReferenceEquals(found, unit)) _byCombatUnit.Remove(unit.CombatUnit);

            int last = _units.Count - 1;
            if (index != last) _units[index] = _units[last];
            _units.RemoveAt(last);
        }

        private UnitAnimationCorpse AcquireCorpse()
        {
            for (int i = 0; i < _corpses.Count; i++)
            {
                UnitAnimationCorpse corpse = _corpses[i];
                if (corpse != null && !corpse.IsPlaying) return corpse;
            }

            var go = new GameObject("Unit Death Corpse");
            go.SetActive(false);
            go.AddComponent<SpriteRenderer>();
            UnitAnimationCorpse created = go.AddComponent<UnitAnimationCorpse>();
            _corpses.Add(created);
            return created;
        }

        private void SpawnCorpse(AnimatedUnit source)
        {
            Sprite[] frames = source.Set.Death;
            if (frames == null || frames.Length == 0 || source.Renderer == null) return;

            UnitAnimationCorpse corpse = AcquireCorpse();
            SpriteRenderer renderer = source.Renderer;
            corpse.Play(frames, source.Set.DeathFps, source.Transform.position, renderer.flipX, renderer.sharedMaterial, renderer.color, renderer.sortingLayerID, renderer.sortingOrder);
        }

        private void NoteAttack(CombatUnit source, float now)
        {
            if (source == null) return;
            if (_byCombatUnit.TryGetValue(source, out AnimatedUnit attacker)) attacker.TriggerAttack(now);
        }

        private sealed class AnimatedUnit
        {
            private enum ClipKind : byte { None, Idle, Walk, Attack, Hurt, Death, Mine }

            private readonly UnitFlipbookAnimator _owner;
            private readonly WalkAnimation _binding;
            private readonly Damageable _damageable;
            private readonly SpriteRenderer _renderer;
            private readonly UnitAnimationSet _set;

            private Vector3 _lastPosition;
            private ClipKind _clip = ClipKind.None;
            private Sprite[] _frames = System.Array.Empty<Sprite>();
            private float _fps;
            private bool _loop;
            private int _frame;
            private float _clock;
            private float _hurtUntil;
            private float _attackUntil;
            private bool _dead;
            private bool _subscribed;

            public AnimatedUnit(UnitFlipbookAnimator owner, WalkAnimation binding, Damageable damageable, SpriteRenderer renderer, UnitAnimationSet set)
            {
                _owner = owner;
                _binding = binding;
                _damageable = damageable;
                _renderer = renderer;
                _set = set;
                _lastPosition = damageable.transform.position;
            }

            public WalkAnimation Binding => _binding;
            public Damageable Damageable => _damageable;
            public CombatUnit CombatUnit => _damageable != null ? _damageable.Unit : null;
            public SpriteRenderer Renderer => _renderer;
            public Transform Transform => _damageable != null ? _damageable.transform : null;
            public UnitAnimationSet Set => _set;
            public bool IsUsable => _binding != null && _damageable != null && _renderer != null;
            public bool IsActiveInHierarchy => _damageable != null && _damageable.gameObject.activeInHierarchy;

            public void Subscribe()
            {
                if (_subscribed || _damageable == null) return;
                _damageable.Damaged += OnDamaged;
                _damageable.Died += OnDied;
                _damageable.Revived += OnRevived;
                _subscribed = true;
            }

            public void Dispose()
            {
                if (!_subscribed || _damageable == null) return;
                _damageable.Damaged -= OnDamaged;
                _damageable.Died -= OnDied;
                _damageable.Revived -= OnRevived;
                _subscribed = false;
            }

            public void Step(float dt, float now)
            {
                bool moving = IsMoving(dt, out float dx);
                if (Mathf.Abs(dx) > 0.05f) _renderer.flipX = dx < 0f;

                ClipKind wanted = PickClip(now, moving);
                ResolveClip(wanted);
                if (_frames == null || _frames.Length == 0)
                {
                    if (_renderer.sprite == null && _set.Idle.Length > 0) _renderer.sprite = _set.Idle[0];
                    return;
                }

                if (_renderer.sprite != _frames[_frame]) _renderer.sprite = _frames[_frame];
                if (_frames.Length <= 1 || _fps <= 0f) return;

                _clock += dt * _fps;
                int steps = (int)_clock;
                if (steps <= 0) return;
                _clock -= steps;

                int next = _loop ? (_frame + steps) % _frames.Length : Mathf.Min(_frames.Length - 1, _frame + steps);
                if (next == _frame) return;
                _frame = next;
                _renderer.sprite = _frames[_frame];
            }

            public void TriggerAttack(float now)
            {
                if (_dead || _set.AttackDuration <= 0f) return;
                _attackUntil = Mathf.Max(_attackUntil, now + _set.AttackDuration);
            }

            private void OnDamaged(DamageResult result)
            {
                float now = Time.time;
                if (!_dead && _set.HurtDuration > 0f) _hurtUntil = Mathf.Max(_hurtUntil, now + _set.HurtDuration);

                CombatUnit source = result.Info != null ? result.Info.Source : null;
                if (source != null && source != CombatUnit) _owner.NoteAttack(source, now);
            }

            private void OnDied(DamageResult result)
            {
                _dead = true;
                _hurtUntil = 0f;
                _attackUntil = 0f;
                _owner.SpawnCorpse(this);
            }

            private void OnRevived()
            {
                _dead = false;
                _hurtUntil = 0f;
                _attackUntil = 0f;
                _lastPosition = _damageable.transform.position;
                _clip = ClipKind.None;
            }

            private ClipKind PickClip(float now, bool moving)
            {
                if (_dead) return ClipKind.Death;
                if (now < _hurtUntil) return ClipKind.Hurt;
                if (now < _attackUntil) return ClipKind.Attack;
                if (_binding.IsMining()) return ClipKind.Mine;
                return moving ? ClipKind.Walk : ClipKind.Idle;
            }

            private void ResolveClip(ClipKind wanted)
            {
                Sprite[] frames;
                float fps;
                bool loop;
                GetClipData(wanted, out frames, out fps, out loop);
                if ((frames == null || frames.Length == 0) && wanted != ClipKind.Idle) GetClipData(ClipKind.Idle, out frames, out fps, out loop);
                if (frames == null) frames = System.Array.Empty<Sprite>();

                if (wanted == _clip && ReferenceEquals(frames, _frames) && Mathf.Approximately(fps, _fps) && loop == _loop) return;
                _clip = wanted;
                _frames = frames;
                _fps = fps;
                _loop = loop;
                _frame = 0;
                _clock = 0f;
            }

            private void GetClipData(ClipKind kind, out Sprite[] frames, out float fps, out bool loop)
            {
                switch (kind)
                {
                    case ClipKind.Walk:
                        frames = _set.Walk;
                        fps = _set.WalkFps;
                        loop = true;
                        return;
                    case ClipKind.Attack:
                        frames = _set.Attack;
                        fps = _set.AttackFps;
                        loop = false;
                        return;
                    case ClipKind.Hurt:
                        frames = _set.Hurt;
                        fps = _set.HurtFps;
                        loop = false;
                        return;
                    case ClipKind.Death:
                        frames = _set.Death;
                        fps = _set.DeathFps;
                        loop = false;
                        return;
                    case ClipKind.Mine:
                        frames = _set.Mine;
                        fps = _set.MineFps;
                        loop = true;
                        return;
                    default:
                        frames = _set.Idle;
                        fps = _set.IdleFps;
                        loop = true;
                        return;
                }
            }

            private bool IsMoving(float dt, out float dx)
            {
                Vector3 now = _damageable.transform.position;
                Vector3 delta = now - _lastPosition;
                _lastPosition = now;
                float minSpeed = Mathf.Max(0.01f, _binding.MovingSpeed);
                CharacterMotor motor = _binding.Motor;
                if (motor != null)
                {
                    Vector2 velocity = motor.Velocity;
                    dx = Mathf.Abs(velocity.x) > Mathf.Abs(delta.x) ? velocity.x : delta.x;
                    return velocity.sqrMagnitude > minSpeed * minSpeed;
                }

                dx = delta.x;
                if (dt <= 1e-5f) return false;
                float speedSq = delta.sqrMagnitude / (dt * dt);
                return speedSq > minSpeed * minSpeed;
            }
        }
    }
}
