using Unity.Netcode;
using UnityEngine;

public class Teleporter : NetworkBehaviour
{
    [SerializeField] private GameObject [] spawnPoints;

    [SerializeField] private bool isEnd = false;

    [SerializeField] private Transform targetTransform;

    [SerializeField] private GameObject gameManager;

    [SerializeField] private bool toSpace = true;

    [SerializeField] private AudioSource teleportSound;

    public GameObject spaceStation;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer && !isEnd)
        {
            if (spawnPoints.Length == 0) return;
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)].transform;
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }
    }

    public Transform GetTeleportTransform() {
        return targetTransform;
    }

    [ServerRpc(Delivery = default, RequireOwnership = false)]
    public void TeleportServerRpc(ServerRpcParams rpcParams = default) {
        if (toSpace)
            gameManager.GetComponent<GameManager>().AddSpacePlayerServerRpc();
        else
            gameManager.GetComponent<GameManager>().RemoveSpacePlayerServerRpc();
        //Play sound
        PlayTeleportSoundClientRpc();
        if (IsServer && !isEnd) {
            if (spawnPoints.Length == 0) return;
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)].transform;
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }
    }

    [ClientRpc]
    public void PlayTeleportSoundClientRpc() {
        teleportSound.Play();
    }
}
