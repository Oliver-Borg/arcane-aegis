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

    private PlayerAttack playerAttack;
    private PlayerInventory playerInventory;
    private PlayerInteraction playerInteraction;

    private PlayerController playerController;

    

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        gui.enabled = IsOwner;
        if (!IsOwner) return;
        playerAttack = GetComponent<PlayerAttack>();
        playerInventory = GetComponent<PlayerInventory>();
        playerInteraction = GetComponent<PlayerInteraction>();
        playerController = GetComponent<PlayerController>();
    }


    void Update() {
        if (!IsOwner) return;
        
        pointsText.text = "" + (int) playerInventory.GetPoints();
        keysText.text = "" + playerInventory.GetKeys();
        int rightIndex = playerAttack.rightIndex;
        int leftIndex = playerAttack.leftIndex;

        // Disable all GameObjects except the current spell
        for (int i = 0; i < leftSpells.Length; i++) leftSpells[i].SetActive(i == leftIndex);
        for (int i = 0; i < rightSpells.Length; i++) rightSpells[i].SetActive(i == rightIndex);
        
        // Set recharge bars
        leftRecharge.fillAmount = playerAttack.GetChargeRatio(leftIndex);
        rightRecharge.fillAmount = playerAttack.GetChargeRatio(rightIndex);

        // Set health ball
        healthBall.fillAmount = playerController.GetHealthRatio();
    }
}
