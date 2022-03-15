using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using NaughtyAttributes;

public class FadeManager : MonoBehaviour
{
    public CanvasGroup fadeCanvas;
    public Image fadeIn;
    public Image fadeOut;
    public float fadeSpeed = 2f;
    public static FadeManager Instance;
    private bool currentlyFading;
    private void Awake()
    {
        if(Instance == null) { Instance = this; DontDestroyOnLoad(this); }
        else { Destroy(gameObject); }
        fadeIn.material.SetFloat("_FadeAmount", 1f);
        fadeOut.material.SetFloat("_FadeAmount", 1f);
        ToggleCanvasGroup(false);
        currentlyFading = false;
    }
    [Button]
    public void StartFade(Action OnFadeIn = null, Action OnFadeOut = null)
    {
        if (currentlyFading) { Debug.LogWarning("Already fading! Make sure not to call this twice!"); return; }
        ToggleCanvasGroup(true);
        currentlyFading = true;
        StartCoroutine(FadeIn(OnFadeIn, OnFadeOut));
    }
    private void ToggleCanvasGroup(bool active)
    {
        fadeCanvas.alpha = active ? 1 : 0;
        fadeCanvas.interactable = active;
        fadeCanvas.blocksRaycasts = active;
    }
    private IEnumerator FadeIn(Action OnComplete, Action OnFadeOut)
    {
        float num = 1f;
        while(num > -0.1f)
        {
            fadeIn.material.SetFloat("_FadeAmount", num);
            num -= Time.deltaTime * fadeSpeed;
            yield return new WaitForEndOfFrame();
        }
        OnComplete?.Invoke();

        //Do something interesting;

        yield return new WaitForSeconds(0.5f);
        StartCoroutine(FadeOut(OnFadeOut));
    }
    private IEnumerator FadeOut(Action OnComplete)
    {
        fadeOut.material.SetFloat("_FadeAmount", -0.1f);
        fadeIn.material.SetFloat("_FadeAmount", 1f);
        float num = -0.1f;
        while (num < 1f)
        {
            fadeOut.material.SetFloat("_FadeAmount", num);
            num += Time.deltaTime * fadeSpeed;
            yield return new WaitForEndOfFrame();
        }
        ToggleCanvasGroup(false);

        OnComplete?.Invoke();

        currentlyFading = false;
    }
}
