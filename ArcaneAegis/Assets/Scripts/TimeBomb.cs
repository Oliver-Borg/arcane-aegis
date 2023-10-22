using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class TimeBomb : NetworkBehaviour
{
    [SerializeField] Light [] lights;
    [SerializeField] GameObject explosion;

    NetworkVariable<bool> fireRune = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );
    NetworkVariable<bool> gravityRune = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );
    NetworkVariable<bool> iceRune = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );
    NetworkVariable<bool> lightningRune = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    [SerializeField] private bool debug;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer) {
            if (debug) {
                AddElementRuneServerRpc(0);
                AddElementRuneServerRpc(1);
                AddElementRuneServerRpc(2);
                AddElementRuneServerRpc(3);
            }  
        }
        foreach (Light light in lights) {
            light.enabled = false;
        }
    }

    [ServerRpc(Delivery = default, RequireOwnership = false)]
    public void AddElementRuneServerRpc(int elementIndex, ServerRpcParams rpcParams = default) {
        bool allRunes = true;
        switch (elementIndex) {
            case 0:
                fireRune.Value = true;
                break;
            case 1:
                gravityRune.Value = true;
                break;
            case 2:
                iceRune.Value = true;
                break;
            case 3:
                lightningRune.Value = true;
                break;
        }
        allRunes = fireRune.Value && gravityRune.Value && iceRune.Value && lightningRune.Value;
        if (allRunes) {
            DoExplosionClientRpc();
        }

    }

    [ClientRpc]
    public void DoExplosionClientRpc() {
        StartCoroutine(DoExplosion());
    }

    IEnumerator DoExplosion() {
        foreach (Light light in lights) {
            light.enabled = true;
        }
        float startTime = Time.time;
        while (Time.time - startTime < 5f) {
            foreach (Light light in lights) {
                light.enabled = !light.enabled;
                yield return new WaitForSeconds(0.2f);
            }
        }
        // Disable all lights
        foreach (Light light in lights) {
            light.enabled = false;
        }
        Instantiate(explosion, transform.position, Quaternion.identity);
        if(IsServer) {
            // Do camera shake on all PlayerControllers
            foreach (NetworkClient player in NetworkManager.Singleton.ConnectedClientsList) {
                PlayerController playerController = player.PlayerObject.GetComponent<PlayerController>();
                if (playerController != null) {
                    StartCoroutine(playerController.CameraShakeCoroutine(100f));
                }
            }
            GameManager gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
            gameManager.KillAllEnemiesServerRpc();
            gameManager.GameWonServerRpc(); 
            yield return new WaitForSeconds(10f);
            NetworkManager.Singleton.Shutdown();
        }
        
    }

    void Update() {
        lights[0].enabled = fireRune.Value;
        lights[1].enabled = gravityRune.Value;
        lights[2].enabled = iceRune.Value;
        lights[3].enabled = lightningRune.Value;
    }

}
