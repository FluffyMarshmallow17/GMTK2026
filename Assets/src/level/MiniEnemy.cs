using UnityEngine;

public class MiniEnemy : MonoBehaviour
{
    private int countdown;

    void Awake()
    {
        
    }

    public int getCountdown()
    {
        return countdown;
    }


    public void decreaseCountdown()
    {
        countdown--;
    }
    
}