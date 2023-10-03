using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float speed = 5f;

    [SerializeField] private Transform cube;

    [SerializeField] private Camera playerCamera;

    private Vector3 velocity = Vector3.zero;

    private NetworkVariable<PlayerData> playerData = new NetworkVariable<PlayerData>(
        new PlayerData {
            name = "Player",
        }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner
    ); 

    private NetworkVariable<float> health = new NetworkVariable<float>(
        100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner
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

        Vector3 movement = Vector3.zero;

        float speed_mult = 1f;

        if (Input.GetKey(KeyCode.W)) movement += playerCamera.transform.forward;
        if (Input.GetKey(KeyCode.S)) movement -= playerCamera.transform.forward;
        if (Input.GetKey(KeyCode.A)) movement -= playerCamera.transform.right;
        if (Input.GetKey(KeyCode.D)) movement += playerCamera.transform.right;
        if (Input.GetKey(KeyCode.LeftShift)) speed_mult = 2f;
        transform.position += movement * Time.deltaTime * speed * speed_mult;

        // Rotate camera
        if (Input.GetKey(KeyCode.Q)) playerCamera.transform.Rotate(Vector3.up, -Time.deltaTime * 100f);
        if (Input.GetKey(KeyCode.E)) playerCamera.transform.Rotate(Vector3.up, Time.deltaTime * 100f);

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
            var ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            // Spawn cube at ray start

            if (Physics.Raycast(ray, out var hit))
            {
                // Check if we hit a player
                NetworkLog.LogInfoServer("Hit something at position " + hit.collider.transform);
                var player = hit.collider.GetComponent<PlayerController>();
                if (player != null)
                {
                    var targetID = player.OwnerClientId; 
                    NetworkLog.LogInfoServer($"Player {OwnerClientId} hit player {targetID}");
                    // Do damage
                    NetworkLog.LogInfoServer("Calling ServerRpc to damage player " + targetID);
                    DoDamageServerRpc(clientID: targetID);
                }
                var enemy = hit.collider.GetComponent<EnemyAI>(); // TODO Change to mesh collider
                if (enemy != null)
                {
                    NetworkLog.LogInfoServer($"Player {OwnerClientId} hit enemy");
                    // Do damage
                    NetworkLog.LogInfoServer("Calling ServerRpc to damage enemy");
                    enemy.TakeDamageClientRpc(30f);
                }
            }
            SpawnProjectileServerRpc(ray.origin, ray.direction);
        }

        // OOB damage
        if (transform.position.y < -10f) DoDamageServerRpc(clientID: OwnerClientId);
    }

    // TODO Fix damaging players

    [ServerRpc] // Runs on the server (sent by client)
    public void DoDamageServerRpc(ulong clientID, float damage=50f, ServerRpcParams rpcParams = default) {
        NetworkLog.LogInfoServer("Received ServerRpc on " + OwnerClientId + " with network ID " + NetworkObjectId + " to damage player " + clientID);
        NetworkObject client = NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject;
        PlayerController controller = client.GetComponent<PlayerController>();
        controller.DoDamageClientRpc(damage, new ClientRpcParams { Send = {TargetClientIds = new ulong[] { clientID } } });
    }

    [ServerRpc]
    private void SpawnProjectileServerRpc(Vector3 position, Vector3 direction, ServerRpcParams rpcParams = default) {
        var projectile = Instantiate(cube, position + direction, Quaternion.identity);
        projectile.GetComponent<NetworkObject>().Spawn();
        projectile.GetComponent<Rigidbody>().AddForce(direction * 10000f);
    }

    [ClientRpc] // Runs on all clients (sent from server). Can use ClientRpcParams to give a list of clients to run on
    public void DoDamageClientRpc(float damage = 50f, ClientRpcParams rpcParams = default) {
        if(!IsOwner) 
        {
            NetworkLog.LogInfoServer("Not owner");
            return;
        }
        NetworkLog.LogInfoServer("Received ClientRpc on " + OwnerClientId + " with network ID " + NetworkObjectId + " to damage player " + OwnerClientId);
        health.Value -= damage;
        // Die
        if (health.Value <= 0f)
        {
            health.Value = 100f;
            transform.position = new Vector3(0f, 0f, 0f);
        }

    }      
    
}
