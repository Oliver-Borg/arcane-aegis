using UnityEngine;
using Unity.Netcode;
using System.Security.Cryptography.X509Certificates;

public class Door : NetworkBehaviour
{
    [SerializeField] private Transform leftDoor = null;
    [SerializeField] private Transform rightDoor = null;
    [SerializeField] private float openAngle = 90.0f;
    [SerializeField] private float openSpeed = 2.0f;

    public bool open = false; // This is used to enable monster spawning
    [ServerRpc(Delivery = default, RequireOwnership = false)]
    public void OpenServerRpc(ServerRpcParams rpcParams = default)
    {
        open = true;
    }

    void Update()
    {
        if (!IsServer) return;
        if (open)
        {
            if (leftDoor != null)
                leftDoor.localRotation = Quaternion.Slerp(leftDoor.localRotation, Quaternion.Euler(0, openAngle, 0), Time.deltaTime * openSpeed);
            if (rightDoor != null)
                rightDoor.localRotation = Quaternion.Slerp(rightDoor.localRotation, Quaternion.Euler(0, openAngle, 0), Time.deltaTime * openSpeed);
        }
    }



}
