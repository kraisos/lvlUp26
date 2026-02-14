using UnityEngine;
using TMPro;

public class WinTextScreen : MonoBehaviour
{
    private static readonly string[] victoryMessages = new string[]
    {
        "Post tenebras lux!",
        "La lumière est revenue... l'espoir renaît.",
        "Le courant est rétabli, les ténèbres reculent !",
        "L'énergie circule à nouveau, la vie reprend ses droits.",
        "Un phare dans la nuit — l'humanité persévère.",
        "La flamme de l'espoir ne s'éteint jamais.",
        "Le monde s'illumine, un pas vers demain.",
        "Là où il y a de la lumière, il y a de l'espoir.",
        "Les ombres se dissipent, place à la lumière !",
        "Le pouvoir de la lumière triomphe des ténèbres.",
        "Une étincelle suffit à rallumer l'espoir.",
        "La nuit est finie, l'aube se lève enfin.",
        "Brille encore, petite lumière, le monde a besoin de toi.",
    };

    void Start()
    {
        TMP_Text subtitle = null;
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text t in texts)
        {
            if (t.gameObject.name == "subtitle")
            {
                subtitle = t;
                break;
            }
        }

        if (subtitle != null)
        {
            subtitle.text = victoryMessages[Random.Range(0, victoryMessages.Length)];
        }
        else
        {
            Debug.LogWarning("WinTextScreen: No TMP_Text named 'subtitle' found in children.");
        }
    }
}
