using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using Vosk;
using SimpleJSON;

public class RealtimeSpeechToText : MonoBehaviour
{
    [Header("Vosk Settings")]
    public string modelName = "vosk-model-small-en-us-0.15";

    [Header("Microphone Settings")]
    public string selectedMicrophone;
    public List<string> availableMicrophones = new List<string>();

    [Header("Audio Processing")]
    public float silenceThreshold = 0.02f;

    [Header("Debug")]
    [SerializeField] private float currentVolume;
    [SerializeField] private bool isSpeaking;
    [SerializeField] private string lastRecognizedText;

    private AudioClip microphoneClip;
    private VoskRecognizer recognizer;
    private const int SampleRate = 16000;
    private const int RecordingLength = 1; // 1 second

    private float[] audioBuffer;
    private int audioBufferIndex = 0;

    public TextProcessorBase textProcessor;

    void OnValidate()
    {
        availableMicrophones.Clear();
        availableMicrophones.AddRange(Microphone.devices);
    }

    void Start()
    {
        InitializeMicrophone();
        InitializeVosk();
        StartRecording();
    }

    void InitializeMicrophone()
    {
        if (availableMicrophones.Count == 0)
        {
            Debug.LogError("No microphones found!");
            return;
        }

        if (string.IsNullOrEmpty(selectedMicrophone))
        {
            selectedMicrophone = availableMicrophones[0];
        }

        Debug.Log($"Using microphone: {selectedMicrophone}");
    }

    void InitializeVosk()
    {
        try
        {
            var modelPath = Path.Combine(Application.streamingAssetsPath, "models", modelName);
            var model = new Model(modelPath);
            recognizer = new VoskRecognizer(model, SampleRate);
            Debug.Log("Vosk recognizer initialized successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Vosk: {e.Message}");
        }
    }

    void StartRecording()
    {
        if (string.IsNullOrEmpty(selectedMicrophone) || recognizer == null) return;

        microphoneClip = Microphone.Start(selectedMicrophone, true, RecordingLength, SampleRate);
        audioBuffer = new float[SampleRate * RecordingLength];
    }

    void Update()
    {
        if (microphoneClip == null || recognizer == null) return;

        int pos = Microphone.GetPosition(selectedMicrophone);
        if (pos < 0 || pos == audioBufferIndex) return;

        // Read new audio data
        int newDataLength = pos - audioBufferIndex;
        if (newDataLength < 0) newDataLength += SampleRate * RecordingLength;

        float[] newData = new float[newDataLength];
        microphoneClip.GetData(newData, audioBufferIndex);

        ProcessAudioChunk(newData);

        // Convert to bytes and process with Vosk
        byte[] audioBytes = ConvertFloatsToBytes(newData);
        bool isFinal = recognizer.AcceptWaveform(audioBytes, audioBytes.Length);

        if (isFinal)
        {
            string result = recognizer.Result();
            ProcessResult(result);
        }
        else
        {
            string partial = recognizer.PartialResult();
            ProcessPartial(partial);
        }

        // Update buffer index
        audioBufferIndex = pos;
    }

    void ProcessAudioChunk(float[] audioChunk)
    {
        float sumSquared = 0f;
        bool wasSpeaking = isSpeaking;
        int speakingCount = 0;

        for (int i = 0; i < audioChunk.Length; i++)
        {
            float sample = Mathf.Abs(audioChunk[i]);
            sumSquared += sample * sample;

            if (sample > silenceThreshold)
            {
                speakingCount++;
                isSpeaking = true;
            }
        }

        currentVolume = Mathf.Sqrt(sumSquared / audioChunk.Length);
        
        if (speakingCount == 0)
        {
            isSpeaking = false;
        }

        // You can add more logic here if needed, e.g., for handling speech start/end
    }

    void ProcessResult(string resultJson)
    {
        var result = JSON.Parse(resultJson);
        string text = result["text"];
        if (!string.IsNullOrEmpty(text))
        {
            lastRecognizedText = text;
            Debug.Log($"Recognized: {text}");
            if (textProcessor != null)
            {
                textProcessor.ProcessText(text);
            }
        }
    }

    void ProcessPartial(string partialJson)
    {
        var partial = JSON.Parse(partialJson);
        string text = partial["partial"];
        if (!string.IsNullOrEmpty(text))
        {
            Debug.Log($"Partial: {text}");
            if (textProcessor != null)
            {
                textProcessor.ProcessPartialText(text);
            }
        }
    }

    byte[] ConvertFloatsToBytes(float[] floats)
    {
        byte[] bytes = new byte[floats.Length * 2];
        for (int i = 0; i < floats.Length; i++)
        {
            short pcm = (short)(floats[i] * 32767f);
            bytes[i * 2] = (byte)(pcm & 0xff);
            bytes[i * 2 + 1] = (byte)((pcm >> 8) & 0xff);
        }
        return bytes;
    }

    void OnDestroy()
    {
        if (recognizer != null)
        {
            recognizer.Dispose();
        }

        if (Microphone.IsRecording(selectedMicrophone))
        {
            Microphone.End(selectedMicrophone);
        }
    }
}