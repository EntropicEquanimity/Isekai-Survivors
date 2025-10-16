using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

[CreateAssetMenu(menuName = "Scriptable Singletons/Scene Manager")]
public class SceneManager : ScriptableObject
{
    private const string Path = "Singletons/Scene Manager";

    private static SceneManager instance;

    public static SceneManager Instance
    {
        get
        {
            if(instance == null)
            {
                Debug.Log("No instance of SceneManager found. Pulling from Resources/" + Path + " to create a new instance.");
                instance = Resources.Load<SceneManager>(Path);
            }
            return instance;
        }
    }
    [Button] public void GoToMainMenu() { ChangeScene(Scene.MainMenu, null); }
    [Button] public void GoToGameScene() => ChangeScene(Scene.Game, null);
    public void ChangeScene(Scene scene, Action OnFadeOut, Action OnFadeIn = null, bool useFade = true)
    {
        switch (scene)
        {
            case Scene.MainMenu:
                break;
            case Scene.Game:
                break;
            default:
                Debug.LogError("The target scene has not been added to the SceneManager yet! Returning to Main Menu");
                break;
        }
        Debug.Log("<b>SceneManager</b> switching scene to " + scene.ToString() + " from " + UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        if (FadeManager.Instance != null && useFade)
        {
            FadeManager.Instance.StartFadeIn(()=>UnityEngine.SceneManagement.SceneManager.LoadSceneAsync((int)scene, LoadSceneMode.Single), OnFadeOut);
        }
        else
        {
            OnFadeIn?.Invoke();
            UnityEngine.SceneManagement.SceneManager.LoadScene((int)scene, LoadSceneMode.Single);
            OnFadeOut?.Invoke();
        }
    }
}
public enum Scene
{
    MainMenu = 0,
    Game = 1
}