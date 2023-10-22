using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

namespace HelloWorld
{
    public class HelloWorldManager : MonoBehaviour
    { 
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private InputField ipInput;

        public void Host()
        {
            SetIP(ipInput.text);
            NetworkManager.Singleton.StartHost();
            mainMenu.SetActive(false);
        }

        public void Client()
        {
            SetIP(ipInput.text);
            NetworkManager.Singleton.StartClient();
            mainMenu.SetActive(false);
        }

        public void Server()
        {
            SetIP(ipInput.text);
            NetworkManager.Singleton.StartServer();
            mainMenu.SetActive(false);
        }

        public void SetIP(string ip)
        {
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.ConnectionData.Address = ip;
        }
    }
}