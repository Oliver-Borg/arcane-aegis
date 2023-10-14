using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class SpawnPoint : NetworkBehaviour
{
    [SerializeField] private GameObject requiredDoor = null;
    [SerializeField] private float spawnCooldown = 5f;

    [SerializeField] private float spawnRadius = 1f;

    private bool onCooldown = false;

    IEnumerator SpawnCooldown() { // TODO actually use this?
        onCooldown = true;
        yield return new WaitForSeconds(spawnCooldown);
        onCooldown = false;
    }
    public bool IsActive() {
        if (onCooldown) return false;
        if (requiredDoor == null) return true;
        Door door = requiredDoor.GetComponent<Door>();
        return door.IsOpen();
    }

    public Vector3 GetSpawnPosition() {
        Vector3 spawnPosition = transform.position;
        spawnPosition.x += Random.Range(-spawnRadius, spawnRadius);
        spawnPosition.z += Random.Range(-spawnRadius, spawnRadius);
        return spawnPosition;
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
