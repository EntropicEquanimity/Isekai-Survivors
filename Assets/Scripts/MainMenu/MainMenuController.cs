using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    public SettingsSO settings;

    #region Settings
    //Gameplay
    public void ToggleDamageNumbers(bool active)
    {
        settings.showDamageNumbers = active;
    }
    public void ToggleHealingNumbers(bool active)
    {
        settings.showHealingNumbers = active;
    }
    public void ToggleAnimatedText(bool active)
    {
        settings.animatedFloatingText = active;
    }
    public void ChangeFloatingTextSizeMultiplier(float size)
    {
        settings.textSizeMultiplier = size;
    }

    //Sound
    public void ChangeMainVolume(float volume)
    {

    }
    #endregion
}
