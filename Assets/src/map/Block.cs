using UnityEngine;

public class Block : MonoBehaviour
{

    void Awake()
    {
        
    }

    public int applyAffect(int countdown)
    {
        return countdown -= 25;
    }

    void Update()
    {
        
    }
    
}