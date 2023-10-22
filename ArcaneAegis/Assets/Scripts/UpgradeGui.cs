using UnityEngine;
using UnityEngine.UI;

public class UpgradeGui : MonoBehaviour
{
    [SerializeField] private GameObject [] upgradeIcons;
    [SerializeField] private Color [] elementColours;
    [SerializeField] private Image iconBG;

    public void SetUpgrade (UpgradeEnum upgradeEnum, ElementEnum elementEnum) {
        if (elementEnum == ElementEnum.None) {
            foreach (GameObject icon in upgradeIcons) {
                icon.SetActive(false);
            }
            iconBG.color = Color.white;
            return;
        }
        if (upgradeEnum == UpgradeEnum.Catalyst || upgradeEnum == UpgradeEnum.TechRune) {
            foreach (GameObject icon in upgradeIcons) {
                icon.SetActive(false);
            }
            return;
        }
        for (int i = 0; i < upgradeIcons.Length; i++) {
            upgradeIcons[i].SetActive(i == (int) upgradeEnum);
        }
        iconBG.color = elementColours[(int) elementEnum];
    }
}
