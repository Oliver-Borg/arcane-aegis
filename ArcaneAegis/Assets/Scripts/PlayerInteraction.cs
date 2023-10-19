using UnityEngine;
using Unity.Netcode;
using TMPro;

public class PlayerInteraction : NetworkBehaviour
{
    [SerializeField] private float interactionRange = 1f;
    [SerializeField] private LayerMask interactionLayer;
    [SerializeField] private Camera playerCamera;

    [SerializeField] private TextMeshProUGUI interactionText;

    void Update()
    {
        if (!IsOwner) return;

        Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactionRange, Color.red);

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, interactionRange, interactionLayer))
        {

            Debug.Log("Hit " + hit.collider.gameObject.name);
            if (hit.collider.gameObject.TryGetComponent(out Upgrade upgrade))
            {
                if (upgrade.upgradeType == UpgradeEnum.Catalyst) {
                    if (GetComponent<PlayerInventory>().catalystUpgrade != ElementEnum.None) {
                        interactionText.text = "You already have a catalyst upgrade";
                        return;
                    }
                } else if (upgrade.upgradeType == UpgradeEnum.TechRune) {
                    if (GetComponent<PlayerInventory>().HasTechRune()) {
                        interactionText.text = "You already have a tech rune";
                        return;
                    }
                } else if (GetComponent<PlayerInventory>().HasUpgrade()) {
                    interactionText.text = "You already have an inactive upgrade";
                    return;
                }
                
                interactionText.text = upgrade.GetUpgradeText();
                if (Input.GetKeyDown(KeyCode.E))
                {
                    // TODO Do this properly with a server rpc to prevent multiple players picking it up
                    if (upgrade.upgradeType == UpgradeEnum.Catalyst) {
                        GetComponent<PlayerInventory>().catalystUpgrade = upgrade.upGradeElement;
                    } else if (upgrade.upgradeType == UpgradeEnum.TechRune) {
                        GetComponent<PlayerInventory>().AddTechRuneServerRpc();
                    } else {
                        GetComponent<PlayerInventory>().SetUpgrade(new UpgradeEnums {
                            upgradeType = upgrade.upgradeType,
                            element = upgrade.upGradeElement
                        });
                    }
                    upgrade.PickupUpgradeServerRpc();
                }
            }
            else if (hit.transform.parent != null && hit.transform.parent.TryGetComponent(out Door door))
            {
                if (door.IsOpen()) return;
                PlayerInventory inventory = GetComponent<PlayerInventory>();
                interactionText.text = "Press E to open door " + inventory.GetKeys() + " / 1 key";
                if (Input.GetKeyDown(KeyCode.E))
                {
                    
                    if (inventory.GetKeys() > 0)
                    {
                        inventory.UseKeyServerRpc();
                        door.OpenServerRpc();
                    }
                }
            }
            else if (hit.collider.GetComponentInParent<PlayerController>() != null)
            {
                PlayerController player = hit.collider.GetComponentInParent<PlayerController>();
                if (!player.IsDead()) return;
                // TODO fix collider orientation
                PlayerInventory inventory = GetComponent<PlayerInventory>();
                interactionText.text = "Press E to revive player " + inventory.GetKeys() + " / 1 key";
                if (Input.GetKeyDown(KeyCode.E))
                {
                    
                    if (inventory.GetKeys() > 0)
                    {
                        inventory.UseKeyServerRpc();
                        player.ReviveServerRpc();
                    }
                }
            }
            else if (hit.transform.TryGetComponent(out Teleporter teleporter)) {
                interactionText.text = "Press E to teleport";
                if (Input.GetKeyDown(KeyCode.E)) {
                    Transform targetTransform = teleporter.GetTeleportTransform();
                    teleporter.TeleportServerRpc();
                    // Enable space station
                    if (teleporter.spaceStation != null) {
                        teleporter.spaceStation.SetActive(true);
                    }
                    transform.position = targetTransform.position;
                    transform.rotation = targetTransform.rotation;
                }
            }
            else if (hit.transform.TryGetComponent(out Alchemist alchemist)) {
                PlayerInventory inventory = GetComponent<PlayerInventory>();
                interactionText.text = "Press E to buy a key";
                if (Input.GetKeyDown(KeyCode.E)) {
                    alchemist.BuyKey();
                    inventory.BuyKeyServerRpc();
                }
                if (inventory.HasUpgrade() && !alchemist.HasUpgrade()) {
                    string upgradeText = inventory.GetUpgrade().ToString();
                    interactionText.text += "\nPress F to activate " + upgradeText + " upgrade for " + inventory.UpgradeCost() + " points";
                    if (Input.GetKeyDown(KeyCode.F) && inventory.GetPoints() >= inventory.UpgradeCost()) {
                        alchemist.SetUpgrade(inventory.PopUpgrade());
                    }
                } else if (alchemist.HasUpgrade()) {
                    string upgradeText = alchemist.GetUpgrade().ToString();
                    PlayerAttack attack = GetComponent<PlayerAttack>();
                    if (attack.UpgradeCount() < 6) {
                        interactionText.text += "\n press F to equip " + upgradeText + " upgrade";
                        if (Input.GetKeyDown(KeyCode.F)) {
                            attack.AddUpgrade(alchemist.BuyUpgrade(), attack.UpgradeCount());
                        }
                    } else {
                        interactionText.text += "\n press 1-6 to replace upgrade with " + upgradeText;
                        for (int i = 0; i < 6; i++) {
                            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) {
                                attack.AddUpgrade(alchemist.BuyUpgrade(), i);
                            }
                        }
                    }                 
                }
            }
            else
            {
                interactionText.text = "";
            }
        }
        else
        {
            interactionText.text = "";
        }
    }

}
