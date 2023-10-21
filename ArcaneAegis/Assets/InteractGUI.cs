using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public enum InteractEnum {
    PickupUpgrade,
    BuyKey,
    DoorRevive,
    TechRune,
    FireCatalyst,
    GravityCatalyst,
    IceCatalyst,
    LightningCatalyst,
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
        this.interactEnum = interactEnum;
        for (int i = 0; i < interactObjects.Length; i++) {
            interactObjects[i].SetActive(i == (int) interactEnum);
        }
    } 

    public GameObject InteractObject() {
        if (interactEnum == InteractEnum.None) return interactObjects[0];
        Debug.Log("InteractEnum: " + (int) interactEnum + " " + interactObjects[(int) interactEnum].name);
        return interactObjects[(int) interactEnum];
    }

    public InteractElement GetInteractElement() {
        return InteractObject().GetComponent<InteractElement>();
    }

}
