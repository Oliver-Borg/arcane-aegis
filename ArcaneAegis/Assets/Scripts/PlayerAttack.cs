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
    [SerializeField] private Transform crossHair;
    [SerializeField] private GameObject hitmarkerPrefab;

    private UpgradeEnums [] upgrades = new UpgradeEnums[6];

    private int upgradeCount = 0;

    
    private GameObject [] spells;

    private Animator animator;

    private GameObject currentHandEffect;

    private bool casting = false;

    public int rightIndex = 0, leftIndex = 2;
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
        rightSpell = spells[rightIndex].GetComponent<Spell>();
        leftSpell = spells[leftIndex].GetComponent<Spell>();
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
        animator.SetFloat("castMultiplier", spell.CastSpeedMultiplier);
        animator.SetTrigger(spell.HandTrigger);
        // If Both then just spawn effect on left hand
        yield return new WaitForSeconds(spell.CastDelay);
        Transform handTransform = hand == HandEnum.Right ? rightHandTransform : leftHandTransform;
        int index = hand == HandEnum.Right ? rightIndex : leftIndex;
        Quaternion rotation = playerCamera.transform.rotation;
        Ray ray = new Ray(handTransform.position, playerCamera.transform.forward);
        // Draw ray for debugging
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.green, 2f);
        Vector3 spawnPoint = handTransform.position + transform.forward * 0.1f;
        if (Physics.Raycast(ray, out RaycastHit hit)) {
            Vector3 direction = hit.point - spawnPoint;
            rotation = Quaternion.LookRotation(direction);
        }
        if (spell.spawnOnFloor) {
            rotation = Quaternion.Euler(0, 0, 0);
            // Change spawn location of spell to floor below player
            Ray floorRay = new Ray(transform.position, Vector3.down);
            if (Physics.Raycast(floorRay, out RaycastHit floorHit)) {
                spawnPoint = floorHit.point;
            }
        }
        if (spell.hand == HandEnum.Both)
            rotation = Quaternion.Euler(0, 0, 0);

        SpawnEffectServerRpc(index, rotation, spawnPoint);

        PlayerInventory inventory = GetComponent<PlayerInventory>();
        if (inventory.catalystUpgrade == spell.element) {
            // TODO Implement catalyst upgrades
            // Fire: ring of fire
            // Ice: proper freeze
            // Lightning: mobile lightning orb
            // Gravity: black hole meteor

            // For now just spawn a second effect after CastDelay
            yield return new WaitForSeconds(spell.CastDelay);
            SpawnEffectServerRpc(index, rotation, spawnPoint);
            yield return new WaitForSeconds(spell.CastDelay);
            SpawnEffectServerRpc(index, rotation, spawnPoint);
            yield return new WaitForSeconds(Mathf.Max(spell.AnimationTime-3*spell.CastDelay, 0f));

        }
        else
        {
            yield return new WaitForSeconds(spell.AnimationTime-spell.CastDelay);
        }

        
        casting = false;
    }
    [ServerRpc]
    public void SpawnEffectServerRpc(int index, Quaternion rotation, Vector3 position, ServerRpcParams rpcParams = default) {
        SpawnEffectClientRpc(index, rotation, position);
    }

    [ClientRpc]
    public void SpawnEffectClientRpc(int index, Quaternion rotation, Vector3 position, ClientRpcParams rpcParams = default) {
        // We just spawn the effects on the clients do prevent complexity
        NetworkLog.LogInfoServer("Spawning effect");
        
        Spell spell = spells[index].GetComponent<Spell>();
        Transform handTransform = spell.hand == HandEnum.Right ? rightHandTransform : leftHandTransform;
        if (spell.handEffectPrefab != null)
            Instantiate(spell.handEffectPrefab, handTransform);
        GameObject effect = Instantiate(spell.effectPrefab, position, rotation);
        RFX4_PhysicsMotion physicsMotion = effect.GetComponentInChildren<RFX4_PhysicsMotion>(true);
        RFX4_RaycastCollision physicsRaycast = effect.GetComponentInChildren<RFX4_RaycastCollision>(true);
        if (physicsRaycast != null) {
            physicsRaycast.CollisionEnter += CollisionEnter;
            physicsRaycast.Damage = spell.Damage;
            physicsRaycast.Element = spell.element;
        }
        
        if (physicsMotion != null) {
            physicsMotion.CollisionEnter += CollisionEnter;
            physicsMotion.Damage = spell.Damage;
            physicsMotion.Element = spell.element;
        }
        if (spell.destroyAfter >= 0f) {
            Destroy(effect, spell.destroyAfter);
        }
    }

    private void CollisionEnter(object sender, RFX4_PhysicsMotion.RFX4_CollisionInfo e)
    {
        if (!IsServer) return; // Do hit detection on the server

        // Debug.Log(e.HitPoint); //a collision coordinates in world space
        // Debug.Log(e.HitGameObject.name); //a collided gameobject
        Debug.Log(e.HitCollider.name); //a collided collider :)

        GameObject effect;
        float damage;
        int elementIndex;
        try
        {
            RFX4_PhysicsMotion physicsMotion = (RFX4_PhysicsMotion) sender;
            effect = physicsMotion.gameObject.transform.parent.gameObject;
            damage = physicsMotion.Damage;
            elementIndex = (int) physicsMotion.Element;
        }
        catch (System.InvalidCastException)
        {
            RFX4_RaycastCollision physicsRaycast = (RFX4_RaycastCollision) sender;
            effect = physicsRaycast.gameObject.transform.parent.gameObject;
            damage = physicsRaycast.Damage*Time.deltaTime; 
            elementIndex = (int) physicsRaycast.Element;
            // Use delta time for raycast spells to simulate damage per second
        }



        
        EnemyAI enemy = e.HitCollider.GetComponent<EnemyAI>(); // TODO Change to mesh collider
        if (enemy != null)
        {
            Debug.Log("Hit enemy for " + damage + " damage");
            // Send player index to enemy so that we can track damage
            enemy.TakeDamageServerRpc(damage, elementIndex, OwnerClientId);
        }
        // Delete effect
        // Destroy(effect);
        
    }

    [ClientRpc]
    public void CreateHitmarkerClientRpc(float damage, bool killed, ClientRpcParams rpcParams = default) {
        if (!IsOwner) return;
        Color color = killed ? Color.red : Color.white;
        GameObject hitmarker = Instantiate(hitmarkerPrefab, crossHair);
        hitmarker.GetComponent<Hitmarker>().Inititialise(color, Mathf.Clamp(damage/100f, 0.1f, 0.5f)+0.5f);
    }

    public void AddUpgrade(UpgradeEnums upgrade, int index) {
        RemoveUpgrade(upgrades[index]);
        upgrades[index] = upgrade;
        Spell current = GetSpellOfElement(upgrade.element);
        current.AddUpgrade(upgrade.upgradeType);
        upgradeCount++;
    }

    public void RemoveUpgrade(UpgradeEnums upgrade) {
        if (upgrade == null) return;
        Spell current = GetSpellOfElement(upgrade.element);
        current.RemoveUpgrade(upgrade.upgradeType);
        upgradeCount--;
    }

    public UpgradeEnums [] GetUpgrades() {
        return upgrades;
    }

    public int UpgradeCount() {
        return upgradeCount;
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

    public float GetChargeRatio(int index) {
        return spells[index].GetComponent<Spell>().ChargeRatio();
    }


    void Update()
    {   
        if (!IsOwner) return;
        // Check if CastCoroutine in progress
        if (!casting) {
            // Use E to change rightSpell and Q to change leftSpell
            if (Input.GetKeyDown(KeyCode.E)) {
                rightIndex = rightIndex == 0 ? 1 : 0;
                rightSpell = spells[rightIndex].GetComponent<Spell>();
                // rightSpell.ChangeHand(HandEnum.Right);
            }
            else if (Input.GetKeyDown(KeyCode.Q)) {
                leftIndex = leftIndex == 2 ? 3 : 2;
                leftSpell = spells[leftIndex].GetComponent<Spell>();
                // leftSpell.ChangeHand(HandEnum.Left);
            }
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
