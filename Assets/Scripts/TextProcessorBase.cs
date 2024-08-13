using UnityEngine;

public class TextProcessorBase : MonoBehaviour
{
    public virtual void ProcessText(string text)
    {
        Debug.Log("Base ProcessText called with: " + text);
    }

    public virtual void ProcessPartialText(string partialText)
    {
        Debug.Log("Base ProcessPartialText called with: " + partialText);
    }
}