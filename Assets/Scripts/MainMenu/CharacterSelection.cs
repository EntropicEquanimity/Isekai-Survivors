using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
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
        if (character.entityStats.health != 100) { sb.Append(character.entityStats.health > 100 ? "<color=green>" : "<color=red>"); }
        sb.Append(character.entityStats.health).Append(character.entityStats.health != 100 ? "</color>" : "").AppendLine();

        sb.Append("Damage: ");
        if (character.entityStats.damage != 0) { sb.Append(character.entityStats.damage > 0 ? "<color=green>" : "<color=red>"); }
        sb.Append(character.entityStats.damage).Append(character.entityStats.damage != 0 ? "</color>" : "").AppendLine();

        sb.Append("Speed: ");
        if (character.entityStats.moveSpeed != 3) { sb.Append(character.entityStats.moveSpeed > 3 ? "<color=green>" : "<color=red>"); }
        sb.Append(character.entityStats.moveSpeed).Append(character.entityStats.moveSpeed != 3 ? "</color>" : "").AppendLine();

        sb.Append("Defense: ");
        if (character.entityStats.defense != 0) { sb.Append(character.entityStats.defense > 0 ? "<color=green>" : "<color=red>"); }
        sb.Append(character.entityStats.defense).Append(character.entityStats.defense != 0 ? "</color>" : "").AppendLine();

        characterStats.text = sb.ToString();
    }
}
