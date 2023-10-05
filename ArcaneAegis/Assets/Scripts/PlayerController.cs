using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Collections;

public class PlayerController : NetworkBehaviour
{
    // [SerializeField] private float speed = 5f;

    [SerializeField] private Transform cube;

    [SerializeField] private Camera playerCamera;

    // private Vector3 velocity = Vector3.zero;

    private NetworkVariable<PlayerData> playerData = new NetworkVariable<PlayerData>(
        new PlayerData {
            name = "Player",
        }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner
    ); 

    private NetworkVariable<float> health = new NetworkVariable<float>(
        100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public struct PlayerData : INetworkSerializable {
        public FixedString32Bytes name;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref name);
        }
    }

    public override void OnNetworkSpawn() {
        playerCamera.enabled = IsOwner;
        playerData.OnValueChanged += (PlayerData previousValue, PlayerData newValue) => {
            Debug.Log($"Player {previousValue.name} changed name to {newValue.name}");
        };
    }

    void Update()
    {
        // Get input from key presses and move accordingly
        if (!IsOwner) return;
        // NetworkLog.LogInfoServer("Player spawned with network ID " + NetworkObjectId + " and client ID " + OwnerClientId);


        // Get click
        if (Input.GetMouseButtonDown(0))
        {
            // Get ray
            var ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            // Spawn cube at ray start

            if (Physics.Raycast(ray, out var hit))
            {
                var enemy = hit.collider.GetComponent<EnemyAI>(); // TODO Change to mesh collider
                if (enemy != null)
                {
                    enemy.TakeDamageServerRpc(50f);
                }
            }
            SpawnProjectileServerRpc(ray.origin, ray.direction);
        }

        // OOB damage
        if (transform.position.y < -10f) DoDamageServerRpc(100f);
    }

    // TODO Fix damaging players

    [ServerRpc(Delivery = default, RequireOwnership = false)] // Runs on the server (sent by client)
    public void DoDamageServerRpc(float damage=50f, ServerRpcParams rpcParams = default) {
        health.Value -= damage;
        // Die
        if (health.Value <= 0f)
        {
            health.Value = 100f;
            transform.position = new Vector3(0f, 0f, 0f);
        }
    }

    [ServerRpc]
    private void SpawnProjectileServerRpc(Vector3 position, Vector3 direction, ServerRpcParams rpcParams = default) {
        var projectile = Instantiate(cube, position + direction, Quaternion.identity);
        projectile.GetComponent<NetworkObject>().Spawn();
        projectile.GetComponent<Rigidbody>().AddForce(direction * 10000f);
        StartCoroutine(DespawnProjectileCoroutine(projectile));
    }

    IEnumerator DespawnProjectileCoroutine(Transform projectile) {
        yield return new WaitForSeconds(1f);
        projectile.GetComponent<NetworkObject>().Despawn();
    }    
    
}
