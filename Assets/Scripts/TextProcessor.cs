using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Text;

public class TextProcessor : TextProcessorBase
{
    [SerializeField] private string openAIApiKey;
    [SerializeField] private string openAIEndpoint = "https://api.openai.com/v1/chat/completions";

    public override void ProcessText(string text)
    {
        Debug.Log("TextProcessor ProcessText called with: " + text);
        StartCoroutine(SendToOpenAI(text));
    }

    public override void ProcessPartialText(string partialText)
    {
        Debug.Log("TextProcessor ProcessPartialText called with: " + partialText);
        // Implement any logic for partial text processing here
    }

    private IEnumerator SendToOpenAI(string text)
    {
        string jsonPayload = $"{{\"model\": \"gpt-3.5-turbo\", \"messages\": [{{\"role\": \"user\", \"content\": \"{text}\"}}]}}";

        using (UnityWebRequest request = new UnityWebRequest(openAIEndpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + openAIApiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + request.error);
            }
            else
            {
                string response = request.downloadHandler.text;
                Debug.Log("OpenAI Response: " + response);
                // Process the response here
            }
        }
    }
}