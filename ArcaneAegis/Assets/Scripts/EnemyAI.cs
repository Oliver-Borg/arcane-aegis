using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
public class EnemyAI : NetworkBehaviour
{
    [SerializeField] private GameObject enemyModel;

    [SerializeField] private RuntimeAnimatorController animator;

    private NetworkVariable<float> health = new NetworkVariable<float>(
        100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner
    );

    private GameObject model;

    [SerializeField] private NavMeshAgent agent;
    // Update is called once per frame

    private void Start() {
        model = Instantiate(enemyModel, transform.position, Quaternion.identity);
        model.transform.parent = transform;

        // Set model animation controller
        model.GetComponent<Animator>().runtimeAnimatorController = animator;
        // TODO Network animator
    }

    void Update()
    {
        // Get list of PlayerController scripts in scene and their positions
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players.Length == 0) return;
        // Find closest player
        float closestDistance = 100000f;
        GameObject closestPlayer = null;
        foreach (GameObject player in players) {
            float distance = Vector3.Distance(player.transform.position, transform.position);
            if (distance < closestDistance) {
                closestDistance = distance;
                closestPlayer = player;
            }
        }
        agent.SetDestination(closestPlayer.transform.position);
    }

    [ClientRpc]
    public void TakeDamageClientRpc(float damage) {
        health.Value -= damage;
        if (health.Value <= 0) {
            EnemyDeathServerRpc();
        }
    }

    [ServerRpc]
    private void EnemyDeathServerRpc() {
        GetComponent<NetworkObject>().Despawn();
        Destroy(gameObject);
    }
}
