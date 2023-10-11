using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Collections;

public class GameManager : NetworkBehaviour {

    [SerializeField] private Transform [] enemySpawnPoints;

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
    }

    private float [] SpawnWeights (int round) {
        float [] weights = new float [enemyPrefabs.Length];

        // TODO improve distribution
        for (int i = 0; i < enemyPrefabs.Length; i++) {
            EnemyAI enemyAI = enemyPrefabs[i].GetComponent<EnemyAI>();
            weights[i] = (i + 1 + Mathf.Pow(round, 2) / maxRound) / enemyAI.spawnWeight;
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
        return Mathf.RoundToInt(Mathf.Lerp(minEnemies, maxEnemies, round / maxRound));
    }

    private void SpawnEnemy(float [] weights) {
        float random = Random.Range(0f, 1f);
        float sum = 0;
        for (int i = 0; i < weights.Length; i++) {
            sum += weights[i];
            if (random < sum) {
                Debug.Log("Spawning enemy model " + i + " with weight " + weights[i] + " in round " + round);
                SpawnEnemyServerRpc(i, enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length)].position);
                break;
            }
        }
    }

    IEnumerator StartRound() {
        if (roundStarted) yield break;
        roundStarted = true;
        yield return new WaitForSeconds(roundStartDelay);
        round++;
        enemyCount = NumberOfEnemies(round);
        float [] weights = SpawnWeights(round);
        int startEnemies = Mathf.RoundToInt(enemyCount*roundStartSpawnRatio);
        for (int i = 0; i < startEnemies; i++) {
            yield return new WaitForSeconds(roundStartSpawnDelay);
            SpawnEnemy(weights);
        }

        int remainingEnemies = enemyCount - startEnemies;

        while (remainingEnemies > 0) {
            yield return new WaitForSeconds(roundTime/enemyCount);
            SpawnEnemy(weights);
            remainingEnemies--;
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
}
