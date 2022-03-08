using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class NumberPopup : MonoBehaviour
{
    public TMP_Text numText;
    public Ease animationEase;

    public void Initialize(string text, Color textColor, Vector2 position, int fontSize = 12, bool animated = true)
    {
        numText.text = text;
        numText.color = textColor;
        numText.fontSize = fontSize;
        transform.position = position;

        if (animated)
        {
            transform.DOMoveY(transform.position.y + 0.5f, 0.5f).SetEase(animationEase).OnComplete(() => gameObject.SetActive(false));
        }
        else
        {
            StartCoroutine(Delay(0.5f));
        }
    }
    private IEnumerator Delay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
}
