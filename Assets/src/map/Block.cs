using TMPro;
using UnityEngine;

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
    SpriteRenderer symbolRenderer;

    [SerializeField] Sprite[] numberSprites;
    [SerializeField] Sprite[] operationSprites;

    public int NumberSpriteCount => numberSprites.Length;

    void Awake()
    {
        symbolRenderer = transform.Find("Sprite").GetComponent<SpriteRenderer>();
        symbolRenderer.transform.localScale *= 0.5f;

        if (display != null)
            display.gameObject.SetActive(false);
    }

    public void SetNumber(int index)
    {
        number = index + 1;
        operation = operationType.Empty;
        symbolRenderer.sprite = numberSprites[index];
    }

    public void SetOperation(operationType op)
    {
        operation = op;
        symbolRenderer.sprite = operationSprites[(int)operation];
    }

    public string getAffect()
    {
        switch (operation)
        {
            case operationType.Add: return "+";
            case operationType.Subtract: return "-";
            case operationType.Multiply: return "x";
            case operationType.Divide: return "/";
            case operationType.Empty: return number.ToString();
            default: return number.ToString();
        }
    }

    public string ToFriendlyString()
    {
        switch (operation)
        {
            case operationType.Add: return "+";
            case operationType.Subtract: return "-";
            case operationType.Multiply: return "x";
            case operationType.Divide: return "/";
            default: return operation.ToString();
        }
    }
}
