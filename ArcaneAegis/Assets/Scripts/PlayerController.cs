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

    private NetworkVariable<bool> isDead = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    [SerializeField] private MonoBehaviour [] disableOnDeath; 

    [SerializeField] private Collider interactCollider;

    [SerializeField] private AnimationCurve cameraShakeCurve;

    [SerializeField] private float cameraShakeDuration = 0.1f;

    [SerializeField] private float cameraShakeBaseAmplitude = 0.1f;

    [SerializeField] private float selfDamage = 100f;

    private Animator animator;

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
        animator = GetComponent<Animator>();
        animator.SetBool("isDead", isDead.Value);
    }

    public bool IsDead() {
        return isDead.Value;
    }

    public float GetHealthRatio() {
        return health.Value / 100f;
    }

    void Update()
    {
        if (IsServer)
        {
            RegenHealth();
        }

        if (!IsOwner) return;
        // OOB respawn
        if (transform.position.y < -100f) transform.position = new Vector3(0f, 0f, 0f);
        if (Input.GetKeyDown(KeyCode.J)) TakeDamageServerRpc(selfDamage);
    }

    void RegenHealth() {
        if (health.Value >= 100f || IsDead()) return;
        float regenAmnt = regenRate * Time.deltaTime;
        health.Value += regenAmnt;
        if (health.Value > 100f) health.Value = 100f;
    }


    [ServerRpc(Delivery = default, RequireOwnership = false)] // Runs on the server (sent by client)
    public void TakeDamageServerRpc(float damage=50f, ServerRpcParams rpcParams = default) {
        health.Value -= damage;
        TakeDamageClientRpc(damage);
        // Die
        if (health.Value <= 0f)
        {
            // RespawnClientRpc();
            health.Value = 0f;
            isDead.Value = true;
            DieClientRpc();
        }
    }

    [ClientRpc]
    public void TakeDamageClientRpc(float damage) {
        // Do camera shake here
        if (!IsOwner) return;
        StartCoroutine(CameraShakeCoroutine(damage));
    }   

    public IEnumerator CameraShakeCoroutine(float damage) {
        float startTime = Time.time;
        float damageRatio = Mathf.Clamp(damage/100f, 0.5f, 1.0f);
        float cameraShakeDuration = this.cameraShakeDuration*damageRatio;
        while (Time.time - startTime < cameraShakeDuration) {
            float time = Time.time - startTime;
            float amplitude = damageRatio*cameraShakeBaseAmplitude * cameraShakeCurve.Evaluate(time / cameraShakeDuration);
            Vector3 offset = new Vector3(Random.Range(-amplitude, amplitude), Random.Range(-amplitude, amplitude), 0f);
            playerCamera.transform.position += offset;
            yield return null;
        }
    }

    [ClientRpc]
    public void DieClientRpc() {
        // Change layer of parent transform to Interaction
        interactCollider.gameObject.layer = LayerMask.NameToLayer("Interaction");
        transform.GetComponent<Collider>().excludeLayers = LayerMask.NameToLayer("Player");
        transform.tag = "DeadPlayer";
        transform.gameObject.layer = LayerMask.NameToLayer("DeadPlayer");
        // Disable components
        foreach (MonoBehaviour obj in disableOnDeath) {
            obj.enabled = false;
        }
        // transform.GetComponent<Collider>().enabled = false;
        animator.SetBool("isDead", true);
    }

    [ServerRpc(Delivery = default, RequireOwnership = false)]
    public void ReviveServerRpc(ServerRpcParams rpcParams = default) {
        isDead.Value = false;
        health.Value = 100f;
        RespawnClientRpc();
    }

    [ClientRpc]
    public void RespawnClientRpc() {
        // TODO Fix this: Only owner can set their own position due to ClientNetworkTransform
        interactCollider.gameObject.layer = LayerMask.NameToLayer("Player");
        transform.GetComponent<Collider>().excludeLayers = 0;
        transform.tag = "Player";
        transform.gameObject.layer = LayerMask.NameToLayer("Player");
        foreach (MonoBehaviour obj in disableOnDeath) {
            obj.enabled = true;
        }
        animator.SetBool("isDead", false);
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
        // GUI.Label(new Rect(10, 10, 100, 20), $"Health: {health.Value}");
    }
    
}
