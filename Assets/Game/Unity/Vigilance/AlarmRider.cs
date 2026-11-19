using Blondie.Pooling;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// A rider on its way to the Seat to tell it what a witness saw. It is a Lancer (the mortal prefab) with no brain of
    /// its own: this component rides it along the Seat's static path field, every frame, so nothing asks for a new path.
    /// It is an ordinary mortal to everything else: it can be seen, hit and killed. Reaching the Seat raises Vigilance a
    /// full stage; a rider that dies on the way raises nothing. <see cref="RegionAlarms"/> sends and counts riders.
    /// </summary>
    public sealed class AlarmRider : MonoBehaviour
    {
        private RegionAlarms _owner;
        private RegionSettlements.Settlement _seat;
        private HordeMember _member;
        private SpriteRenderer _sprite;
        private bool _active;

        /// <summary>It is riding for the Seat now.</summary>
        public bool IsRiding => _active;

        public HordeMember Member => _member;

        internal void Launch(RegionAlarms owner, RegionSettlements.Settlement seat)
        {
            _owner = owner;
            _seat = seat;
            _member = GetComponent<HordeMember>();
            _sprite = GetComponent<SpriteRenderer>();
            _active = true;
            enabled = true;
        }

        private void Update()
        {
            if (!_active) return;
            if (!_member.IsAlive) { End(arrived: false); return; }

            RegionGrid grid = RegionGrid.Current;
            RegionMortals mortals = RegionMortals.Current;
            if (grid == null || mortals == null || mortals.Homes == null) return;

            Vector3 p = transform.position;
            if (AlarmRules.HasArrived(grid.CellAt(p), _seat.Plan.Interior)) { End(arrived: true); return; }

            Vector2 dir;
            if (!mortals.Homes.TryDirection(_seat, p, Time.time, out dir))
            {
                // The field is still being computed: head for the Seat's middle.
                Vector3 c = mortals.Homes.Centre(_seat);
                dir = new Vector2(c.x - p.x, c.y - p.y);
                if (dir.sqrMagnitude < 0.01f) return;
                dir.Normalize();
            }
            Move(grid, p, dir, AlarmTuning.RiderCellsPerSecond * grid.CellSize * Time.deltaTime);
        }

        // Straight along dir, or sliding along a wall on one axis when the straight step is blocked.
        private void Move(RegionGrid grid, Vector3 p, Vector2 dir, float length)
        {
            Vector3 next = p + new Vector3(dir.x * length, dir.y * length, 0f);
            if (!grid.IsWalkable(next))
            {
                var slideX = new Vector3(next.x, p.y, p.z);
                var slideY = new Vector3(p.x, next.y, p.z);
                bool xFirst = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y);
                if (xFirst && grid.IsWalkable(slideX)) next = slideX;
                else if (grid.IsWalkable(slideY)) next = slideY;
                else if (grid.IsWalkable(slideX)) next = slideX;
                else return;
            }
            transform.position = next;
            if (_sprite != null && Mathf.Abs(dir.x) > 0.05f) _sprite.flipX = dir.x < 0f;
        }

        // Pooled: when the object is despawned while riding (the Region ends, a test clears the horde), the ride is over.
        private void OnDisable()
        {
            if (_active) End(arrived: false);
        }

        private void End(bool arrived)
        {
            _active = false;
            enabled = false;
            _owner.RiderEnded(this, arrived);
            if (arrived) Pools.Despawn(gameObject);
        }
    }
}
