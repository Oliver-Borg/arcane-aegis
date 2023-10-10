// Class to store spell properties such as cooldown, damage, etc.

using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Spell : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float cooldown = 1f;
    [SerializeField] private float chargeCost = 10f;
    public float fullCharge = 100f;
    public float rechargeRate = 10f;
    public float castTime = 0.5f;
    public bool onCooldown = false;
    public GameObject effectPrefab;

    public GameObject handEffectPrefab;

    public ElementEnum element;
    
    private float currentCharge = 0f;

    private int damageUpgrades = 0;
    private int cooldownUpgrades = 0;
    private int costUpgrades = 0;

    public void RemoveUpgrade(UpgradeEnum type) {
        switch (type) {
            case UpgradeEnum.Damage:
                damageUpgrades--;
                break;
            case UpgradeEnum.Cooldown:
                cooldownUpgrades--;
                break;
            case UpgradeEnum.Cost:
                costUpgrades--;
                break;
        }
        damageUpgrades = Mathf.Max(damageUpgrades, 0);
        cooldownUpgrades = Mathf.Max(cooldownUpgrades, 0);
        costUpgrades = Mathf.Max(costUpgrades, 0);
    }

    public void AddUpgrade(UpgradeEnum type) {
        switch (type) {
            case UpgradeEnum.Damage:
                damageUpgrades++;
                break;
            case UpgradeEnum.Cooldown:
                cooldownUpgrades++;
                break;
            case UpgradeEnum.Cost:
                costUpgrades++;
                break;
        }
    }

    // TODO Tweak these values
    // Right now damage is obvious choice
    public float Damage {
        get {
            return damage * (1 + damageUpgrades * 0.5f);
        }
    }

    public float Cooldown {
        get {
            return cooldown * (1 - cooldownUpgrades * 0.2f);
        }
    }

    public float ChargeCost {
        get {
            return chargeCost * (1 - costUpgrades * 0.2f);
        }
    }

    public void Start() {
        currentCharge = fullCharge;
    }    

    public void Update() {
        if (currentCharge < fullCharge) {
            currentCharge += rechargeRate * Time.deltaTime;
            if (currentCharge > fullCharge) currentCharge = fullCharge;
        }
    }

    public bool Cast(Transform handTransform, Quaternion rotation) {
        Debug.Log("Casting spell" + name + " with charge " + currentCharge + " and cost " + chargeCost);
        if (onCooldown || ChargeCost > currentCharge) return false;
        onCooldown = true;
        StartCoroutine(CooldownCoroutine());
        currentCharge -= ChargeCost;
        return true;
    }

    IEnumerator CooldownCoroutine() {
        yield return new WaitForSeconds(Cooldown+castTime);
        onCooldown = false;
    }

}
