using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Collections;

public class GameManager : NetworkBehaviour {

    [SerializeField] private GameObject [] enemySpawnPoints;

    // Put hardest first
    [SerializeField] private GameObject [] enemyPrefabs;

    [SerializeField] private int enemyCount = 0;

    [SerializeField] private int maxRound = 25;

    [SerializeField] private float roundTime = 60f;

    [SerializeField] private int maxEnemies = 25;

    [SerializeField] private int minEnemies = 10;

    [SerializeField] private float roundStartSpawnRatio = 0.5f;

    [SerializeField] private float roundStartSpawnDelay = 0.1f;

    [SerializeField] private float roundStartDelay = 5f;

    [SerializeField] private float roundEndDelay = 5f;

    [SerializeField] private int maxUniqueEnemiesPerRound = 3;

    [SerializeField] private bool peaceful = false;



    private int round = 0;

    private bool roundStarted = false;
     

    void Update()
    {
        if (!IsServer) return;
        // Get all game objects with tag "Enemy"
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        // If there are less than enemyCount enemies, spawn a new one
        // if (enemies.Length < enemyCount) {
        //     // Get a random spawn point
        //     Transform spawnPoint = enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length)];
        //     // Spawn enemy
        //     SpawnEnemyServerRpc(spawnPoint.position);
        // }
        if (!roundStarted && enemies.Length == 0) {
            StartCoroutine(StartRound());
        }
        if (Input.GetKeyDown(KeyCode.K)) {
            KillAllEnemiesServerRpc();
        }
        if (Input.GetKeyDown(KeyCode.L)) {
            ReviveAllPlayersServerRpc();
        }
    }

    private float [] SpawnWeights (int round) {
        float [] weights = new float [enemyPrefabs.Length];

        // TODO improve distribution
        for (int i = 0; i < enemyPrefabs.Length; i++) {
            EnemyAI enemyAI = enemyPrefabs[i].GetComponent<EnemyAI>();
            weights[i] = (i + 1 + Mathf.Pow(round, 2) / maxRound) / Mathf.Sqrt(enemyAI.spawnWeight);
        }

        // Normalize weights
        float sum = 0;
        for (int i = 0; i < weights.Length; i++) {
            sum += weights[i];
        }
        for (int i = 0; i < weights.Length; i++) {
            weights[i] /= sum;
        }

        return weights;
    } 

    private int NumberOfEnemies (int round) {
        int numPlayers = GameObject.FindGameObjectsWithTag("Player").Length;
        return Mathf.RoundToInt(Mathf.Lerp(minEnemies, maxEnemies, round / maxRound))*numPlayers;
    }

    public GameObject [] GetActiveSpawnpoints() {
        
        int j = 0;
        for (int i = 0; i < enemySpawnPoints.Length; i++) {
            if (enemySpawnPoints[i].GetComponent<SpawnPoint>().IsActive()) {
                j++;
            }
        }
        GameObject [] activeSpawnpoints = new GameObject [j];
        j = 0;
        for (int i = 0; i < enemySpawnPoints.Length; i++) {
            if (enemySpawnPoints[i].GetComponent<SpawnPoint>().IsActive()) {
                activeSpawnpoints[j] = enemySpawnPoints[i];
                j++;
            }
        }
        return activeSpawnpoints;
    }

    private float SpawnEnemy(float [] weights) {
        float random = Random.Range(0f, 1f);
        float sum = 0;
        
        for (int i = 0; i < weights.Length; i++) {
            sum += weights[i];
            if (random < sum) {
                // Debug.Log("Spawning enemy model " + i + " with weight " + weights[i] + " in round " + round);
                GameObject [] activeSpawnpoints = GetActiveSpawnpoints();
                if (activeSpawnpoints.Length == 0) return 0;
                int spawnPointIndex = Random.Range(0, activeSpawnpoints.Length);
                Vector3 spawnPoint = activeSpawnpoints[spawnPointIndex].GetComponent<SpawnPoint>().GetSpawnPosition();

                SpawnEnemyServerRpc(i, spawnPoint);
                return enemyPrefabs[i].GetComponent<EnemyAI>().spawnWeight;
            }
        }
        return 0;
    }

