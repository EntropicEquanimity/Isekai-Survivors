using Blondie.Core;
using Blondie.GameSetup;
using Blondie.UI;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Jack's body dying loses the Run (until Souls and reviving arrive, ticket #10); losing the Main Base does not: the game freezes (time stops, the Character and building stop taking orders),
    /// it says so on screen and <see cref="Defeated"/> fires. The freeze lifts when the Region unloads.
    /// </summary>
    public sealed class RegionDefeat : MonoBehaviour
    {
        public bool IsDefeated { get; private set; }

        private void Update()
        {
            if (IsDefeated) return;
            CharacterControl body = Player.Character;
            if (body != null && body.IsDead) Lose();
        }

        private void OnDestroy()
        {
            if (IsDefeated) GamePause.Resume(this);
        }

        private void Lose()
        {
            IsDefeated = true;
            GamePause.Pause(this);
            BLog.Info("Region", "Jack's body died: the Run is lost.");
            Camera cam = Camera.main;
            if (cam != null) FloatingText.Spawn(cam.transform.position + new Vector3(0f, 0f, 10f), "Jack has fallen", Color.red, 4f);
            RegionEvents.End(RegionEnd.Defeated);   // each system stops itself (IRegionEndListener)
        }
    }
}
