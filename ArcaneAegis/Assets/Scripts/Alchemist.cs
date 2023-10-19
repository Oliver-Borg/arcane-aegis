using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;

public class Alchemist : NetworkBehaviour
{
    private UpgradeEnums upgrade; // Upgrade will be local

    private Animator animator;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        animator = GetComponent<Animator>();
    }

    public bool SetUpgrade(UpgradeEnums upgrade) {
        if (this.upgrade != null) return false;
        this.upgrade = upgrade;
        SetUpgradeServerRpc();
        return true;
    }

    public UpgradeEnums BuyUpgrade() {
        UpgradeEnums upgrade = this.upgrade;
        this.upgrade = null;
        BuyUpgradeServerRpc();
        return upgrade;
    }

    public bool HasUpgrade() {
        return upgrade != null;
    }

    public UpgradeEnums GetUpgrade() {
        return upgrade;
    }

    public void BuyKey() {
        BuyKeyServerRpc();
    }

    [ServerRpc]
    public void SetUpgradeServerRpc(ServerRpcParams rpcParams = default) {
        SetTriggerClientRpc("SetUpgrade");
    }


    [ServerRpc]
    public void BuyKeyServerRpc(ServerRpcParams rpcParams = default) {
        SetTriggerClientRpc("BuyKey");
    }

    [ServerRpc]
    public void BuyUpgradeServerRpc(ServerRpcParams rpcParams = default) {
        SetTriggerClientRpc("BuyUpgrade");
    }

    [ClientRpc]
    public void SetTriggerClientRpc(string trigger, ClientRpcParams clientRpcParams = default) {
        animator.SetTrigger(trigger);
    }
}
