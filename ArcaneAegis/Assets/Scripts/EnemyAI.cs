using System.Collections;
using System.Collections.Generic;
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

    [SerializeField] private float maxHealth = 100f;
    private bool onCooldown = false;

    [SerializeField] private float spawnTime = 5f;

    public float spawnWeight = 1f;

    private bool alive = true;

    private bool spawned = false;

    [SerializeField] private float despawnDelay = 5f;
    [SerializeField] private LayerMask playerLayer;

    [SerializeField] private GameObject [] weapons;

    [SerializeField] private GameObject [] upgrades;

    [SerializeField] private float dropChance = 0.5f;

    [SerializeField] private ElementEnum [] weaknesses;
    [SerializeField] private float stunTime = 0.01f;

    [SerializeField] private float stunCooldown = 0.05f;

    [SerializeField] private float slowTime = 5f;

    

    private bool stunned = false;

    private bool stunnable = true;

    private bool slowed = false;

    private Dictionary<ElementEnum, float> damageTaken = new Dictionary<ElementEnum, float>();

    private Dictionary<PlayerController, float> damagers = new Dictionary<PlayerController, float>();

    private Dictionary<ElementEnum, GameObject> upgradePrefabs = new Dictionary<ElementEnum, GameObject>();

    private NetworkVariable<float> health = new NetworkVariable<float>(
        100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    private Animator animator;

    [SerializeField] private NavMeshAgent agent;
    private Coroutine attackCoroutine = null;

    private Coroutine spawnCoroutine = null;

    public override void OnNetworkSpawn() {
        animator = GetComponent<Animator>();
        // Randomly choose weapon and disable others (enemies have multiple weapons by default)
        int weaponIndex = Random.Range(0, weapons.Length);
        for (int i = 0; i < weapons.Length; i++) {
            if (i == weaponIndex) continue;
            weapons[i].SetActive(false);
        }

        // Assign upgrades correctly
        foreach (GameObject upgrade in upgrades) {
            Upgrade upgradeScript = upgrade.GetComponent<Upgrade>();
            upgradePrefabs[upgradeScript.upGradeElement] = upgrade;
        }

        // Start spawn coroutine
        spawnCoroutine = StartCoroutine(SpawnCoroutine());
        if (IsServer) health.Value = maxHealth;
    }

    IEnumerator SpawnCoroutine() {
        yield return new WaitForSeconds(spawnTime);
        spawned = true;
    }

    private Collider [] closePlayerList() {
        return Physics.OverlapSphere(transform.position+transform.up, attackRange, playerLayer);
    }

    private Collider [] hitPlayerList() {
        return Physics.OverlapSphere(transform.position+transform.forward*attackRange+transform.up, hitRange, playerLayer);
    }

    void Update()
    {
        // Enemies are completely controlled by the server
        if(!spawned || !alive || !IsServer || stunned) return;
        // Get list of PlayerController scripts in scene and their positions
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players.Length == 0) return;

        // Check if any players in attack range
        Collider [] hitPlayers = hitPlayerList();
        if (hitPlayers.Length > 0) {
            agent.ResetPath(); // Stop agent
            SetAnimationBoolClientRpc("Walking", false);
            Attack();
            return;
        }

        // Check if players close and start rotating towards them if so
        Collider [] closePlayers = closePlayerList();
        if (closePlayers.Length > 0) {
            agent.ResetPath(); // Stop agent
            // Rotate the enemy towards the player in small steps
            Vector3 direction = (closePlayers[0].transform.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
            float lookspeed = agent.angularSpeed/180*Mathf.PI;
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * lookspeed);
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
        SetAnimationBoolClientRpc("Walking", true);
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
        // TODO Improve this behaviour either using delayed attacks, or by using a trigger collider
        // Check if any players in attack range
        Collider [] hitPlayers = hitPlayerList();
        if (hitPlayers.Length == 0) return;
        // Play attack animation
        PlayAnimationClientRpc("Attack");

        foreach (Collider player in hitPlayers) {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if(playerController == null) continue;
            playerController.TakeDamageServerRpc(attackDamage);
        }
    }

    [ServerRpc(Delivery = default, RequireOwnership = false)]
    public void TakeDamageServerRpc(float damage, int elementIndex, ulong playerIndex, ServerRpcParams rpcParams = default) {
        ElementEnum element = (ElementEnum) elementIndex;
        if (element == ElementEnum.Lightning) {
            StartCoroutine(StunCoroutine());
        }
        else if (element == ElementEnum.Ice) {
            StartCoroutine(SlowCoroutine());
        }
        // Check if element in weaknesses
        foreach (ElementEnum weakness in weaknesses) {
            if (weakness == element) {
                damage *= 1.5f;
                break;
            }
        }
        // Check if player in damagers
        PlayerController player = NetworkManager.Singleton.ConnectedClients[playerIndex].PlayerObject.GetComponent<PlayerController>();

        if (damagers.ContainsKey(player)) {
            damagers[player] += damage;
        }
        else {
            damagers[player] = damage;
        }

        // Add element to damage taken
        // This is kind of a bug cause we can overdamage enemies, 
        // but I think this behaviour is nice for the drop chances
        if (damageTaken.ContainsKey(element)) {
            damageTaken[element] += damage;
        }
        else {
            damageTaken[element] = damage;
        }

        health.Value -= damage;
        if (health.Value <= 0 && alive) {
            alive = false;
            EnemyDeathServerRpc();
        }
    }

    IEnumerator StunCoroutine() {
        if (!IsServer || !stunnable) yield break;
        agent.isStopped = true;
        stunned = true;
        stunnable = false;
        PlayAnimationClientRpc("Stun");
        SetAnimationBoolClientRpc("Walking", false);
        yield return new WaitForSeconds(stunTime);
        stunned = false;
        if (alive)
            agent.isStopped = false;
        SetAnimationBoolClientRpc("Walking", true);
        yield return new WaitForSeconds(stunCooldown);
        stunnable = true;
    }

    IEnumerator SlowCoroutine() {
        if (!IsServer || slowed) yield break;
        agent.speed /= 2;
        slowed = true;
        animator.speed /= 2;
        yield return new WaitForSeconds(slowTime);
        agent.speed *= 2;
        animator.speed *= 2;
        slowed = false;
    }

    [ServerRpc(Delivery = default, RequireOwnership = false)]
    private void EnemyDeathServerRpc() {
        // Play death animation
        PlayAnimationClientRpc("Die");
        GetComponent<NavMeshAgent>().enabled = false;
        GetComponent<Collider>().enabled = false; // TODO Create ragdoll
        DropUpgradeServerRpc();
        // Give points to players
        foreach (KeyValuePair<PlayerController, float> damager in damagers) {
            GameObject playerObject = damager.Key.gameObject;
            PlayerInventory playerInventory = playerObject.GetComponent<PlayerInventory>();
            playerInventory.AddPointsServerRpc(damager.Value);
        }
        StartCoroutine(DespawnCoroutine());
    }

    [ServerRpc]
    private void DropUpgradeServerRpc() {
        if (Random.Range(0f, 1f) > dropChance) return;
        // Choose upgrade drop randomly based on amount of damage taken
        float totalDamage = 0f;
        foreach (float damage in damagers.Values) {
            totalDamage += damage;
        }
        float randomDamage = Random.Range(0f, totalDamage);
        float currentDamage = 0f;
        ElementEnum upgradeElement = ElementEnum.Fire;
        foreach (KeyValuePair<ElementEnum, float> damage in damageTaken) {
            currentDamage += damage.Value;
            if (currentDamage > randomDamage) {
                upgradeElement = damage.Key;
                break;
            }
        }
        // Spawn upgrade
        GameObject upgradePrefab = upgradePrefabs[upgradeElement];
        GameObject upgrade = Instantiate(upgradePrefab, transform.position, Quaternion.identity);
        upgrade.GetComponent<NetworkObject>().Spawn();
    }

    [ClientRpc]
    private void PlayAnimationClientRpc(string animation) {
        // TODO Try and get NetworkAnimator working instead
        animator.SetTrigger(animation);
    }

    [ClientRpc]
    private void SetAnimationBoolClientRpc(string animation, bool value) {
        animator.SetBool(animation, value);
    }

    IEnumerator DespawnCoroutine() {
        yield return new WaitForSeconds(despawnDelay);
        Destroy(gameObject);
        GetComponent<NetworkObject>().Despawn();
    }

    void OnDrawGizmosSelected() {
        Gizmos.DrawWireSphere(transform.position+transform.forward*attackRange+transform.up, hitRange);
        Gizmos.DrawWireSphere(transform.position+transform.up, attackRange);
    }
}
