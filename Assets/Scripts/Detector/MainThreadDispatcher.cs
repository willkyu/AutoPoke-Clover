using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly ConcurrentQueue<Action> actionQueue = new ConcurrentQueue<Action>();
    private static readonly ManualResetEvent hasNewItem = new ManualResetEvent(false);

    public static void Enqueue(Action action)
    {
        actionQueue.Enqueue(action);
        hasNewItem.Set(); // 通知等待线程有新任务
    }

    void Update()
    {
        // Debug.Log("MainThreadDispatcher active");
        while (actionQueue.TryDequeue(out var action))
        {
            action?.Invoke();
        }
        hasNewItem.Reset(); // 等下一帧
    }

    public static T InvokeSync<T>(Func<T> func)
    {
        T result = default;
        Exception ex = null;
        ManualResetEvent done = new ManualResetEvent(false);

        Enqueue(() =>
        {
            try { result = func(); }
            catch (Exception e) { ex = e; }
            finally { done.Set(); }
        });

        done.WaitOne();
        if (ex != null)
            throw ex;

        return result;
    }
}
