using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
public enum operationType
{
    Add, 
    Subtract,
    Multiply,
    Divide,
    Empty,
}
public class Block : MonoBehaviour
{
    int number;
    operationType operation;
    public TextMeshPro display;

    void Awake()
    {
        if (UnityEngine.Random.value > 0.2f)
        {
            number = UnityEngine.Random.Range(1, 10);
            display.text = number.ToString();
            operation = operationType.Empty;
        }
        else
        {
            operation = (operationType)UnityEngine.Random.Range(0, 4);
            display.text = ToFriendlyString();
        }
    }

    public string getAffect()
    {
        switch (operation)
        {
            case operationType.Add:      return "+";
            case operationType.Subtract: return "-";
            case operationType.Multiply: return "x";
            case operationType.Divide:   return "/";
            case operationType.Empty:    return number + "";
            default:                 return number + "";
        }
    }

    public string ToFriendlyString()
    {
        switch (operation)
        {
            case operationType.Add:      return "+";
            case operationType.Subtract: return "-";
            case operationType.Multiply: return "x";
            case operationType.Divide:   return "/";
            default:                 return operation.ToString();
        }
    }

    public int applyAffect(int countdown)
    {
        return countdown -= 25;
    }

    void Update()
    {
        
    }

}