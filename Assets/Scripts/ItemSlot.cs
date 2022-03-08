using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ItemSlot : MonoBehaviour
{
    public Image itemSprite;
    public Image cooldownFillSprite;
    public TMPro.TMP_Text itemLevelText;
    public Equipment equipment;
    private Image _itemBackground;
    public GraphicsSettingsSO graphicsSettings;

    public Material defaultMaterial;
    public Material offCooldownMaterial;
    public bool Empty => equipment == null;
    private void OnEnable()
    {
        itemSprite.enabled = false;
        _itemBackground = GetComponent<Image>();
    }
    public void Initialize(Equipment equipment)
    {
        _itemBackground = GetComponent<Image>();
        itemSprite.enabled = true;
        itemSprite.sprite = equipment.itemData.icon;
        cooldownFillSprite.fillAmount = 0;
        transform.localScale = Vector3.one;
        _itemBackground.color = equipment.ItemType == ItemType.Weapon ? graphicsSettings.weaponTint : graphicsSettings.equipmentTint;
        itemLevelText.text = "1";
        this.equipment = equipment;
    }
    public void ResetToEmpty()
    {
        _itemBackground = GetComponent<Image>();
        itemSprite.enabled = false;
        cooldownFillSprite.fillAmount = 0;
        transform.localScale = Vector3.one;
        _itemBackground.color = Color.white;
        itemLevelText.text = "";
        equipment = null;
    }
    private void Update()
    {
        if (equipment == null) { return; }
        UpdateCooldown(equipment.CurrentCooldown, equipment.Cooldown);
        itemLevelText.text = equipment.ItemLevel.ToString();

        if (cooldownFillSprite.fillAmount == 0)
        {
            _itemBackground.material = offCooldownMaterial;
        }
        else
        {
            _itemBackground.material = defaultMaterial;
        }
    }
    public void UpdateCooldown(float currentTime, float maxTime)
    {
        cooldownFillSprite.fillAmount = currentTime / maxTime;
    }
}
