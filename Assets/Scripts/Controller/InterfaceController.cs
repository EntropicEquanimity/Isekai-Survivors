using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using BlondieUtils;
using UnityEngine.EventSystems;
using DG.Tweening;

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

    [BoxGroup("GameOver")] public List<WeaponOverview> weaponSummaries;
    [BoxGroup("GameOver")] public CanvasGroup gameOverCanvas;

    public static InterfaceController Instance;
    void Update()
    {
        if (GameManager.Instance.GameState != GameState.Normal) { return; }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
        {
            if (Time.timeScale < 1f)
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
        gameOverCanvas.alpha = 0f;
        gameOverCanvas.interactable = false;
    }
    #region Items
    public void OpenChooseItemPanel(List<ItemSO> items, System.Action onChoose = null)
    {
        if (items.Count < 1) { Debug.LogError("Empty List Of Items was handed to the ItemPanel!"); return; }
        if (!Pause()) { return; }
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
    public bool Pause()
    {
        if (GameManager.Instance.GameState == GameState.Normal)
        {
            Time.timeScale = 0;
            GameManager.Instance.GameState = GameState.Paused;
            return true;
        }
        return false;
    }
    public bool Resume()
    { 
        if(GameManager.Instance.GameState == GameState.Paused)
        {
            Time.timeScale = 1;
            GameManager.Instance.GameState = GameState.Normal;
            return true;
        }
        return false;
    }
    public void OpenPauseMenu()
    {
        if (Pause()) pauseMenu.SetActive(true);
    }
    public void ClosePauseMenu()
    {
        if (Resume()) pauseMenu.SetActive(false);
    }
    public void GiveUp()
    {
        ClosePauseMenu();
        FindAnyObjectByType<Player>().TakeDamage(new DamageInfo(null, 999, 0, true));
    }
    public void Restart()
    {
        SceneManager.Instance.GoToGameScene();
    }
    public void GoToMainMenu()
    {
        SceneManager.Instance.GoToMainMenu();
    }
    public void Quit()
    {
#if UNITY_STANDALONE_WIN
        Application.Quit();
#endif
    }
    #endregion

    #region Game Over
    public void ShowGameOver(List<Equipment> weapons, float time)
    {
        gameOverCanvas.interactable = true;
        gameOverCanvas.DOFade(1f, 2f);
        DamageRecord[] damageRecord = new DamageRecord[weapons.Count];
        for (int i = 0; i < weapons.Count; i++)
        {
            damageRecord[i] = weapons[i].DamageRecord;
        }
        float totalDmg = 0;
        float totalKills = 0;
        for (int i = 0; i < damageRecord.Length; i++) 
        {
            totalDmg += damageRecord[i].damageDealt;
            totalKills += damageRecord[i].kills;
        }
        for (int i = 0; i < damageRecord.Length; i++)
        {
            weaponSummaries[i].gameObject.SetActive(true);
            weaponSummaries[i].Initialize(weapons[i].itemData.icon, damageRecord[i].damageDealt, damageRecord[i].kills, damageRecord[i].damageDealt / totalDmg, damageRecord[i].kills / totalKills);
        }
    }
    #endregion

    #region Player HUD
    public void UpdateHealthBar(int current, int max)
    {
        playerHealthBarFill.fillAmount = (float)current / (float)max;
        playerHealthText.text = current + "/" + max;
    }
    public void UpdateExpBar(int current, int max)
    {
        playerExpBarFill.fillAmount = (float)current / (float)max;
        //playerExpText.text = current + "/" + max;
    }
    public void UpdateKills(int kills)
    {
        playerKills.text = kills.ToString();
    }
    public void UpdatePlayerLevel(int level)
    {
        playerLevel.text = level.ToString();
    }
    #endregion
}
