using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class NumberPopupManager : MonoBehaviour
{
    public static NumberPopupManager Instance;
    public GameObject prefab;
    [BoxGroup("Settings")] public int normalFontSize = 16;
    [BoxGroup("Settings")] public int critFontSize = 24;
    [BoxGroup("Settings")] public float duration = 0.5f;
    [BoxGroup("Settings")] public Color critColor = Color.red;
    [BoxGroup("Settings")] public Color normalColor = Color.black;
    [ReadOnly] public List<NumberPopup> numberPopups = new List<NumberPopup>();
    public void Awake()
    {
        if(Instance == null) { Instance = this; }
        numberPopups = new List<NumberPopup>();
    }

    public void DamageNumber(int damage, bool crit, Vector2 position)
    {
        NumberPopup popup = GetNumberPopup();
        popup.transform.position = position;
        popup.Initialize(damage.ToString(), crit ? critColor : normalColor, duration, crit ? critFontSize : normalFontSize);
    }
    public NumberPopup GetNumberPopup()
    {
        for (int i = 0; i < numberPopups.Count; i++)
        {
            if (!numberPopups[i].gameObject.activeInHierarchy)
            {
                NumberPopup numberPopup = numberPopups[i];
                numberPopup.gameObject.SetActive(true);
                return numberPopup;
            }
        }
        NumberPopup obj = CreateNewNumberPopup().GetComponent<NumberPopup>();
        numberPopups.Add(obj);
        return obj;
    }
    private GameObject CreateNewNumberPopup()
    {
        return Instantiate(prefab, this.transform);
    }
}
