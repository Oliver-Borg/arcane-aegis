using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
public class EnemyAI : NetworkBehaviour
{
    // [SerializeField] private GameObject enemyModel;

    // [SerializeField] private RuntimeAnimatorController animator;

    // Range used for detection
    [SerializeField] private float attackRange = 2f;

    // Range used for attacking
    [SerializeField] private float hitRange = 1f;

    [SerializeField] private float attackDamage = 10f;

    [SerializeField] private float attackCooldown = 1f;
    private bool onCooldown = false;

    [SerializeField] private float spawnTime = 5f;

    private bool spawned = false;

    [SerializeField] private LayerMask playerLayer;

    

    private NetworkVariable<float> health = new NetworkVariable<float>(
        100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner
    );

    private GameObject model;

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
        if(!spawned) return;
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
        DoAttackClientRpc();
    }

    [ClientRpc]
    public void DoAttackClientRpc() {

        // Play attack animation
        animator.SetTrigger("Attack");

        // Check if any players in attack range
        Collider [] hitPlayers = closePlayerList();

        foreach (Collider player in hitPlayers) {
            Debug.Log("Hit player");
            PlayerController playerController = player.GetComponent<PlayerController>();
            if(playerController == null) continue;
            playerController.DoDamageServerRpc(playerController.OwnerClientId, attackDamage);
        }
    }

    public void SetModelAndAnimator(GameObject modelPrefab, RuntimeAnimatorController animatorController) {
        model = Instantiate(modelPrefab, transform.position, Quaternion.identity);
        model.transform.parent = transform;
        model.GetComponent<Animator>().runtimeAnimatorController = animatorController;
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

    void OnDrawGizmosSelected() {
        Debug.Log("Draw");
        Gizmos.DrawWireSphere(transform.position+transform.forward+transform.up, hitRange);
        Gizmos.DrawWireSphere(transform.position+transform.up, attackRange);
    }
}
