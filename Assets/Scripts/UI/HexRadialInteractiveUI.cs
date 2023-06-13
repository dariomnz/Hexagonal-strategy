using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using RotaryHeart.Lib.SerializableDictionary;

public interface IInteractive
{

    public Dictionary<HexInteration, UnityAction> GetInteractions();
    /// <summary>
    ///  Return a dictionary of callbacks to the actions
    /// </summary>
    public void Interact();
}

public enum HexInteration
{
    None,
    Close,
    UnitMove,
    UnitAttack,
    CellCreateUnit,
    CellCreateUnitKnight,
    CellCreateUnitBarbarian,
    CellCreateUnitMage,
    CellCreateUnitRogue,
}

public class HexRadialInteractiveUI : MonoBehaviour
{
    public float Radius = 1f;

    public SerializableDictionaryBase<HexInteration, HexRadialButtomUI> interactionPrefabs;

    public void CreateMenu(Dictionary<HexInteration, UnityAction> interations)
    {
        Clean();
        if (interations == null)
            return;
        List<KeyValuePair<HexInteration, UnityAction>> listinterations = interations.ToList();
        listinterations.Add(new KeyValuePair<HexInteration, UnityAction>(HexInteration.Close, () => { }));
        int i = 1;

        foreach (KeyValuePair<HexInteration, UnityAction> entry in listinterations)
        {
            HexRadialButtomUI newButton = Instantiate(interactionPrefabs[entry.Key], transform);
            Vector3 newPos = Quaternion.AngleAxis(i * (360f / listinterations.Count), Vector3.forward) * (Vector3.down * Radius);
            LeanTween.moveLocal(newButton.gameObject, newPos, 0.3f).setEase(LeanTweenType.easeOutBounce);
            newButton.Action = entry.Value;
            if (entry.Key == HexInteration.Close)
                newButton.Action += () => { HexGameUI.Instance.CloseInteraction(); };
            i++;
        }
    }

    public void Clean()
    {
        LeanTween.cancel(gameObject);
        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }
}
