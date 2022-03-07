using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Pickup : MonoBehaviour
{
    public ItemSO item;
    private void Start()
    {
        Initialize();
    }
    public abstract void Initialize();
    public abstract void OnPickup();
}
