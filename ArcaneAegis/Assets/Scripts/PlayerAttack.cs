using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System;
public class PlayerAttack : NetworkBehaviour {
    
    [SerializeField] private GameObject [] spellPrefabs;
    [SerializeField] private Transform handTransform;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform spellSlots;

    private Tuple<UpgradeEnum, ElementEnum> [] upgrades = new Tuple<UpgradeEnum, ElementEnum>[5];

    private int spellIndex = 0;
    private GameObject [] spells;

    private Animator animator;

    private GameObject currentHandEffect;

    private bool casting = false;


    public override void OnNetworkSpawn() {
        animator = GetComponent<Animator>();
        // Instantiate spell GameObjects (script holders) on the client
        spells = new GameObject[spellPrefabs.Length];
        for (int i = 0; i < spellPrefabs.Length; i++) {
            GameObject spell = Instantiate(spellPrefabs[i], spellSlots);
            spells[i] = spell;
        }
        if (IsOwner)
            CreateHandEffectServerRpc();
    }

    [ServerRpc]
    private void CreateHandEffectServerRpc () {
        return;
        // TODO Implement this (extension)
        // Hand effects are designed as cast effects so looping needs to be done manually
    }

    IEnumerator CastCoroutine(int index, Quaternion rotation) {
        Spell spell = spells[index].GetComponent<Spell>();
        casting = true;
        yield return new WaitForSeconds(spell.castTime);
        // Do a fresh raycast to get the up to date rotation
        Ray ray = new Ray(handTransform.position, playerCamera.transform.forward);
        // Draw ray for debugging
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.green, 2f);
        if (Physics.Raycast(ray, out RaycastHit hit)) {
            Vector3 spawnPoint = handTransform.position + transform.forward * 0.1f;
            Vector3 direction = hit.point - spawnPoint;
            rotation = Quaternion.LookRotation(direction);
        }
        SpawnEffectServerRpc(index, rotation);
        if (spell.handEffectPrefab != null)
            Instantiate(spell.handEffectPrefab, handTransform);
        casting = false;
    }

    [ServerRpc]
    public void SpawnEffectServerRpc(int index, Quaternion rotation, ServerRpcParams rpcParams = default) {
        NetworkLog.LogInfoServer("Spawning effect");
        Spell spell = spells[index].GetComponent<Spell>();
        GameObject effect = Instantiate(spell.effectPrefab, handTransform.position, rotation);
        effect.GetComponent<NetworkObject>().Spawn();
        RFX4_PhysicsMotion physicsMotion = effect.GetComponentInChildren<RFX4_PhysicsMotion>(true);
        RFX4_RaycastCollision physicsRaycast = effect.GetComponentInChildren<RFX4_RaycastCollision>(true);
        if (physicsRaycast != null) {
            physicsRaycast.CollisionEnter += CollisionEnter;
            physicsRaycast.Damage = spell.Damage;
        }
        
        if (physicsMotion != null) {
            physicsMotion.CollisionEnter += CollisionEnter;
            physicsMotion.Damage = spell.Damage;
        }
    }

    private void CollisionEnter(object sender, RFX4_PhysicsMotion.RFX4_CollisionInfo e)
    {
        if (!IsServer) return; // Do hit detection on the server
        // Debug.Log(e.HitPoint); //a collision coordinates in world space
        // Debug.Log(e.HitGameObject.name); //a collided gameobject
        // Debug.Log(e.HitCollider.name); //a collided collider :)

        GameObject effect;
        float damage;
        try
        {
            RFX4_PhysicsMotion physicsMotion = (RFX4_PhysicsMotion) sender;
            effect = physicsMotion.gameObject.transform.parent.gameObject;
            damage = physicsMotion.Damage;
        }
        catch (System.InvalidCastException)
        {
            RFX4_RaycastCollision physicsRaycast = (RFX4_RaycastCollision) sender;
            effect = physicsRaycast.gameObject.transform.parent.gameObject;
            damage = physicsRaycast.Damage;
        }



        
        EnemyAI enemy = e.HitCollider.GetComponent<EnemyAI>(); // TODO Change to mesh collider
        if (enemy != null)
        {
            enemy.TakeDamageServerRpc(damage);
        }
        // Delete effect and despawn
        effect.GetComponent<NetworkObject>().Despawn(true);
    }

    public void AddUpgrade(Upgrade upgrade) {
        RemoveUpgrade(upgrades[0]);
        for (int i = 0; i < upgrades.Length-1; i++) {
            upgrades[i] = null;
            upgrades[i] = upgrades[i+1];
        }
        upgrades[upgrades.Length-1] = new Tuple<UpgradeEnum, ElementEnum>(upgrade.upgradeType, upgrade.upGradeElement);
        Spell current = GetSpellOfElement(upgrade.upGradeElement);
        current.AddUpgrade(upgrade.upgradeType);
    }

    public void RemoveUpgrade(Tuple<UpgradeEnum, ElementEnum> upgrade) {
        if (upgrade == null) return;
        Spell current = GetSpellOfElement(upgrade.Item2);
        current.RemoveUpgrade(upgrade.Item1);
    }
    

    private Spell GetSpellOfElement(ElementEnum element) {
        for (int i = 0; i < spells.Length; i++) {
            Spell current = spells[i].GetComponent<Spell>();
            if (current.element == element) {
                return current;
            }
        }
        return null;
    }


    void Update()
    {   
        if (!IsOwner) return;
        // Use scroll wheel to change spell
        int oldSpellIndex = spellIndex;
        // Check if CastCoroutine in progress
        if (!casting) {
            if (Input.GetAxis("Mouse ScrollWheel") > 0f) {
                spellIndex++;
            if (spellIndex >= spells.Length) spellIndex = 0;
            } else if (Input.GetAxis("Mouse ScrollWheel") < 0f) {
                spellIndex--;
                if (spellIndex < 0) spellIndex = spells.Length-1;
            }
            if (oldSpellIndex != spellIndex) {
                CreateHandEffectServerRpc();
            }
        }   

        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            // Spawn effect
            // Create raycast and set rotation to the direction from the player to the raycast hit point
            // Cast forward ray from camera in camera direction

            BasicBehaviour basicBehaviour = GetComponent<BasicBehaviour>();
            if (basicBehaviour.IsSprinting()) return;

            Ray ray = new Ray(handTransform.position, playerCamera.transform.forward);
            // Draw ray for debugging
            Debug.DrawRay(ray.origin, ray.direction * 100, Color.yellow, 2f);
            if (Physics.Raycast(ray, out RaycastHit hit)) {

                Vector3 spawnPoint = handTransform.position + transform.forward * 0.1f;
                Vector3 direction = hit.point - spawnPoint;
                Quaternion rotation = Quaternion.LookRotation(direction);
                Spell spell = spells[spellIndex].GetComponent<Spell>();
                Debug.Log("Casting spell " + spell.name);
                bool success = spell.Cast(handTransform, rotation) && !casting;
                Debug.Log("Success: " + success);

                if (!success) return;
                StartCoroutine(CastCoroutine(spellIndex, rotation));
                
                // TODO Increase animation speed based on upgrades
                animator.SetTrigger("Attack");
                // For now use hitscan for damage TODO change to projectile
                
            }
        }
    }


}
