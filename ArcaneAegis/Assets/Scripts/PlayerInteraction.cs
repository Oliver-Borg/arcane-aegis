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
                interactionText.text = upgrade.GetUpgradeText();
                if (Input.GetKeyDown(KeyCode.E))
                {
                    // TODO Do this properly with a server rpc to prevent multiple players picking it up
                    GetComponent<PlayerAttack>().AddUpgrade(upgrade);
                    upgrade.PickupUpgradeServerRpc();
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
