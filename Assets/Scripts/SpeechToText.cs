using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Vosk;
using SimpleJSON;

public class ImprovedSpeechToText : MonoBehaviour
{
    [Header("Microphone Settings")]
    public string selectedMicrophone;
    public List<string> availableMicrophones = new List<string>();

    [Header("Vosk Settings")]
    public string modelName = "vosk-model-en-us-0.22";

    [Header("Audio Processing")]
    public float silenceThreshold = 0.02f;
    public float minSpeechDuration = 0.5f;
    public float maxSilenceDuration = 1.0f;

    private VoskRecognizer recognizer;
    private const int SampleRate = 16000;
    private AudioClip microphoneClip;
    private bool isListening = false;

    private List<float> audioBuffer = new List<float>();
    private bool isSpeaking = false;
    private float silenceDuration = 0f;
    private float speechDuration = 0f;

    private Thread recognitionThread;
    private bool isRecognizing = false;
    private object recognizerLock = new object();

    private Queue<float[]> processingQueue = new Queue<float[]>();

    void OnValidate()
    {
        availableMicrophones.Clear();
        availableMicrophones.AddRange(Microphone.devices);
    }

    void Start()
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
        Debug.Log($"Using Vosk model: {modelName}");

        InitializeVosk();
        StartListening();
    }

    void InitializeVosk()
    {
        var modelPath = Path.Combine(Application.streamingAssetsPath, "models", modelName);
        if (!Directory.Exists(modelPath))
        {
            Debug.LogError($"Model directory not found: {modelPath}");
            return;
        }

        try
        {
            var model = new Model(modelPath);
            recognizer = new VoskRecognizer(model, SampleRate);
            Debug.Log("Vosk recognizer initialized successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Vosk model or recognizer: {e.Message}");
            return;
        }

        isRecognizing = true;
        recognitionThread = new Thread(ProcessAudioQueue);
        recognitionThread.Start();
    }

    void StartListening()
    {
        microphoneClip = Microphone.Start(selectedMicrophone, true, 1, SampleRate);

        if (microphoneClip == null)
        {
            Debug.LogError($"Failed to start listening from microphone: {selectedMicrophone}");
            return;
        }

        Debug.Log($"Microphone clip sample rate: {AudioSettings.outputSampleRate}");

        isListening = true;
        StartCoroutine(CaptureAudio());
    }

    IEnumerator CaptureAudio()
    {
        int lastSample = 0;
        float[] tempBuffer = new float[1024];

        while (isListening)
        {
            int currentPos = Microphone.GetPosition(selectedMicrophone);
            if (currentPos < lastSample) currentPos += microphoneClip.samples;

            int sampleDiff = currentPos - lastSample;
            if (sampleDiff > 0)
            {
                if (sampleDiff > tempBuffer.Length)
                {
                    tempBuffer = new float[sampleDiff];
                }

                microphoneClip.GetData(tempBuffer, lastSample % microphoneClip.samples);

                ProcessAudioChunk(tempBuffer, sampleDiff);

                lastSample = currentPos;
            }

            yield return null;
        }
    }

    void ProcessAudioChunk(float[] audioChunk, int sampleCount)
    {
        for (int i = 0; i < sampleCount; i++)
        {
            float sample = Mathf.Abs(audioChunk[i]);

            if (sample > silenceThreshold)
            {
                if (!isSpeaking)
                {
                    isSpeaking = true;
                    silenceDuration = 0f;
                    audioBuffer.Clear();
                }
                speechDuration += 1f / SampleRate;
                silenceDuration = 0f;
            }
            else
            {
                silenceDuration += 1f / SampleRate;
            }

            audioBuffer.Add(audioChunk[i]);

            if (isSpeaking)
            {
                if (silenceDuration >= maxSilenceDuration && speechDuration >= minSpeechDuration)
                {
                    // End of speech detected
                    float[] speechSegment = audioBuffer.ToArray();
                    lock (processingQueue)
                    {
                        processingQueue.Enqueue(speechSegment);
                    }

                    isSpeaking = false;
                    speechDuration = 0f;
                    audioBuffer.Clear();
                }
            }
        }
    }

    void ProcessAudioQueue()
    {
        while (isRecognizing)
        {
            float[] audioChunk = null;

            lock (processingQueue)
            {
                if (processingQueue.Count > 0)
                {
                    audioChunk = processingQueue.Dequeue();
                }
            }

            if (audioChunk != null)
            {
                byte[] audioBytes = ConvertFloatsToBytes(audioChunk);

                if (audioBytes.Length > 0)
                {
                    bool isRecognized;
                    lock (recognizerLock)
                    {
                        isRecognized = recognizer.AcceptWaveform(audioBytes, audioBytes.Length);
                    }

                    if (isRecognized)
                    {
                        string resultJson;
                        lock (recognizerLock)
                        {
                            resultJson = recognizer.Result();
                        }
                        ProcessRecognitionResult(resultJson);
                    }
                }
            }

            Thread.Sleep(10);
        }
    }

    void ProcessRecognitionResult(string resultJson)
    {
        Debug.Log("Vosk Result JSON: " + resultJson);

        var parsedResult = JSON.Parse(resultJson);
        string text = parsedResult["text"];

        if (!string.IsNullOrEmpty(text))
        {
            Debug.Log("Recognized text: " + text);

            if (parsedResult["result"] != null)
            {
                var words = parsedResult["result"].AsArray;
                foreach (JSONNode word in words)
                {
                    string wordText = word["word"];
                    float confidence = word["conf"].AsFloat;
                    Debug.Log($"Word: {wordText}, Confidence: {confidence}");
                }
            }
        }
        else
        {
            Debug.Log("No text detected in this segment.");
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
        isListening = false;
        isRecognizing = false;

        if (recognizer != null)
        {
            lock (recognizerLock)
            {
                recognizer.Dispose();
            }
        }

        if (recognitionThread != null && recognitionThread.IsAlive)
        {
            recognitionThread.Join();
        }
    }
}