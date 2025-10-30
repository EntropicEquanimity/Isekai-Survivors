using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEditor;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class CharacterSelection : MonoBehaviour
{
    [BoxGroup("Characters")] public List<PlayerSO> characters = new List<PlayerSO>();
    [BoxGroup("Required")] public Transform characterButtonParent;
    [BoxGroup("Required")] public GameObject characterButtonPrefab;
    [BoxGroup("Required")] public SessionSettingsSO settings;

    [BoxGroup("UI")] public TMP_Text characterNameText;
    [BoxGroup("UI")] public TMP_Text characterDescription;
    [BoxGroup("UI")] public Image weaponSprite;
    [BoxGroup("UI")] public TMP_Text weaponName;
    [BoxGroup("UI")] public TMP_Text characterStats;

    [ReadOnly] public List<CharacterButton> characterButtons = new List<CharacterButton>();

#if UNITY_EDITOR
    [Button]
    public void LoadAllCharacters()
    {
        characters = AssetDatabase.FindAssets("t:PlayerSO").Select(x => AssetDatabase.LoadAssetAtPath<PlayerSO>(AssetDatabase.GUIDToAssetPath(x))).ToList();
    }
#endif
    private void OnEnable()
    {
        for (int i = 0; i < characterButtons.Count; i++)
        {
            Destroy(characterButtons[i].gameObject);
        }
        characterButtons = new List<CharacterButton>();

        if (characterButtonParent == null) { Debug.LogError("Character button's parent is missing!"); }
        for (int i = 0; i < characters.Count; i++)
        {
            PlayerSO playerSO = characters[i];
            characterButtons.Add(Instantiate(characterButtonPrefab, characterButtonParent).GetComponent<CharacterButton>());
            characterButtons[i].UpdateButtonView(characters[i]);
            characterButtons[i].GetComponent<Button>().onClick.AddListener(() => SelectCharacter(playerSO));
        }
        if (characterButtons.Count > 0)
        {
            SelectCharacter(characters[0]);
        }
    }
    public void SelectCharacter(PlayerSO character)
    {
        settings.selectedPlayerCharacter = character;
        characterNameText.text = character.name;
        characterDescription.text = character.characterDescription;
        weaponSprite.sprite = character.startingWeapon.icon;
        weaponName.text = character.startingWeapon.name;
        weaponSprite.GetComponentInParent<HorizontalLayoutGroup>().CalculateLayoutInputHorizontal();

        StringBuilder sb = new StringBuilder();

        sb.Append("Health: ");
        int hp = character.entityStats.health;
        if (hp > 100) { sb.Append("<color=green>"); }
        else if (hp == 100) { sb.Append("<color=white>"); }
        else { sb.Append("<color=red>"); }
        sb.Append(character.entityStats.health).AppendLine("</color>");

        if (character.entityStats.damage != 0) { sb.Append("Damage: ").Append(character.entityStats.damage > 0 ? "<color=green>" : "<color=red>").Append(character.entityStats.damage).AppendLine("</color>"); }
        if (character.entityStats.moveSpeed != 3) { sb.Append("Speed: ").Append(character.entityStats.moveSpeed > 3 ? "<color=green>" : "<color=red>").Append(character.entityStats.moveSpeed).AppendLine("</color>"); }
        if (character.entityStats.defense != 0) { sb.Append("Defense: ").Append(character.entityStats.defense > 0 ? "<color=green>+" : "<color=red>").Append(character.entityStats.defense).AppendLine("</color>"); }
        if (character.entityStats.dodgeChance != 0) { sb.Append("Dodge: ").Append(character.entityStats.dodgeChance > 0 ? "<color=green>+" : "<color=red>").Append(character.entityStats.dodgeChance).AppendLine("%</color>"); }
        if (character.playerStats.pickupRadius != 0) { sb.Append("Pickup Radius: ").Append(character.playerStats.pickupRadius > 0f ? "<color=green>+" : "<color=red>").Append(character.playerStats.pickupRadius).AppendLine("</color>"); }
        if (character.playerStats.essenceGain != 0) { sb.Append("Essence Gain: ").Append(character.playerStats.essenceGain> 0f ? "<color=green>+" : "<color=red>").Append(character.playerStats.essenceGain).AppendLine("%</color>"); }
        if (character.playerStats.xpGain != 0) { sb.Append("XP Gain: ").Append(character.playerStats.xpGain > 0f ? "<color=green>+" : "<color=red>").Append(character.playerStats.xpGain).AppendLine("%</color>"); }
        if (character.playerStats.goldGain != 0) { sb.Append("Gold Gain: ").Append(character.playerStats.goldGain> 0f ? "<color=green>+" : "<color=red>").Append(character.playerStats.goldGain).AppendLine("%</color>"); }
        if (character.playerStats.luck != 0) { sb.Append("Luck: ").Append(character.playerStats.luck > 0 ? "<color=green>+" : "<color=red>").Append(character.playerStats.luck).AppendLine("</color>"); }

        characterStats.text = sb.ToString();
    }
}
