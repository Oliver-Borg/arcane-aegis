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
                }
                interactionText.text = upgrade.GetUpgradeText();
                if (Input.GetKeyDown(KeyCode.E))
                {
                    // TODO Do this properly with a server rpc to prevent multiple players picking it up
                    if (upgrade.upgradeType == UpgradeEnum.Catalyst) {
                        GetComponent<PlayerInventory>().catalystUpgrade = upgrade.upGradeElement;
                    } else {
                        GetComponent<PlayerAttack>().AddUpgrade(upgrade);
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
            else
            {
                interactionText.text = "";
            } // TODO Add doors and alchemist
        }
        else
        {
            interactionText.text = "";
        }
    }

}
