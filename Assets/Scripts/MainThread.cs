using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;

public class MainThread : MonoBehaviour
{
    private static MainThread instance;
    private static int mainThreadId;
    private Queue<Action> executionQueue = new Queue<Action>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            mainThreadId = Thread.CurrentThread.ManagedThreadId;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        while (executionQueue.Count > 0)
        {
            executionQueue.Dequeue().Invoke();
        }
    }

    public static void Execute(Action action)
    {
        if (instance != null)
        {
            if (Thread.CurrentThread.ManagedThreadId == mainThreadId)
            {
                action();
            }
            else
            {
                instance.executionQueue.Enqueue(action);
            }
        }
    }
}