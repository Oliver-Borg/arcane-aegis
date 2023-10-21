using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PlayerGUI : NetworkBehaviour
{
    [SerializeField] private Text pointsText;
    [SerializeField] private Text keysText;
    [SerializeField] private Canvas gui;
    [SerializeField] private Image leftRecharge;
    [SerializeField] private Image rightRecharge;
    [SerializeField] private GameObject [] leftSpells;
    [SerializeField] private GameObject [] rightSpells;
    [SerializeField] private Image healthBall;
    [SerializeField] private Image healthSheen;
    [SerializeField] private GameObject inactiveUpgradeIcon;
    [SerializeField] private GameObject [] upgradeIcons;
    [SerializeField] private GameObject [] shopUpgradeIcons;
    [SerializeField] private GameObject buyUpgradeIcon;
    [SerializeField] private GameObject shopGui;
    [SerializeField] private GameObject hintGui;
    [SerializeField] private GameObject techRuneIcon;
    [SerializeField] private GameObject [] catalystIcons;
    [SerializeField] private Text roundText;
    [SerializeField] private GameObject gameLostGui;
    [SerializeField] private GameObject gameWonGui;

    private PlayerAttack playerAttack;
    private PlayerInventory playerInventory;
    private PlayerInteraction playerInteraction;
    private PlayerController playerController;
    private GameManager gameManager;

    private bool paused = false;
    

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        gui.enabled = IsOwner;
        if (!IsOwner) return;
        playerAttack = GetComponent<PlayerAttack>();
        playerInventory = GetComponent<PlayerInventory>();
        playerInteraction = GetComponent<PlayerInteraction>();
        playerController = GetComponent<PlayerController>();
        GameObject gameManagerObject = GameObject.Find("GameManager");
        gameManager = gameManagerObject.GetComponent<GameManager>();
    }


    void Update() {
        if (!IsOwner) return;
        
        // Pause
        if (Input.GetKeyDown(KeyCode.Escape)) {
            paused = !paused;
            hintGui.SetActive(paused);
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;
        }

        pointsText.text = "" + (int) playerInventory.GetPoints();
        keysText.text = "" + playerInventory.GetKeys();
        int rightIndex = playerAttack.rightIndex;
        int leftIndex = playerAttack.leftIndex;

        // Disable all GameObjects except the current spell
        for (int i = 0; i < leftSpells.Length; i++) leftSpells[i].SetActive(i == leftIndex);
        for (int i = 0; i < rightSpells.Length; i++) rightSpells[i].SetActive(i == rightIndex);
        
        // Set recharge bars
        leftRecharge.fillAmount = 1 - playerAttack.GetChargeRatio(leftIndex);
        rightRecharge.fillAmount = 1 - playerAttack.GetChargeRatio(rightIndex);

        // Set health ball
        healthBall.fillAmount = playerController.GetHealthRatio();

        // Set health sheen
        float healthRatio = playerController.GetHealthRatio();
        Color healthColor = healthSheen.color;
        healthColor.a = Mathf.Lerp(0.3f, 0f, healthRatio);
        healthSheen.color = healthColor;
        // Set upgrade icon

        UpgradeEnums inactiveUpgrade = playerInventory.GetUpgrade();
        if (inactiveUpgrade == null) {
            inactiveUpgradeIcon.SetActive(false);
        } else {
            inactiveUpgradeIcon.SetActive(true);
            inactiveUpgradeIcon.GetComponent<UpgradeGui>().SetUpgrade(inactiveUpgrade.upgradeType, inactiveUpgrade.element);
        }

        // Set Upgrade icons and shop icons
        UpgradeEnums [] upgrades = playerAttack.GetUpgrades();
        for (int i = 0; i < upgradeIcons.Length; i++) {
            UpgradeEnums upgradei = null;
            if (i < upgrades.Length) upgradei = upgrades[i];
            UpgradeGui upgradeGui = upgradeIcons[i].GetComponent<UpgradeGui>();
            UpgradeGui shopUpgradeGui = shopUpgradeIcons[i].GetComponent<UpgradeGui>();
            if (upgradei == null) {
                upgradeGui.SetUpgrade(UpgradeEnum.Catalyst, ElementEnum.None);
                shopUpgradeGui.SetUpgrade(UpgradeEnum.Catalyst, ElementEnum.None);
            } else {
                upgradeGui.SetUpgrade(upgradei.upgradeType, upgradei.element);
                shopUpgradeGui.SetUpgrade(upgradei.upgradeType, upgradei.element);
            }
        }

        // Set shop active icon and display gui
        bool shopOpen = playerInteraction.shopOpen;
        shopGui.SetActive(shopOpen);
        if (shopOpen) {
            UpgradeEnums shopUpgrade = playerInteraction.shopUpgrade;
            buyUpgradeIcon.GetComponent<UpgradeGui>().SetUpgrade(shopUpgrade.upgradeType, shopUpgrade.element);
        }

        // Set tech rune icon
        techRuneIcon.SetActive(playerInventory.HasTechRune());

        // Set catalyst icons
        ElementEnum catalystUpgrade = playerInventory.catalystUpgrade;
        for (int i = 0; i < catalystIcons.Length; i++) {
            catalystIcons[i].SetActive(i == (int) catalystUpgrade);
        }

        // Set round text
        roundText.text = "Round " + gameManager.GetRound();

        // Set game lost gui
        gameLostGui.SetActive(gameManager.GameLost());

        // Set game won gui
        gameWonGui.SetActive(gameManager.GameWon());
    }
}
