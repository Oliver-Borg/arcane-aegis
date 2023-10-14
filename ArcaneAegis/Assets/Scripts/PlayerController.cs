using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Collections;

public class PlayerController : NetworkBehaviour
{
    // [SerializeField] private float speed = 5f;

    [SerializeField] private Transform cube;

    [SerializeField] private Camera playerCamera;

    [SerializeField] private float regenRate = 10f;

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
        if (IsServer)
        {
            RegenHealth();
        }

        if (!IsOwner) return;
        // OOB damage
        if (transform.position.y < -10f) TakeDamageServerRpc(100f);
    }

    void RegenHealth() {
        if (health.Value >= 100f) return;
        float regenAmnt = regenRate * Time.deltaTime;
        health.Value += regenAmnt;
        if (health.Value > 100f) health.Value = 100f;
    }


    [ServerRpc(Delivery = default, RequireOwnership = false)] // Runs on the server (sent by client)
    public void TakeDamageServerRpc(float damage=50f, ServerRpcParams rpcParams = default) {
        health.Value -= damage;
        // Die
        if (health.Value <= 0f)
        {
            RespawnClientRpc();
            health.Value = 100f;
        }
    }

    [ClientRpc]
    public void RespawnClientRpc() {
        // TODO Fix this: Only owner can set their own position due to ClientNetworkTransform
        if (!IsOwner) return;
        transform.position = new Vector3(0f, 0f, 0f);
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

    // Show GUI with health
    void OnGUI() {
        if (!IsOwner) return;
        GUI.Label(new Rect(10, 10, 100, 20), $"Health: {health.Value}");
    }
    
}
