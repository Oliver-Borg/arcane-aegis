using UnityEngine;
using Unity.Netcode;

public class PlayerAttack : NetworkBehaviour {
    
    [SerializeField] private GameObject [] spellPrefabs;
    [SerializeField] private Transform handTransform;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform spellSlots;

    private int spellIndex = 0;
    private GameObject [] spells;

    private Animator animator;

    private GameObject currentHandEffect;



    public override void OnNetworkSpawn() {
        animator = GetComponent<Animator>();
        // Instantiate spell GameObjects (script holders) on the client
        spells = new GameObject[spellPrefabs.Length];
        for (int i = 0; i < spellPrefabs.Length; i++) {
            GameObject spell = Instantiate(spellPrefabs[i], spellSlots);
            spells[i] = spell;
        }
        CreateHandEffectServerRpc();
    }

    [ServerRpc]
    private void CreateHandEffectServerRpc () {
        // TODO Fix this (extension)
        // Hand effects are designed as cast effects so looping needs to be done manually
        GameObject handEffect = spells[spellIndex].GetComponent<Spell>().handEffectPrefab;
        if (handEffect == null) return;
        if (currentHandEffect != null) handEffect.GetComponent<NetworkObject>().Despawn(true);
        currentHandEffect = Instantiate(handEffect, handTransform.position, handTransform.rotation, handTransform);
        currentHandEffect.GetComponent<NetworkObject>().Spawn();
    }
    void Update()
    {   
        if (!IsOwner) return;
        // Use scroll wheel to change spell
        int oldSpellIndex = spellIndex;
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




        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            // Spawn effect
            // Create raycast and set rotation to the direction from the player to the raycast hit point
            // Cast forward ray from camera in camera direction

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit)) {

                Vector3 spawnPoint = handTransform.position + transform.forward * 0.1f;
                Vector3 direction = hit.point - spawnPoint;
                Quaternion rotation = Quaternion.LookRotation(direction);
                Spell spell = spells[spellIndex].GetComponent<Spell>();
                Debug.Log("Casting spell " + spell.name);
                bool success = spell.Cast(spawnPoint, rotation);
                Debug.Log("Success: " + success);

                if (!success) return;

                animator.SetTrigger("Attack");
                // For now use hitscan for damage TODO change to projectile
                var enemy = hit.collider.GetComponent<EnemyAI>(); // TODO Change to mesh collider
                if (enemy != null)
                {
                    enemy.TakeDamageServerRpc(50f);
                }
            }
        }
    }


}
