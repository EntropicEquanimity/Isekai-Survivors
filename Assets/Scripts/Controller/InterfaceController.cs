using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using NaughtyAttributes;
using BlondieUtils;
using UnityEngine.EventSystems;

public class InterfaceController : MonoBehaviour
{
    [BoxGroup("HUD")] public TMP_Text playerHealthText;
    [BoxGroup("HUD")] public Image playerHealthBarFill;
    [BoxGroup("HUD")] public TMP_Text playerExpText;
    [BoxGroup("HUD")] public Image playerExpBarFill;
    [BoxGroup("HUD")] public TMP_Text gameTimeText;
    [BoxGroup("HUD")] public TMP_Text playerLevel;
    [BoxGroup("HUD")] public TMP_Text playerKills;
    [BoxGroup("HUD")] public TMP_Text playerEssence;

    [BoxGroup("Pause")] public GameObject pauseMenu;

    [BoxGroup("Items")] public Transform itemChoicePanelParent;
    [BoxGroup("Items")] public GameObject itemChoiceCardPrefab;
    [BoxGroup("Items")] public List<ItemCard> itemCards;

    public static InterfaceController Instance;
    void Update()
    {
        if (GameManager.Instance.GameState != GameState.Normal) { return; }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
        {
            if (Time.timeScale == 0)
            {
                ClosePauseMenu();
            }
            else
            {
                OpenPauseMenu();
            }
        }

        gameTimeText.text = Utils.FormatTimeToMinutes(GameManager.Instance.gameTime);
        UpdateKills(GameManager.Instance.PlayerKills);
    }
    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        for (int i = 0; i < 5; i++)
        {
            itemCards.Add(Instantiate(itemChoiceCardPrefab, itemChoicePanelParent).GetComponent<ItemCard>());
            itemCards[i].gameObject.SetActive(false);
        }
        itemChoicePanelParent.gameObject.SetActive(false);
    }
    #region Items
    public void OpenChooseItemPanel(List<ItemSO> items, System.Action onChoose = null)
    {
        if (items.Count < 1)
        {
            Debug.LogError("Empty List Of Items was handed to the ItemPanel!");
            return;
        }
        Pause();
        itemChoicePanelParent.gameObject.SetActive(true);
        int count = items.Count > 5 ? 5 : items.Count;
        for (int i = 0; i < itemCards.Count; i++)
        {
            if (i < count)
            {
                itemCards[i].Initialize(items[i], onChoose);
                itemCards[i].gameObject.SetActive(true);
            }
            else
            {
                itemCards[i].gameObject.SetActive(false);
            }
        }
        EventSystem.current.SetSelectedGameObject(itemCards[0].gameObject);
    }
    public void CloseChooseItemPanel()
    {
        for (int i = 0; i < itemCards.Count; i++)
        {
            itemCards[i].gameObject.SetActive(false);
        }
        itemChoicePanelParent.gameObject.SetActive(false);
        Resume();
    }
    #endregion

    #region Pause Menu
    public void Pause()
    {
        Time.timeScale = 0;
        GameManager.Instance.GameState = GameState.Paused;
    }
    public void Resume()
    {
        Time.timeScale = 1;
        GameManager.Instance.GameState = GameState.Normal;
    }
    public void OpenPauseMenu()
    {
        Pause();
        pauseMenu.SetActive(true);
    }
    public void ClosePauseMenu()
    {
        Resume();
        pauseMenu.SetActive(false);
    }
    public void GiveUp()
    {
        Debug.LogError("Give up not implemented yet!");
    }
    public void Quit()
    {
        Application.Quit();
    }
    #endregion

    #region Player HUD
    public void UpdateHealthBar(int current, int max)
    {
        playerHealthBarFill.fillAmount = (float)current / (float)max;
        playerHealthText.text = current + " / " + max;
    }
    public void UpdateExpBar(int current, int max)
    {
        playerExpBarFill.fillAmount = (float)current / (float)max;
        playerExpText.text = current + " / " + max;
    }
    public void UpdateKills(int kills)
    {
        playerKills.text = kills.ToString();
    }
    public void UpdatePlayerLevel(int level)
    {
        playerLevel.text = "LVL " + level.ToString();
    }
    #endregion
}
