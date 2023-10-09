// Class to store spell properties such as cooldown, damage, etc.

using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Spell : MonoBehaviour
{
    public float damage = 10f;
    public float cooldown = 1f;
    public float chargeCost = 10f;
    public float fullCharge = 100f;
    public float rechargeRate = 10f;
    public float castTime = 0.5f;
    public bool onCooldown = false;
    public GameObject effectPrefab;

    public GameObject handEffectPrefab;
    
    private float currentCharge = 0f;

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
        if (onCooldown || chargeCost > currentCharge) return false;
        onCooldown = true;
        StartCoroutine(CooldownCoroutine());
        currentCharge -= chargeCost;
        return true;
    }

    IEnumerator CooldownCoroutine() {
        yield return new WaitForSeconds(cooldown+castTime);
        onCooldown = false;
    }

}
