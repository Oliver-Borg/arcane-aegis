using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class PlayerInteraction : NetworkBehaviour
{
    [SerializeField] private float interactionRange = 1f;
    [SerializeField] private LayerMask interactionLayer;
    [SerializeField] private Camera playerCamera;

    [SerializeField] private Text interactionText;
    [SerializeField] private GameObject interactionGUI;
    [SerializeField] private GameObject shopUpgradeCost;
    [SerializeField] private GameObject upgradePickupIcon;

    public UpgradeEnums shopUpgrade = new UpgradeEnums {
        upgradeType = UpgradeEnum.None,
        element = ElementEnum.None
    };

    public bool shopOpen = false;

    void Update()
    {
        if (!IsOwner) return;

        Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactionRange, Color.red);
        interactionGUI.SetActive(false);
        shopOpen = false;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, interactionRange, interactionLayer))
        {
            InteractGUI interactGUI = interactionGUI.GetComponent<InteractGUI>();
            
            Debug.Log("Hit " + hit.collider.gameObject.name);
            if (hit.collider.gameObject.TryGetComponent(out Upgrade upgrade))
            {
                if (upgrade.upgradeType == UpgradeEnum.Catalyst) {
                    

                    if (GetComponent<PlayerInventory>().catalystUpgrade != ElementEnum.None) {
                        interactionText.text = "You already have a catalyst upgrade";
                        return;
                    }
                    if (upgrade.upGradeElement == ElementEnum.Fire)
                        interactGUI.SetInteract(InteractEnum.FireCatalyst);
                    else if (upgrade.upGradeElement == ElementEnum.Gravity)
                        interactGUI.SetInteract(InteractEnum.GravityCatalyst);
                    else if (upgrade.upGradeElement == ElementEnum.Ice)
                        interactGUI.SetInteract(InteractEnum.IceCatalyst);
                    else if (upgrade.upGradeElement == ElementEnum.Lightning)
                        interactGUI.SetInteract(InteractEnum.LightningCatalyst);
                    
                } else if (upgrade.upgradeType == UpgradeEnum.TechRune) {
                    if (GetComponent<PlayerInventory>().HasTechRune()) {
                        interactionText.text = "You already have a tech rune";
                        return;
                    }
                    interactGUI.SetInteract(InteractEnum.TechRune);

                } 
                else {
                    interactGUI.SetInteract(InteractEnum.PickupUpgrade);
                }
                
                upgradePickupIcon.GetComponent<UpgradeGui>().SetUpgrade(upgrade.upgradeType, upgrade.upGradeElement);
                interactionGUI.SetActive(true);
                if (Input.GetKeyDown(KeyCode.F))
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
                interactGUI.SetInteract(InteractEnum.DoorRevive);
                interactionGUI.SetActive(true);
                InteractElement interactElement = interactGUI.GetInteractElement();
                interactElement.SetCost(1, inventory.GetKeys() > 0);

                if (Input.GetKeyDown(KeyCode.F))
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
                interactGUI.SetInteract(InteractEnum.DoorRevive);
                if (Input.GetKeyDown(KeyCode.F))
                {
                    InteractElement interactElement = interactGUI.GetInteractElement();
                    interactElement.SetCost(1, inventory.GetKeys() > 0);
                    if (inventory.GetKeys() > 0)
                    {
                        inventory.UseKeyServerRpc();
                        player.ReviveServerRpc();
                    }
                }
            }
            else if (hit.transform.TryGetComponent(out Teleporter teleporter)) {
                interactionText.text = "Press F to time travel";
                if (Input.GetKeyDown(KeyCode.F)) {
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
                // interactionText.text = "Press E to buy a key";
                
                interactGUI.SetInteract(InteractEnum.BuyKey);
                InteractElement interactElement = interactGUI.GetInteractElement();
                interactElement.SetCost(inventory.KeyCost(), inventory.GetPoints() >= inventory.KeyCost());
                interactionGUI.SetActive(true);
                bool canAffordKey = inventory.GetPoints() >= inventory.KeyCost();
                bool canAffordUpgrade = inventory.GetPoints() >= inventory.UpgradeCost();
                if (Input.GetKeyDown(KeyCode.F) && canAffordKey) {
                    alchemist.BuyKey();
                    inventory.BuyKeyServerRpc();
                }
                if (inventory.HasUpgrade()) {
                    InteractElement upgradeCost = shopUpgradeCost.GetComponent<InteractElement>();
                    
                    PlayerAttack attack = GetComponent<PlayerAttack>();
                    shopOpen = true;
                    shopUpgrade = inventory.GetUpgrade();
                    upgradeCost.SetCost(inventory.UpgradeCost(), canAffordUpgrade);
                    if (!canAffordUpgrade) return;
                    for (int i = 0; i < 6; i++) {
                        if (Input.GetKeyDown(KeyCode.Alpha1 + i)) {
                            alchemist.SetUpgrade(inventory.PopUpgrade());
                            attack.AddUpgrade(alchemist.BuyUpgrade(), i);
                        }
                    }                 
                }
                else {
                    shopOpen = false;
                }
            }
            else
            {
                interactGUI.SetInteract(InteractEnum.None);
                interactionText.text = "";
            }
        }
        else
        {
            interactionText.text = "";
        }
    }

}
