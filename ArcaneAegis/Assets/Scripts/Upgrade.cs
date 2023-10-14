using UnityEngine;
using Unity.Netcode;
using System;
using Unity.VisualScripting;
using UnityEngine.UI;

public enum ElementEnum
{
    Fire,
    Lightning,
    Gravity,
    Ice
}

public enum UpgradeEnum
{
    Damage,
    Cooldown,
    Cost
}



public class Upgrade : NetworkBehaviour
{

    public ElementEnum upGradeElement;
    [SerializeField] private Color baseColour; 
    [SerializeField] private GameObject sphere;
    public UpgradeEnum upgradeType;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        sphere.GetComponent<Renderer>().material.color = baseColour;
        upgradeType = (UpgradeEnum)UnityEngine.Random.Range(0, 3);
    }

    public string GetUpgradeText() {
        return upGradeElement.ToString() + " " + upgradeType.ToString() + " Upgrade\nPress E to pick up";
    }

    [ServerRpc(Delivery = default, RequireOwnership = false)]
    public void PickupUpgradeServerRpc(ServerRpcParams rpcParams = default)
    {
        gameObject.GetComponent<NetworkObject>().Despawn(true);
    }
}
