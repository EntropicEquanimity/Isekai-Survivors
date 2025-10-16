using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    public SettingsSO settings;

    [BoxGroup("Settings")] public Toggle damageNumbers, healingNumbers, animatedText;

    #region Settings
    //Gameplay
    public void ToggleDamageNumbers(bool active)
    {
        settings.showDamageNumbers = active;
        PlayerPrefs.SetInt("DamageNumbers", active == true ? 1 : 0);
    }
    public void ToggleHealingNumbers(bool active)
    {
        settings.showHealingNumbers = active;
        PlayerPrefs.SetInt("HealingNumbers", active == true ? 1 : 0);
    }
    public void ToggleAnimatedText(bool active)
    {
        settings.animatedFloatingText = active;
        PlayerPrefs.SetInt("AnimatedText", active == true ? 1 : 0);
    }
    public void ChangeFloatingTextSizeMultiplier(float size)
    {
        settings.textSizeMultiplier = size;
        PlayerPrefs.SetFloat("TextSizeMultiplier", size);
    }
    public void ToggleFlashOnTakeDamage(bool active)
    {
        settings.colorFlashOnTakeDamage = active;
        PlayerPrefs.SetInt("FlashOnTakeDamage", active == true ? 1 : 0);
    }

    //Sound
    public void ChangeMainVolume(float volume)
    {
        settings.mainVolume = volume;
        PlayerPrefs.SetFloat("MainVol", volume);
    }
    public void ChangeGameVolume(float volume)
    {
        settings.gameVolume = volume;
        PlayerPrefs.SetFloat("GameVol", volume);
    }
    public void ChangeMusicVolume(float volume)
    {
        settings.musicVolume = volume;
        PlayerPrefs.SetFloat("MusicVol", volume);
    }
    public void ChangeInterfaceVolume(float volume)
    {
        settings.interfaceVolume = volume;
        PlayerPrefs.SetFloat("InterfaceVol", volume);
    }
    #endregion
}
