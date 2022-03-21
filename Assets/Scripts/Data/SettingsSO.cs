using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(menuName = "Settings/Settings")]
public class SettingsSO : ScriptableObject
{
    [BoxGroup("Gameplay")] public bool showDamageNumbers = true;
    [BoxGroup("Gameplay")] public bool showHealingNumbers = true;
    [BoxGroup("Gameplay")] public bool animatedFloatingText = true;
    [BoxGroup("Gameplay")] public float textSizeMultiplier = 1f;
    [BoxGroup("Gameplay")] public bool colorFlashOnTakeDamage = true;
    [BoxGroup("Gameplay")] public Color takeDamageColor = Color.red;

    [BoxGroup("Audio")] public float mainVolume = 1;
    [BoxGroup("Audio")] public float gameVolume = 1;
    [BoxGroup("Audio")] public float musicVolume = 0.5f;
    [BoxGroup("Audio")] public float interfaceVolume = 0.5f;
}
