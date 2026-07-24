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

    void Awake()
    {
        symbolRenderer = transform.Find("Sprite").GetComponent<SpriteRenderer>();
        symbolRenderer.transform.localScale *= 0.5f;

        if (Random.value > 0.2f)
        {
            number = Random.Range(1, numberSprites.Length + 1);
            operation = operationType.Empty;
            symbolRenderer.sprite = numberSprites[number - 1];
        }
        else
        {
            operation = (operationType)Random.Range(0, 4);
            symbolRenderer.sprite = operationSprites[(int)operation];
        }

        if (display != null)
            display.gameObject.SetActive(false);
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
