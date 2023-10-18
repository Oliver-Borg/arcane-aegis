using UnityEngine;
using Unity.Netcode;

public class CatalystSpawn : NetworkBehaviour
{
    public GameObject [] spawnPoints;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)].transform;
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }
    }
}
