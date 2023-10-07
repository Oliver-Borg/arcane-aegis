using UnityEngine;
using Unity.Netcode;

public class PlayerAttack : NetworkBehaviour {
    
    [SerializeField] private GameObject [] effects;
    [SerializeField] private Transform [] effectTransforms;

    [SerializeField] private Camera playerCamera;

    private int effectIndex = 0;

    private Animator animator;



    void Start() {
        animator = GetComponent<Animator>();
    }

    void Update()
    {   
        if (!IsOwner) return;
        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            animator.SetTrigger("Attack");
            // Spawn effect
            // Create raycast and set rotation to the direction from the player to the raycast hit point
            // Cast forward ray from camera in camera direction

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit)) {
                Vector3 spawnPoint = effectTransforms[effectIndex].position + transform.forward * 0.1f;
                Vector3 direction = hit.point - spawnPoint;
                Quaternion rotation = Quaternion.LookRotation(direction);
                SpawnEffectServerRpc(effectIndex, spawnPoint, rotation);
            }
        }
    }

    [ServerRpc]
    public void SpawnEffectServerRpc(int effectIndex, Vector3 position, Quaternion rotation, ServerRpcParams rpcParams = default) {
        GameObject effect = Instantiate(effects[effectIndex], position, rotation);
        effect.GetComponent<NetworkObject>().Spawn();
    }
}
