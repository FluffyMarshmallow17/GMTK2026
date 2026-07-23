using UnityEngine;

public class Block : MonoBehaviour
{
    public int applyAffect(int countdown)
    {
        return countdown -= 25;
    }
}