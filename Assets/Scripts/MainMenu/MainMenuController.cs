using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    private void Start()
    {
        FadeManager.Instance.StartFadeOut(delegate
        {
            fightPanel.SetActive(false);
            upgradesPanel.SetActive(false);
            creditsPanel.SetActive(false);
        });
    }
    public GameObject fightPanel, upgradesPanel, creditsPanel;

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnPressEscape();
        }
    }

    private void OnPressEscape()
    {
        if (!fightPanel.activeInHierarchy && !creditsPanel.activeInHierarchy && !upgradesPanel.activeInHierarchy)
        {
            GiveUp();
        }
        else
        {
            ToggleFightMenu(false);
            ToggleUpgradesMenu(false);
            ToggleCreditsMenu(false);
        }
    }

    public void ToggleFightMenu(bool active)
    {
        if (fightPanel.activeInHierarchy == active) { return; }
        FadeManager.Instance.StartFadeIn(delegate
        {
            if (active)
            {
                fightPanel.SetActive(true);
            }
            else
            {
                fightPanel.SetActive(false);
            }
        });
    }
    public void ToggleUpgradesMenu(bool active)
    {
        if (upgradesPanel.activeInHierarchy == active) { return; }
        FadeManager.Instance.StartFadeIn(delegate
        {
            if (active)
            {
                upgradesPanel.SetActive(true);
            }
            else
            {
                upgradesPanel.SetActive(false);
            }
        });
    }
    public void ToggleCreditsMenu(bool active)
    {
        if (creditsPanel.activeInHierarchy == active) { return; }
        FadeManager.Instance.StartFadeIn(delegate
        {
            if (active)
            {
                creditsPanel.SetActive(true);
            }
            else
            {
                creditsPanel.SetActive(false);
            }
        });
    }
    public void GiveUp()
    {
        Debug.Log("Closing the game!");
        Application.Quit();
    }
    public void GoToCombatScene()
    {
        SceneManager.Instance.ChangeScene(Scene.Game, null);
    }
}