    IEnumerator StartRound() {
        if (roundStarted || peaceful) yield break;
        roundStarted = true;
        yield return new WaitForSeconds(roundStartDelay);
        round++;
        enemyCount = NumberOfEnemies(round);
        float [] weights = SpawnWeights(round);

        int uniqueEnemiesThisRound = Random.Range(1, maxUniqueEnemiesPerRound + 1);

        NetworkLog.LogInfoServer("Spawning " + enemyCount + " enemies in round " + round);
        NetworkLog.LogInfoServer("from " + uniqueEnemiesThisRound + " unique enemies types");
        

        int excluded = weights.Length - uniqueEnemiesThisRound;

        // Rebalance weights to exclude smallest weights
        // for (int i = 0; i < excluded; i++) {
        //     float min = Mathf.Infinity;
        //     int minIndex = 0;
        //     for (int j = 0; j < weights.Length; j++) {
        //         if (weights[j] < min && weights[j] != 0) {
        //             min = weights[j];
        //             minIndex = j;
        //         }
        //     }
        //     weights[minIndex] = 0;
        // }

        float [] buckets = new float [weights.Length];
        buckets[0] = weights[0];
        for (int i = 1; i < weights.Length; i++) {
            buckets[i] = weights[i] + buckets[i-1];
        }

        float [] newWeights = new float [weights.Length];

        for (int i = 0; i < uniqueEnemiesThisRound; i++) {
            float random = Random.Range(0f, 1f);
            for (int j = 0; j < buckets.Length; j++) {
                if (random < buckets[j]) {
                    newWeights[j] = weights[j];
                    break;
                }
            }
        }
        weights = newWeights;

        // Renormalize weights
        float sum = 0;
        for (int i = 0; i < weights.Length; i++) sum += weights[i];
        for (int i = 0; i < weights.Length; i++) weights[i] /= sum;

        for (int i = 0; i < weights.Length; i++) {
            NetworkLog.LogInfoServer("Enemy model " + i + " has weight " + weights[i]);
        }

        float startEnemyMass = enemyCount*roundStartSpawnRatio;
        float remainingEnemyMass = enemyCount - startEnemyMass;
        while (startEnemyMass > 0) {
            yield return new WaitForSeconds(roundStartSpawnDelay);
            startEnemyMass -= SpawnEnemy(weights);
        }

        

        while (remainingEnemyMass > 0) {
            yield return new WaitForSeconds(roundTime/enemyCount);
            remainingEnemyMass -= SpawnEnemy(weights);
        }

        yield return new WaitForSeconds(roundEndDelay);
        roundStarted = false;
    }


    [ServerRpc]
    public void SpawnEnemyServerRpc(int enemyModelIndex, Vector3 spawnPoint, ServerRpcParams rpcParams = default) {
        // Debug.Log("Spawning enemy model " + enemyModelIndex);
        GameObject enemy = Instantiate(enemyPrefabs[enemyModelIndex], spawnPoint, Quaternion.identity);
        // Spawn enemy on clients
        enemy.GetComponent<NetworkObject>().Spawn();
        // NetworkLog.LogInfoServer("Spawned enemy");
    }

    [ServerRpc]
    public void KillAllEnemiesServerRpc(ServerRpcParams rpcParams = default) {
        // Get all game objects with tag "Enemy"
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies) {
            enemy.GetComponent<EnemyAI>().TakeDamageServerRpc(1000000f);
        }
    }

    [ServerRpc]
    public void ReviveAllPlayersServerRpc(ServerRpcParams rpcParams = default) {
        GameObject[] players = GameObject.FindGameObjectsWithTag("DeadPlayer");
        foreach (GameObject player in players) {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController.IsDead()) {
                NetworkLog.LogInfoServer("Reviving player");
                playerController.ReviveServerRpc();
            }
        }
    }
}
