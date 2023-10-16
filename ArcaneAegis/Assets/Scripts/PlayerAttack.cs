using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System;
public class PlayerAttack : NetworkBehaviour {
    
    [SerializeField] private GameObject [] spellPrefabs;
    [SerializeField] private Transform rightHandTransform;
    [SerializeField] private Transform leftHandTransform;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform spellSlots;

    private Tuple<UpgradeEnum, ElementEnum> [] upgrades = new Tuple<UpgradeEnum, ElementEnum>[5];

    
    private GameObject [] spells;

    private Animator animator;

    private GameObject currentHandEffect;

    private bool casting = false;

    private int rightIndex = 0, leftIndex = 1;
    private Spell rightSpell, leftSpell;


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
        rightSpell = spells[0].GetComponent<Spell>();
        leftSpell = spells[1].GetComponent<Spell>();
    }

    [ServerRpc]
    private void CreateHandEffectServerRpc () {
        return;
        // TODO Implement this (extension)
        // Hand effects are designed as cast effects so looping needs to be done manually
    }

    IEnumerator CastCoroutine(HandEnum hand) {
        Spell spell = hand == HandEnum.Right ? rightSpell : leftSpell;
        casting = true;
        animator.SetTrigger(spell.HandTrigger);
        // If Both then just spawn effect on left hand
        Transform handTransform = hand == HandEnum.Right ? rightHandTransform : leftHandTransform;
        int index = hand == HandEnum.Right ? rightIndex : leftIndex;
        yield return new WaitForSeconds(spell.CastDelay);
        Quaternion rotation = playerCamera.transform.rotation;
        Ray ray = new Ray(handTransform.position, playerCamera.transform.forward);
        // Draw ray for debugging
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.green, 2f);
        if (Physics.Raycast(ray, out RaycastHit hit)) {
            Vector3 spawnPoint = handTransform.position + transform.forward * 0.1f;
            Vector3 direction = hit.point - spawnPoint;
            rotation = Quaternion.LookRotation(direction);
        }
        SpawnEffectServerRpc(index, rotation);
        yield return new WaitForSeconds(spell.CastTime);
        casting = false;
    }
    [ServerRpc]
    public void SpawnEffectServerRpc(int index, Quaternion rotation, ServerRpcParams rpcParams = default) {
        SpawnEffectClientRpc(index, rotation);
    }

    [ClientRpc]
    public void SpawnEffectClientRpc(int index, Quaternion rotation, ClientRpcParams rpcParams = default) {
        // We just spawn the effects on the clients do prevent complexity
        NetworkLog.LogInfoServer("Spawning effect");
        
        Spell spell = spells[index].GetComponent<Spell>();
        Transform handTransform = spell.hand == HandEnum.Right ? rightHandTransform : leftHandTransform;
        if (spell.handEffectPrefab != null)
            Instantiate(spell.handEffectPrefab, handTransform);
        GameObject effect = Instantiate(spell.effectPrefab, handTransform.position, rotation);
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
        // Delete effect
        Destroy(effect);
        
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
        int spellIndex = rightIndex;
        // Check if CastCoroutine in progress
        if (!casting) {
            if (Input.GetAxis("Mouse ScrollWheel") > 0f) {
                spellIndex++;
            if (spellIndex >= spells.Length) spellIndex = 0;
            } else if (Input.GetAxis("Mouse ScrollWheel") < 0f) {
                spellIndex--;
                if (spellIndex < 0) spellIndex = spells.Length-1;
            }
            if (rightIndex != spellIndex) {
                CreateHandEffectServerRpc();
            }
            rightIndex = spellIndex;
            leftIndex = (spellIndex+1) % spells.Length;
            rightSpell = spells[rightIndex].GetComponent<Spell>();
            leftSpell = spells[leftIndex].GetComponent<Spell>();
            rightSpell.ChangeHand(HandEnum.Right);
            leftSpell.ChangeHand(HandEnum.Left);
        }   

        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            // Spawn effect
            // Create raycast and set rotation to the direction from the player to the raycast hit point
            // Cast forward ray from camera in camera direction

            BasicBehaviour basicBehaviour = GetComponent<BasicBehaviour>();
            if (basicBehaviour.IsSprinting() || casting) return;
            bool success = rightSpell.Cast();
            Debug.Log("Success: " + success);

            if (!success) return;
            StartCoroutine(CastCoroutine(HandEnum.Right));
            // TODO Increase animation speed based on upgrades
            }
        
        else if (Input.GetKeyDown(KeyCode.Mouse1)) {
            // Spawn effect
            // Create raycast and set rotation to the direction from the player to the raycast hit point
            // Cast forward ray from camera in camera direction
            BasicBehaviour basicBehaviour = GetComponent<BasicBehaviour>();
            if (basicBehaviour.IsSprinting() || casting) return;
            bool success = leftSpell.Cast();
            Debug.Log("Success: " + success);
            if (!success) return;
            StartCoroutine(CastCoroutine(HandEnum.Left));
            // TODO Increase animation speed based on upgrades
        }
    }


}
