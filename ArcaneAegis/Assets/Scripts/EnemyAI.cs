using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
public class EnemyAI : NetworkBehaviour
{
    // Range used for detection
    [SerializeField] private float attackRange = 2f;

    // Range used for attacking
    [SerializeField] private float hitRange = 1f;

    [SerializeField] private float attackDamage = 10f;

    [SerializeField] private float attackCooldown = 1f;
    private bool onCooldown = false;

    [SerializeField] private float spawnTime = 5f;

    private bool alive = true;

    private bool spawned = false;

    [SerializeField] private float despawnDelay = 5f;
    [SerializeField] private LayerMask playerLayer;

    

    private NetworkVariable<float> health = new NetworkVariable<float>(
        100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    [SerializeField] private GameObject model;

    private Animator animator;

    [SerializeField] private NavMeshAgent agent;
    // Update is called once per frame
    private Coroutine attackCoroutine = null;

    private Coroutine spawnCoroutine = null;

    private void Start() {
        // TODO Network animator
        animator = model.GetComponent<Animator>();

        // Start spawn coroutine
        spawnCoroutine = StartCoroutine(SpawnCoroutine());

    }

    IEnumerator SpawnCoroutine() {
        yield return new WaitForSeconds(spawnTime);
        spawned = true;
    }

    private Collider [] closePlayerList() {
        return Physics.OverlapSphere(transform.position+transform.up, attackRange, playerLayer);
    }

    private Collider [] hitPlayerList() {
        return Physics.OverlapSphere(transform.position+transform.forward+transform.up, hitRange, playerLayer);
    }

    void Update()
    {
        if(!spawned || !alive || !IsServer) return;
        // Get list of PlayerController scripts in scene and their positions
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players.Length == 0) return;

        // Check if any players in attack range
        Collider [] hitPlayers = hitPlayerList();
        if (hitPlayers.Length > 0) {
            Debug.Log("Hit player");
            agent.ResetPath(); // Stop agent
            animator.SetBool("Walking", false);
            Attack();
            return;
        }

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
        animator.SetBool("Walking", true);
    }


    void Attack() {
        if(onCooldown) return;
        if(attackCoroutine != null) StopCoroutine(attackCoroutine);
        attackCoroutine = StartCoroutine(AttackCoroutine());
        DoAttackServerRpc();
    }

    IEnumerator AttackCoroutine() {
        onCooldown = true;
        yield return new WaitForSeconds(attackCooldown);
        onCooldown = false;
    }



    [ServerRpc]
    public void DoAttackServerRpc() {
        // DoAttackClientRpc();
        // Play attack animation
        animator.SetTrigger("Attack"); // TODO Network animator

        // Check if any players in attack range
        Collider [] hitPlayers = closePlayerList();

        foreach (Collider player in hitPlayers) {
            Debug.Log("Hit player");
            PlayerController playerController = player.GetComponent<PlayerController>();
            if(playerController == null) continue;
            playerController.TakeDamageServerRpc(attackDamage);
        }
    }

    [ServerRpc(Delivery = default, RequireOwnership = false)]
    public void TakeDamageServerRpc(float damage) {
        health.Value -= damage;
        if (health.Value <= 0 && alive) {
            alive = false;
            EnemyDeathServerRpc();
        }
    }

    [ServerRpc(Delivery = default, RequireOwnership = false)]
    private void EnemyDeathServerRpc() {
        // Play death animation
        animator.SetTrigger("Die");
        GetComponent<NavMeshAgent>().enabled = false;
        GetComponent<Collider>().enabled = false; // TODO Create ragdoll
        StartCoroutine(DespawnCoroutine());
    }

    IEnumerator DespawnCoroutine() {
        yield return new WaitForSeconds(despawnDelay);
        Destroy(model);
        Destroy(gameObject);
        GetComponent<NetworkObject>().Despawn();
    }

    void OnDrawGizmosSelected() {
        Debug.Log("Draw");
        Gizmos.DrawWireSphere(transform.position+transform.forward+transform.up, hitRange);
        Gizmos.DrawWireSphere(transform.position+transform.up, attackRange);
    }
}
