using UnityEngine;
using Unity.Netcode;
using System.Security.Cryptography.X509Certificates;

public class Door : NetworkBehaviour
{
    [SerializeField] private Transform leftDoor = null;
    [SerializeField] private Transform rightDoor = null;
    [SerializeField] private float openAngle = 90.0f;
    [SerializeField] private float openSpeed = 2.0f;
    [SerializeField] private AudioSource doorOpenSound;

    NetworkVariable<bool> openState = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    [ServerRpc(Delivery = default, RequireOwnership = false)]
    public void OpenServerRpc(ServerRpcParams rpcParams = default)
    {
        openState.Value = true;
        OpenClientRpc();
    }
    [ClientRpc]
    public void OpenClientRpc()
    {
        if (doorOpenSound != null)
            doorOpenSound.Play();
    }
    public bool IsOpen()
    {
        return openState.Value;
    }

    void Update()
    {
        if (!IsServer) return;
        if (openState.Value)
        {
            if (leftDoor != null)
                leftDoor.localRotation = Quaternion.Slerp(leftDoor.localRotation, Quaternion.Euler(0, openAngle, 0), Time.deltaTime * openSpeed);
            if (rightDoor != null)
                rightDoor.localRotation = Quaternion.Slerp(rightDoor.localRotation, Quaternion.Euler(0, openAngle, 0), Time.deltaTime * openSpeed);
        }
    }



}
