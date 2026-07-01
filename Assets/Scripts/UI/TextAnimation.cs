using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextAnimation : MonoBehaviour
{
    public float delayBetweenCharacters = 0.05f; // Delay between each character
    public Text textComponent;

    private string fullText;
    private string currentText = "";
    private float timeElapsed = 0f;
    private int characterIndex = 0;

    void Start()
    {
        fullText = textComponent.text;
        textComponent.text = ""; // Clear the text initially
    }

    void Update()
    {
        if (currentText != fullText)
        {
            timeElapsed += Time.deltaTime;

            if (timeElapsed > delayBetweenCharacters)
            {
                timeElapsed = 0f;
                currentText += fullText[characterIndex];
                characterIndex++;
                textComponent.text = currentText;
            }
        }
    }
}
