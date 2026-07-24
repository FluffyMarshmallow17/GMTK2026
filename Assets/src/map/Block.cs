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
}
public class Block : MonoBehaviour
{
    int number;
    operationType operation;
    public TextMeshPro display;

    void Awake()
    {
        float prob = UnityEngine.Random.Range(0, 1f);
        Array operationStrings = Enum.GetValues(typeof(operationType));
        if (prob > 0.2)
        {
            number = UnityEngine.Random.Range(0, 10);
            display.text = "" + number;
        }
        else
        {
            int randomIndex = UnityEngine.Random.Range(1,5);
            operation = (operationType) operationStrings.GetValue(randomIndex);
            display.text = "" + ToFriendlyString(operation);
            number = 0;
        }
    }

    public string ToFriendlyString(operationType op)
    {
        switch (op)
        {
            case operationType.Add:      return "+";
            case operationType.Subtract: return "-";
            case operationType.Multiply: return "x";
            case operationType.Divide:   return "/";
            default:                 return op.ToString();
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