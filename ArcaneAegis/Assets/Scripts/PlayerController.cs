using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float speed = 5f;

    private Vector3 velocity = Vector3.zero;

    private NetworkVariable<float> health = new NetworkVariable<float>(100f); 

    void Update()
    {
        // Get input from key presses and move accordingly
        if (IsOwner)
        {
            if (Input.GetKey(KeyCode.W))
            {
                transform.position += Vector3.forward * Time.deltaTime * speed;
            }
            if (Input.GetKey(KeyCode.S))
            {
                transform.position += Vector3.back * Time.deltaTime * speed;
            }
            if (Input.GetKey(KeyCode.A))
            {
                transform.position += Vector3.left * Time.deltaTime * speed;
            }
            if (Input.GetKey(KeyCode.D))
            {
                transform.position += Vector3.right * Time.deltaTime * speed;
            }
            if (transform.position.y > 0f)
            {
                // Slowly move down
                velocity += Vector3.down * Time.deltaTime * 10f;
            }
            else
            {
                // Reset velocity
                velocity = Vector3.zero;
            }
            // Jump
            if (Input.GetKeyDown(KeyCode.Space) && transform.position.y < 0.1f)
            {
                velocity += Vector3.up * 10f;
            }

            // Apply velocity
            transform.position += velocity * Time.deltaTime;

            if (transform.position.y < -10f)
            {
                health.Value -= 10f;
            }
            if (health.Value <= 0f)
            {
                health.Value = 100f;
                transform.position = new Vector3(0f, 0f, 0f);
            }
        }
    }
}
