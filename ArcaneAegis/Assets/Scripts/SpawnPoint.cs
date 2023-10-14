using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class SpawnPoint : NetworkBehaviour
{
    [SerializeField] private GameObject requiredDoor = null;
    [SerializeField] private float spawnCooldown = 5f;

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
}
