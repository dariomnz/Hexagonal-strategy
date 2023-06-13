using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HexRadialButtomUI : MonoBehaviour
{
    UnityAction action;
    public UnityAction Action
    {
        get { return action; }
        set
        {
            action = value;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }
    }

    public Button button;

    void OnEnable()
    {
        button.onClick.AddListener(action);
    }

    void OnDisable()
    {
        button.onClick.RemoveListener(action);
    }
}
