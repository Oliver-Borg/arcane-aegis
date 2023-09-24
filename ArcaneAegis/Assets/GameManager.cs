using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class GameManager : NetworkBehaviour {

    [SerializeField] private Transform [] enemySpawnPoints;

    [SerializeField] private GameObject enemyPrefab;

    [SerializeField] private int enemyCount = 0;

    void Update()
    {
        if (!IsServer) return;
        // Get all game objects with tag "Enemy"
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        // If there are less than enemyCount enemies, spawn a new one
        if (enemies.Length < enemyCount) {
            // Get a random spawn point
            Transform spawnPoint = enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length)];
            // Spawn enemy
            GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
            // Spawn enemy on clients
            enemy.GetComponent<NetworkObject>().Spawn();
            NetworkLog.LogInfoServer("Spawned enemy");
            
        }
    }
}
