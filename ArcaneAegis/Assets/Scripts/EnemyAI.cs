using Unity.Netcode;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
public class EnemyAI : NetworkBehaviour
{
    [SerializeField] private GameObject enemyModel;

    [SerializeField] private AnimatorController animator;

    private GameObject model;

    [SerializeField] private NavMeshAgent agent;
    // Update is called once per frame

    private void Start() {
        model = Instantiate(enemyModel, transform.position, Quaternion.identity);
        model.transform.parent = transform;

        // Set model animation controller
        model.GetComponent<Animator>().runtimeAnimatorController = animator;
        // TODO Network animator
        EnemySpawnServerRpc();
    }

    [ServerRpc]
    private void EnemySpawnServerRpc() {
        model.GetComponent<NetworkObject>().Spawn();
    }

    void Update()
    {
        // Get list of PlayerController scripts in scene and their positions
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players.Length == 0) return;

        NetworkLog.LogInfoServer("Enemy AI is running");
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
}
