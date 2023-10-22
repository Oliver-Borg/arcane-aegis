using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceSpawnDelay : MonoBehaviour
{
    [SerializeField] private float delay = 5f;

    [SerializeField] private GameObject objectToEnable;
    void Start()
    {
        // Enable parent and child components after 5 seconds to prevent audio bugs
        objectToEnable.SetActive(false);
        StartCoroutine(EnableAfterDelay());
    }

    IEnumerator EnableAfterDelay() {
        yield return new WaitForSeconds(5f);
        objectToEnable.SetActive(true);
    }

}
