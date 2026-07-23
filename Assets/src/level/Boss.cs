using UnityEditor;
using UnityEngine;
using TMPro;
public class Boss : MonoBehaviour
{
    private int countdown;
    public TextMeshPro display;

    void Awake()
    {
        countdown = 500;
    }

    public int getCountdown()
    {
        return countdown;
    }


    public void decreaseCountdown()
    {
        countdown--;
    }

    void Update()
    {
        display.text = "" + countdown;
    }

}