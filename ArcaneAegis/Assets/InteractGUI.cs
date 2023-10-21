using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public enum InteractEnum {
    PickupUpgrade,
    BuyKey,
    DoorRevive,
    None
}

public class InteractGUI : NetworkBehaviour
{
    [SerializeField] private GameObject [] interactObjects;

    public InteractEnum interactEnum = InteractEnum.PickupUpgrade;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        SetInteract(interactEnum);
    }

    public void SetInteract(InteractEnum interactEnum) {
        for (int i = 0; i < interactObjects.Length; i++) {
            interactObjects[i].SetActive(i == (int) interactEnum);
        }
    } 


}
