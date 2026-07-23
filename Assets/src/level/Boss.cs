using UnityEngine;

public class Boss : MonoBehaviour
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