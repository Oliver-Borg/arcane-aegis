using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;

public class PlayerInventory : NetworkBehaviour
{
    // This script is used to store key counts, inactive upgrades and points

    [SerializeField] private int startKeys = 0;

    [SerializeField] private float startPoints = 500;
    NetworkVariable<int> keys = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    NetworkVariable<float> points = new NetworkVariable<float>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    NetworkVariable<bool> techRune = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    private UpgradeEnums upgrade = null;

    [SerializeField] private float keyCost = 500f;

    [SerializeField] private float upgradeCost = 500f;

    public ElementEnum catalystUpgrade = ElementEnum.None;

    public override void OnNetworkSpawn() {
        if (IsServer) {
            keys.Value = startKeys;
            points.Value = startPoints;
        }
    }

    public float UpgradeCost() {
        return upgradeCost;
    }

    [ServerRpc(Delivery = default, RequireOwnership = false)]
    public void AddPointsServerRpc(float amount, ServerRpcParams rpcParams = default) {
        points.Value += amount;
    }

    public float GetPoints() {
        return points.Value;
    }

    [ServerRpc(Delivery = default, RequireOwnership = false)]
    public void BuyKeyServerRpc(ServerRpcParams rpcParams = default) {
        if (points.Value >= keyCost) {
            points.Value -= keyCost;
            keys.Value++;
        }
    }

    public int GetKeys() {
        return keys.Value;
    }

    [ServerRpc(Delivery = default, RequireOwnership = false)]
    public void UseKeyServerRpc(ServerRpcParams rpcParams = default) {
        if (keys.Value > 0) {
            keys.Value--;
        }
    }

    [ServerRpc(Delivery = default, RequireOwnership = false)]
    public void AddKeyServerRpc(ServerRpcParams rpcParams = default) {
        keys.Value++;
    }

    [ServerRpc(Delivery = default, RequireOwnership = false)]
    public void AddTechRuneServerRpc(ServerRpcParams rpcParams = default) {
        techRune.Value = true;
    }

    [ServerRpc(Delivery = default, RequireOwnership = false)]
    public void RemoveTechRuneServerRpc(ServerRpcParams rpcParams = default) {
        techRune.Value = false;
    }

    public bool HasTechRune() {
        return techRune.Value;
    }

    public bool HasUpgrade() {
        return upgrade != null;
    }

    public UpgradeEnums GetUpgrade() {
        return upgrade;
    }

    public void SetUpgrade(UpgradeEnums upgrade) {
        this.upgrade = upgrade;
    }

    public UpgradeEnums PopUpgrade() {
        UpgradeEnums upgrade = this.upgrade;
        this.upgrade = null;
        points.Value -= upgradeCost;
        return upgrade;
    }
}
