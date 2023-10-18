using UnityEngine;
using Unity.Netcode;
using TMPro;

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

    [SerializeField] private float keyCost = 500f;
    [SerializeField] private TextMeshProUGUI pointsText;
    [SerializeField] private TextMeshProUGUI keysText;

    public ElementEnum catalystUpgrade = ElementEnum.None;

    public override void OnNetworkSpawn() {
        if (IsServer) {
            keys.Value = startKeys;
            points.Value = startPoints;
        }
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

    void Update() {
        if (!IsOwner) return;
        pointsText.text = "Points: " + (int) points.Value;
        keysText.text = "Keys: " + keys.Value;
    }
}
