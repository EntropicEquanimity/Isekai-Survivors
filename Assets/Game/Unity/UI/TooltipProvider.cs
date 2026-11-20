using System.Text;
using Blondie.Core;
using Blondie.UI;
using Blondie.PixelPerfect;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Isekai
{
    public readonly struct TooltipContent
    {
        public TooltipContent(string header, string body)
        {
            Header = header ?? string.Empty;
            Body = body ?? string.Empty;
        }

        public string Header { get; }
        public string Body { get; }
    }

    public static class TooltipProvider
    {
        private const string CloneSuffix = "(Clone)";
        private static readonly StringBuilder s_text = new(384);

        public static TooltipKey KeyForResource(ResourceType type) => type switch
        {
            ResourceType.Stone => TooltipKey.HudStone,
            ResourceType.Wood => TooltipKey.HudWood,
            _ => TooltipKey.HudSouls,
        };

        public static string BuildButtonBody(BuildingAsset asset, bool canAfford, string placementReason = null)
        {
            string role = RoleOf(asset);
            string reason = string.IsNullOrEmpty(placementReason)
                ? (canAfford ? TooltipText.Get(TooltipKey.BuildPlacementReady) : TooltipText.Get(TooltipKey.BuildPlacementTooExpensive))
                : placementReason;
            return string.Format(TooltipText.Get(TooltipKey.BuildButtonBodyFormat), asset != null ? asset.PriceText : string.Empty, role, reason);
        }

        public static bool TryWorldTooltip(Vector3 world, float radius, out TooltipContent tooltip)
        {
            return TryWorldTooltip(world, radius, out tooltip, out _);
        }

        public static bool TryWorldTooltip(Vector3 world, float radius, out TooltipContent tooltip, out Object token)
        {
            tooltip = default;
            if (!TryWorldTarget(world, radius, out RegionEntity unit, out Building building, out token)) return false;
            tooltip = DescribeWorldTarget(unit, building);
            return true;
        }

        public static bool TryWorldTarget(Vector3 world, float radius, out RegionEntity unit, out Building building, out Object token)
        {
            token = null;
            unit = null;
            building = null;
            float best = radius * radius;
            unit = NearestUnit(world, ref best);
            building = NearestBuilding(world, ref best);

            if (building != null)
            {
                token = building.gameObject;
                return true;
            }

            if (unit == null) return false;
            token = unit.GameObject;
            return true;
        }

        public static TooltipContent DescribeWorldTarget(RegionEntity unit, Building building) =>
            building != null ? Describe(building) : Describe(unit);

        private static RegionEntity NearestUnit(Vector3 world, ref float best)
        {
            RegionEntity pick = null;
            var living = RegionEntities.Living;
            for (int i = 0; i < living.Count; i++)
            {
                RegionEntity e = living[i];
                if (!IsTooltipUnit(e)) continue;
                Vector3 at = e.GameObject.transform.position;
                float dx = at.x - world.x;
                float dy = at.y - world.y;
                float sqr = dx * dx + dy * dy;
                if (sqr > best) continue;
                best = sqr;
                pick = e;
            }
            return pick;
        }

        private static Building NearestBuilding(Vector3 world, ref float best)
        {
            Building pick = null;
            var buildings = Building.All;
            for (int i = 0; i < buildings.Count; i++)
            {
                Building b = buildings[i];
                if (b == null || !b.IsAlive) continue;
                Vector3 at = b.transform.position;
                float dx = at.x - world.x;
                float dy = at.y - world.y;
                float sqr = dx * dx + dy * dy;
                if (sqr > best) continue;
                best = sqr;
                pick = b;
            }
            return pick;
        }

        private static bool IsTooltipUnit(RegionEntity e)
        {
            if (e == null || !e.IsListed || e.Damageable.IsDead) return false;
            return e.Kind is UnitKind.Character or UnitKind.Demon or UnitKind.Mortal or UnitKind.Wildlife;
        }

        private static TooltipContent Describe(RegionEntity unit)
        {
            if (unit == null) return default;
            string name = NameOf(unit);
            s_text.Clear();
            s_text.AppendFormat(TooltipText.Get(TooltipKey.WorldSideLine), unit.Side);

            if (unit.Side == Side.Yours && unit.Kind == UnitKind.Demon && DemonLoyalty.Of(unit.GameObject) is { } loyalty)
                s_text.Append('\n').AppendFormat(TooltipText.Get(TooltipKey.WorldLoyaltyLine), loyalty.Level, Mathf.CeilToInt(loyalty.Loyalty));

            UnitTraits traits = UnitTraits.Of(unit.GameObject);
            if (traits == null || !traits.Any) s_text.Append('\n').Append(TooltipText.Get(TooltipKey.WorldNoTraits));
            else
            {
                s_text.Append('\n').Append(TooltipText.Get(TooltipKey.WorldTraitsHeader));
                for (int i = 0; i < traits.Traits.Count; i++)
                {
                    TraitDef def = TraitTable.Get(traits.Traits[i]);
                    s_text.Append('\n').AppendFormat(TooltipText.Get(TooltipKey.WorldTraitLine), def.Name, def.Description);
                }
            }
            return new TooltipContent(name, s_text.ToString());
        }

        private static TooltipContent Describe(Building building)
        {
            if (building == null) return default;
            string header = building.Asset != null ? building.Asset.DisplayName : TrimName(building.name);
            if (building.IsMainBase && AnchorCatalog.KindOf(building) is { } kind) header = AnchorTuning.Name(kind);
            string role = RoleOf(building);
            string body = string.Format(TooltipText.Get(TooltipKey.WorldRoleLine), role);
            return new TooltipContent(header, body);
        }

        private static string NameOf(RegionEntity unit)
        {
            if (unit.Kind == UnitKind.Character) return TooltipText.Get(TooltipKey.UnitNameJack);
            if (unit.GameObject.TryGetComponent(out DemonBrain demon))
                return demon.Type switch
                {
                    DemonType.BoneDemon => TooltipText.Get(TooltipKey.UnitNameBoneDemon),
                    DemonType.Skeleton => TooltipText.Get(TooltipKey.UnitNameSkeleton),
                    _ => TooltipText.Get(TooltipKey.UnitNameImp),
                };
            if (unit.GameObject.TryGetComponent(out MortalBrain mortal)) return mortal.Type.ToString();
            if (unit.GameObject.TryGetComponent(out WildlifeBrain wildlife)) return wildlife.Type.ToString();
            return TrimName(unit.GameObject.name);
        }

        private static string RoleOf(BuildingAsset asset)
        {
            if (asset == null) return TooltipText.Get(TooltipKey.BuildRoleUnknown);
            return asset.DisplayName switch
            {
                "Portal" => TooltipText.Get(TooltipKey.BuildRolePortal),
                "Spire" => TooltipText.Get(TooltipKey.BuildRoleSpire),
                "Outpost" => TooltipText.Get(TooltipKey.BuildRoleOutpost),
                "Main Base" => TooltipText.Get(TooltipKey.BuildRoleAnchor),
                _ => TooltipText.Get(TooltipKey.BuildRoleUnknown),
            };
        }

        private static string RoleOf(Building building)
        {
            if (building == null) return TooltipText.Get(TooltipKey.BuildRoleUnknown);
            if (building.IsMainBase) return TooltipText.Get(TooltipKey.BuildRoleAnchor);
            if (!building.IsPlayers) return TooltipText.Get(TooltipKey.BuildRoleMortalBuilding);
            return RoleOf(building.Asset);
        }

        private static string TrimName(string name)
        {
            if (string.IsNullOrEmpty(name)) return TooltipText.Get(TooltipKey.UnitNameUnknown);
            int at = name.LastIndexOf(CloneSuffix, System.StringComparison.Ordinal);
            return at > 0 ? name.Substring(0, at).TrimEnd() : name;
        }
    }

    /// <summary>World hover tooltips for units and Buildings, with one nearest-target query each frame.</summary>
    [RegionSystem]
    public sealed class WorldHoverTooltips : MonoBehaviour
    {
        private const float HoverRadius = 1.25f;
        private const float RefreshSeconds = 0.2f;

        private Object _target;
        private string _header;
        private string _body;
        private float _nextRefreshAt;
        private bool _ownsTooltip;

        private void OnDisable() => Hide();

        private void Update()
        {
            if (!TryMouse(out Vector2 screen, out Vector3 world) || UIPanels.IsPointerOverUI(screen))
            {
                Hide();
                return;
            }

            if (!TooltipProvider.TryWorldTarget(world, HoverRadius, out RegionEntity unit, out Building building, out Object target))
            {
                Hide();
                return;
            }

            bool refresh = Time.unscaledTime >= _nextRefreshAt;
            if (!refresh && target == _target) return;
            TooltipContent tip = TooltipProvider.DescribeWorldTarget(unit, building);
            if (!refresh && tip.Header == _header && tip.Body == _body) return;
            _target = target;
            _header = tip.Header;
            _body = tip.Body;
            _nextRefreshAt = Time.unscaledTime + RefreshSeconds;
            _ownsTooltip = true;
            Tooltip.Show(_body, _header);
        }

        private void Hide()
        {
            if (_target == null && string.IsNullOrEmpty(_body) && string.IsNullOrEmpty(_header)) return;
            _target = null;
            _header = string.Empty;
            _body = string.Empty;
            if (_ownsTooltip) Tooltip.Hide();
            _ownsTooltip = false;
        }

        private static bool TryMouse(out Vector2 screen, out Vector3 world)
        {
            screen = default;
            world = default;
            Mouse mouse = Mouse.current;
            if (mouse == null) return false;
            screen = mouse.position.ReadValue();
            if (PixelCamera.Main != null)
            {
                world = PixelCamera.Main.ScreenToWorld(screen);
                return true;
            }

            Camera cam = Camera.main;
            if (cam == null) return false;
            world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            return true;
        }
    }
}
