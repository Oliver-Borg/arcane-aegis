using UnityEngine;
using UnityEngine.UI;

public class InteractElement : MonoBehaviour
{
    [SerializeField] private Text costText;

    public bool canAfford = false;


    public void SetCost(int cost) {
        costText.text = cost.ToString();
    }
    void Update() {
        costText.color = canAfford ? Color.white : Color.red;
    }
}
