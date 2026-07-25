using System;
using UnityEngine;

public class Map : MonoBehaviour
{
    public float mapSize;
    private float destinationScale;
    private void Start()
    {
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(destinationScale, destinationScale, 0.1f), Time.deltaTime);
    }

    public void resizeMap(int totalCountdown)
    {
         destinationScale = Mathf.Sqrt(totalCountdown) * mapSize;
    }

    void OnTriggerExit(Collider other)
    {
        print("Player exited the map");
        if (other.CompareTag("Player"))
        {
            print("Player exited the map1");
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                print("Player exited the map2");
                player.decreaseCountdown((int)(player.getCountdown() * 0.2f));
            }
        }
    }
}