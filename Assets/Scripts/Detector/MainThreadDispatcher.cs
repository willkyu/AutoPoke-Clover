using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly ConcurrentQueue<Action> actionQueue = new ConcurrentQueue<Action>();
    private static int mainThreadId;
    private static MainThreadDispatcher instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject("__MainThreadDispatcher");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MainThreadDispatcher>();
        mainThreadId = Thread.CurrentThread.ManagedThreadId;
    }

    public static bool IsMainThread =>
        Thread.CurrentThread.ManagedThreadId == mainThreadId;

    public static void Enqueue(Action action)
    {
        if (action == null) return;
        actionQueue.Enqueue(action);
    }

    private void Update()
    {
        while (actionQueue.TryDequeue(out var action))
        {
            try { action?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    public static T InvokeSync<T>(Func<T> func)
    {
        if (func == null) return default;
        if (IsMainThread) return func();

        T result = default;
        Exception ex = null;
        using (var done = new ManualResetEventSlim(false))
        {
            Enqueue(() =>
            {
                try { result = func(); }
                catch (Exception e) { ex = e; }
                finally { done.Set(); }
            });
            done.Wait();
        }
        if (ex != null) throw ex;
        return result;
    }

    public static void InvokeSync(Action action)
    {
        if (action == null) return;
        if (IsMainThread) { action(); return; }

        Exception ex = null;
        using (var done = new ManualResetEventSlim(false))
        {
            Enqueue(() =>
            {
                try { action(); }
                catch (Exception e) { ex = e; }
                finally { done.Set(); }
            });
            done.Wait();
        }
        if (ex != null) throw ex;
    }
}
