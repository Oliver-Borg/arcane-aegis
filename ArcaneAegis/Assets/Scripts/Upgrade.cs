using UnityEngine;
using Unity.Netcode;

public enum ElementEnum
{
    Fire,
    Gravity,
    Ice, 
    Lightning,
    None
}

public enum UpgradeEnum
{
    Damage,
    Cooldown,
    Cost,
    Catalyst,
    TechRune,
    None
}

public class UpgradeEnums {
    public UpgradeEnum upgradeType;
    public ElementEnum element;

    public override string ToString() {
        return element.ToString() + " " + upgradeType.ToString();
    }
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
        if (upgradeType != UpgradeEnum.Catalyst && upgradeType != UpgradeEnum.TechRune)
            upgradeType = (UpgradeEnum)UnityEngine.Random.Range(0, 3);
    }

    public string GetUpgradeText() {
        if (upGradeElement == ElementEnum.None)
            return upgradeType.ToString() + " Upgrade\nPress F to pick up";
        return upGradeElement.ToString() + " " + upgradeType.ToString() + " Upgrade\nPress F to pick up";
    }

    [ServerRpc(Delivery = default, RequireOwnership = false)]
    public void PickupUpgradeServerRpc(ServerRpcParams rpcParams = default)
    {
        gameObject.GetComponent<NetworkObject>().Despawn(true);
    }
}
