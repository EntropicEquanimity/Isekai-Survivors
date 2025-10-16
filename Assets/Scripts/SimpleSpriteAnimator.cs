using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;

[RequireComponent(typeof(SpriteRenderer))]
public class SimpleSpriteAnimator : MonoBehaviour
{
    [BoxGroup("Status")] [ReadOnly] public bool isPlaying = false;

    [BoxGroup("Sprites")] public List<Sprite> animationSprites = new List<Sprite>();
    [BoxGroup("Sprites")] public int framesPerSecond = 50;

    [BoxGroup("Settings")] public bool playOnEnable;
    [BoxGroup("Settings")] public bool looping;
    [BoxGroup("Settings")] public bool disableOnComplete;
    [BoxGroup("Settings")] public bool destroyOnComplete;

    public Action OnComplete;

    private SpriteRenderer _sr;
    private int currentlySelectedSprite = 0;
    private float _frameLength;
    private float _waitTime = 0;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }
    public void OnEnable()
    {
        Restart();
    }
    public void Restart()
    {
        isPlaying = playOnEnable;
        currentlySelectedSprite = 0;
        _frameLength = 1f / framesPerSecond;
        _waitTime = 0;
    }
    public void Stop()
    {
        isPlaying = false;
        if (destroyOnComplete) { Destroy(gameObject); }
        else if (disableOnComplete) { gameObject.SetActive(false); }
    }
    public void Pause()
    {
        isPlaying = false;
    }
    public void Resume()
    {
        isPlaying = true;
    }
    void Update()
    {
        if (!isPlaying) { return; }
        if (_waitTime > 0f)
        {
            _waitTime -= Time.deltaTime;
        }
        else
        {
            _sr.sprite = animationSprites[currentlySelectedSprite];
            currentlySelectedSprite++;

            if (currentlySelectedSprite >= animationSprites.Count)
            {
                OnComplete?.Invoke();
                currentlySelectedSprite = 0;
                if (destroyOnComplete) { Destroy(gameObject); }
                else if (disableOnComplete) { gameObject.SetActive(false); }
                else if (looping) { currentlySelectedSprite = 0; }
            }
            _waitTime = _frameLength;
        }
    }
}
