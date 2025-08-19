using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineRunner : MonoBehaviour
{
    private static readonly Queue<IEnumerator> queue = new();
    private static CoroutineRunner _instance;

    public static CoroutineRunner Instance
    {
        get
        {
            if (_instance != null) return _instance;
            var go = new GameObject("__CoroutineRunner");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<CoroutineRunner>();
            return _instance;
        }
    }

    public static void RunSafe(IEnumerator routine)
    {
        lock (queue) queue.Enqueue(routine);
    }

    void Update()
    {
        lock (queue)
        {
            while (queue.Count > 0)
            {
                StartCoroutine(queue.Dequeue());
            }
        }
    }
}
