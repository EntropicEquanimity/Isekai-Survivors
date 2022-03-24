using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NaughtyAttributes;

public class CharacterButton : MonoBehaviour
{
    public PlayerSO characterData;

    public Image image;
    public TMP_Text text;
    
    public void UpdateButtonView(PlayerSO characterData)
    {
        this.characterData = characterData;
        image.sprite = characterData.entitySprite;
        text.text = characterData.name;
    }
    [Button]
    public void UpdateButtonView(){
        UpdateButtonView(characterData);
    }
}
