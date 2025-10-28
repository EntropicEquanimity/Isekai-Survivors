using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using BlondieUtils;

public class WeaponOverview : MonoBehaviour
{
    [BoxGroup("Required")] public Image weaponSprite;
    [BoxGroup("Required")] public Image damageBarFill;
    [BoxGroup("Required")] public Image killBarFill;
    [BoxGroup("Required")] public TMP_Text weaponDamage, weaponKills, weaponDamagePercentage, weaponKillsPercentage;

    public void Initialize(Sprite weaponSprite, float damage, float kills, float dmgPercent, float killsPercent)
    {
        this.weaponSprite.sprite = weaponSprite;
        weaponDamage.text = Utils.FormatNumberShort(damage);
        weaponKills.text = Utils.FormatNumberShort(kills);
        this.weaponDamagePercentage.text = dmgPercent.ToString("P2");
        this.weaponKillsPercentage.text = killsPercent.ToString("P2");
        damageBarFill.fillAmount = dmgPercent;
        killBarFill.fillAmount = killsPercent;
    }
}
