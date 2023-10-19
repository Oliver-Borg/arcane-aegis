using Unity.Netcode;
using UnityEngine;

public class Alchemist : NetworkBehaviour
{
    private UpgradeEnums upgrade; // Upgrade will be local


    public bool SetUpgrade(UpgradeEnums upgrade) {
        if (this.upgrade != null) return false;
        this.upgrade = upgrade;
        return true;
    }

    public UpgradeEnums BuyUpgrade() {
        UpgradeEnums upgrade = this.upgrade;
        this.upgrade = null;
        return upgrade;
    }

    public bool HasUpgrade() {
        return upgrade != null;
    }

    public UpgradeEnums GetUpgrade() {
        return upgrade;
    }
}
