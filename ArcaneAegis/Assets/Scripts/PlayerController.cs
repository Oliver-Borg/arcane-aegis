using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float speed = 5f;

    [SerializeField] private Transform cube;

    private Vector3 velocity = Vector3.zero;

    private NetworkVariable<PlayerData> playerData = new NetworkVariable<PlayerData>(
        new PlayerData {
            health = 100f,
            name = "Player",
        }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner
    ); 

    public struct PlayerData : INetworkSerializable {
        public float health;
        public FixedString32Bytes name;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref health);
            serializer.SerializeValue(ref name);
        }
    }

    public override void OnNetworkSpawn() {
        playerData.OnValueChanged += (PlayerData previousValue, PlayerData newValue) => {
            Debug.Log($"Player {previousValue.name} health changed from {previousValue.health} to {newValue.health}");
        };
    }

    void Update()
    {
        // Get input from key presses and move accordingly
        if (!IsOwner) return;

        Vector3 movement = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) movement += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) movement += Vector3.back;
        if (Input.GetKey(KeyCode.A)) movement += Vector3.left;
        if (Input.GetKey(KeyCode.D)) movement += Vector3.right;
        transform.position += movement * Time.deltaTime * speed;

        // Jump
        if (transform.position.y > 0f) velocity += Vector3.down * Time.deltaTime * 10f;
        else velocity = Vector3.zero;

        // Fall
        if (Input.GetKeyDown(KeyCode.Space) && transform.position.y < 0.1f) velocity += Vector3.up * 10f;

        // Apply velocity
        transform.position += velocity * Time.deltaTime;


        // Get click
        if (Input.GetMouseButtonDown(0))
        {
            // Get ray
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            // Spawn cube at ray start
            SpawnProjectileServerRpc(ray.origin, ray.direction);

            if (Physics.Raycast(ray, out var hit))
            {
                // Check if we hit a player
                var player = hit.collider.GetComponent<PlayerController>();
                if (player != null)
                {
                    Debug.Log($"Hit player {player.playerData.Value.name}");
                    // Do damage
                    DoDamageServerRpc(playerIndex: player.NetworkObjectId);
                }
            }
        }

        var localPlayerData = playerData.Value;
        // OOB damage
        if (transform.position.y < -10f) localPlayerData.health -= 10f;

        // Die
        if (localPlayerData.health <= 0f)
        {
            localPlayerData.health = 100f;
            transform.position = new Vector3(0f, 0f, 0f);
        }
        playerData.Value = localPlayerData;
    }

    // TODO Fix damaging players

    [ServerRpc] // Runs on the server (sent by client)
    private void DoDamageServerRpc(ulong playerIndex = 0, ServerRpcParams rpcParams = default) {
        Debug.Log($"Trying to damage player {playerIndex}");
        DoDamageClientRpc(new ClientRpcParams { Send = {TargetClientIds = new ulong[] { playerIndex } } });
    }

    [ServerRpc]
    private void SpawnProjectileServerRpc(Vector3 position, Vector3 direction, ServerRpcParams rpcParams = default) {
        var projectile = Instantiate(cube, position + direction, Quaternion.identity);
        projectile.GetComponent<NetworkObject>().Spawn();
        projectile.GetComponent<Rigidbody>().AddForce(direction * 10000f);
        Debug.Log($"Spawned projectile at {position} with direction {direction}");
    }

    [ClientRpc] // Runs on all clients (sent from server). Can use ClientRpcParams to give a list of clients to run on
    private void DoDamageClientRpc(ClientRpcParams rpcParams = default) {
        var localPlayerData = playerData.Value;
        localPlayerData.health -= 10f;
        playerData.Value = localPlayerData;
    }
        
    
}
