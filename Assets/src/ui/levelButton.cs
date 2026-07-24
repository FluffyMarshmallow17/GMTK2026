using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelButton : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Button button;

    public Button Button => button;

    public void SetText(string text)
    {
        label.text = text;
    }
}