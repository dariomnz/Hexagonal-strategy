using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    public static LoadingScreen Instance { get; private set; }

    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.

        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public Slider loadingBar;

    public void Open()
    {
        loadingBar.value = 0;
        GetComponent<Canvas>().enabled = true;
    }

    public void UpdateLoading(float percentage)
    {
        loadingBar.value = percentage;
    }

    public void Close()
    {
        GetComponent<Canvas>().enabled = false;
    }
}
