// Class to store spell properties such as cooldown, damage, etc.

using System.Collections;
using Unity.Netcode;
using UnityEngine;

public enum HandEnum {
    Left,
    Right,
    Both
}

public class Spell : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float cooldown = 1f;
    [SerializeField] private float chargeCost = 10f;

    public HandEnum hand = HandEnum.Right;
    public float fullCharge = 100f;
    public float rechargeRate = 10f;
    private float castSpeedMultiplier = 2f;
    [SerializeField] private float castDelay = 0.1f;

    [SerializeField] private float animationTime = 2.08f;
    [SerializeField] private bool onCooldown = false;
    [SerializeField] private float offHandDelay = 0.1f;

    [SerializeField] private float offHandDamageMultiplier = 1.5f;

    public float destroyAfter = 5f;

    public bool spawnOnFloor = false;
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

    public string HandTrigger {
        get {
            switch (hand) {
                case HandEnum.Left:
                    return "Attack1HL";
                case HandEnum.Right:
                    return "Attack1HR";
                case HandEnum.Both:
                    return "Attack2H";
            }
            return "";
        }
    }

    public void ChangeHand(HandEnum newHand) {
        if (hand == HandEnum.Both) return;
        hand = newHand;
    }

    // TODO Tweak these values
    // Right now damage is obvious choice
    public float Damage {
        get {
            if (hand == HandEnum.Left) return damage * (1 + damageUpgrades * 0.5f) * offHandDamageMultiplier;
            return damage * (1 + damageUpgrades * 0.5f);
        }
    }

    public float CastSpeedMultiplier {
        get {
            return castSpeedMultiplier + cooldownUpgrades * 0.2f;
        }
    }

    public float AnimationTime {
        get {
            return (animationTime+offHandDelay) / CastSpeedMultiplier;
        }
    }

    public float Cooldown {
        get {
            return Mathf.Max(cooldown/CastSpeedMultiplier, AnimationTime);
        }
    }

    public float ChargeCost {
        get {
            return chargeCost * (1 - costUpgrades * 0.2f);
        }
    }

    public float CastDelay {
        get {
            if (hand == HandEnum.Left) return (castDelay + offHandDelay)/CastSpeedMultiplier;
            return castDelay/CastSpeedMultiplier;
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

    public bool Cast() {
        Debug.Log("Casting spell" + name + " with charge " + currentCharge + " and cost " + chargeCost);
        if (onCooldown || ChargeCost > currentCharge) return false;
        onCooldown = true;
        StartCoroutine(CooldownCoroutine());
        currentCharge -= ChargeCost;
        return true;
    }

    IEnumerator CooldownCoroutine() {
        yield return new WaitForSeconds(Cooldown);
        onCooldown = false;
    }

}
