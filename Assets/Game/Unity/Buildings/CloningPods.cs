using Blondie.UI;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The Main Base's Cloning Pods (<see cref="CloningBay"/>): when the Character dies, a pod clones it back at the
    /// Base after a delay. With no pods left (or no Base) it stays dead; that alone doesn't lose the Run. The Legacy tree
    /// adds pods and can make used ones rebuild (<see cref="LegacyTree"/>).
    /// </summary>
    [RequireComponent(typeof(Building))]
    public sealed class CloningPods : MonoBehaviour
    {
        [SerializeField, Min(0)] private int _pods = 1;
        [SerializeField, Min(0f)] private float _cloneSeconds = 8f;
        [SerializeField] private Vector2 _exit = new(0f, -1.6f);

        private CloningBay _bay;
        private CharacterControl _character;
        private Building _building;

        public CloningBay Bay => _bay ??= new CloningBay(_pods + LegacyTree.ExtraPods(MetaProgress.Unit), _cloneSeconds,
            LegacyTree.RebuildsPods(MetaProgress.Unit) ? LegacyTree.PodRebuildSeconds : 0f);

        private void Awake() => _building = GetComponent<Building>();

        private void Start()
        {
            _character = Player.Character;
            if (_character != null && _character.TryGetComponent(out Blondie.Combat.Damageable d)) d.Died += _ => OnCharacterDied();
        }

        private void OnCharacterDied()
        {
            if (this == null || !_building.IsAlive) return;
            if (Bay.CharacterDied()) FloatingText.Spawn(transform.position + Vector3.up, $"Cloning... ({Bay.Pods} pods left)", Color.cyan);
            else FloatingText.Spawn(transform.position + Vector3.up, "No Cloning Pods left", Color.red);
        }

        private void Update()
        {
            int pods = Bay.Pods;
            bool ready = Bay.Tick(Time.deltaTime);
            if (Bay.Pods > pods) FloatingText.Spawn(transform.position + Vector3.up, $"Cloning Pod rebuilt ({Bay.Pods} ready)", Color.cyan);
            if (!ready || _character == null || !_building.IsAlive) return;
            _character.transform.position = transform.position + (Vector3)_exit;
            _character.GetComponent<Blondie.Combat.Damageable>().Revive();
        }
    }
}
