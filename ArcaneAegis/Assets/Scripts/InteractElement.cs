using UnityEngine;
using UnityEngine.UI;

public class InteractElement : MonoBehaviour
{
    [SerializeField] private Text costText;

    public void SetCost(float cost, bool canAfford) {
        if (costText == null) return;
        costText.text = ((int)cost).ToString();
        costText.color = canAfford ? Color.white : Color.red;
    }
}
