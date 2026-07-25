using TMPro;
using UnityEngine;

public enum operationType
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Decay,
    Grow,
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
    [SerializeField] Material decayGlow;
    [SerializeField] Material growthGlow;

    Material defaultMaterial;

    public int NumberSpriteCount => numberSprites.Length;

    void Awake()
    {
        symbolRenderer = transform.Find("Sprite").GetComponent<SpriteRenderer>();
        symbolRenderer.transform.localScale *= 0.5f;
        defaultMaterial = symbolRenderer.sharedMaterial;

        if (display != null)
            display.gameObject.SetActive(false);
    }

    public void SetNumber(int index)
    {
        number = index + 1;
        operation = operationType.Empty;
        symbolRenderer.sprite = numberSprites[index];
        ApplyGlowMaterial();
    }

    public void SetOperation(operationType op)
    {
        operation = op;
        symbolRenderer.sprite = operationSprites[(int)operation];
        ApplyGlowMaterial();
    }

    void ApplyGlowMaterial()
    {
        if (symbolRenderer == null)
            return;

        if (operation == operationType.Decay && decayGlow != null)
            symbolRenderer.material = decayGlow;
        else if (operation == operationType.Grow && growthGlow != null)
            symbolRenderer.material = growthGlow;
        else
            symbolRenderer.material = defaultMaterial;
    }

    public string getAffect()
    {
        switch (operation)
        {
            case operationType.Add: return "+";
            case operationType.Subtract: return "-";
            case operationType.Multiply: return "x";
            case operationType.Divide: return "/";
            case operationType.Decay: return "decay";
            case operationType.Grow: return "grow";
            case operationType.Empty: return number.ToString();
            default: return number.ToString();
        }
    }

    public Sprite GetSymbolSprite()
    {
        return symbolRenderer != null ? symbolRenderer.sprite : null;
    }

    public Material GetSymbolMaterial()
    {
        return symbolRenderer != null ? symbolRenderer.sharedMaterial : null;
    }

    public string ToFriendlyString()
    {
        switch (operation)
        {
            case operationType.Add: return "+";
            case operationType.Subtract: return "-";
            case operationType.Multiply: return "x";
            case operationType.Divide: return "/";
            case operationType.Decay: return "decay";
            case operationType.Grow: return "grow";            
            default: return operation.ToString();
        }
    }
}
