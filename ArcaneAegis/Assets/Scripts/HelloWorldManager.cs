using Unity.Netcode;
using UnityEngine;

namespace HelloWorld
{
    public class HelloWorldManager : MonoBehaviour
    {
        // void OnGUI()
        // {
        //     GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        //     if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        //     {
        //         StartButtons();
        //     }

        //     GUILayout.EndArea();
        // }

        // static void StartButtons()
        // {
        //     if (GUILayout.Button("Host")) NetworkManager.Singleton.StartHost();
        //     if (GUILayout.Button("Client")) NetworkManager.Singleton.StartClient();
        //     if (GUILayout.Button("Server")) NetworkManager.Singleton.StartServer();
        // }

        public static void Host()
        {
            NetworkManager.Singleton.StartHost();
        }
        public static void Client()
        {
            NetworkManager.Singleton.StartClient();
        }
        public static void Server()
        {
            NetworkManager.Singleton.StartServer();
        }
    }
}