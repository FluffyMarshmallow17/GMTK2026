using UnityEngine;

public class Zone : MonoBehaviour
{
    public int cost;
    public int size;
    public operationType type;
    public int number;

    void Awake()
    {
        CircleCollider2D collider = gameObject.GetComponent<CircleCollider2D>();
        collider.radius = size;
        // set up the line so it shows the size. similar to Map.cs
        // instantiate a bunch of the same type of blocks inside the radius. same probability as given in leveldata --> so if a multiplier has a 0.05 chance, then theres a 0.05 chance here. if 9 has a 0.1 chance, then a 0.1 chance here.spawn anywhree from 2-5 of these.
        // add a tint inside the collider. 
    }

    public void OnTriggerEnter2D(Collider2D collider) {
        if (collider.transform.CompareTag("Player")) {
            Player player = collider.transform.GetComponent<Player>();
            player.setCountdown(player.getCountdown() - cost);
        }
        // remove collider and collider circle line. zone is now accessible to player
    }

}