using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 便于触发事件的扩展类
/// </summary>
public static class EventTriggerExt
{
    /// <summary>
    /// 触发事件（无参数）
    /// </summary>
    /// <param name="sender">触发源</param>
    /// <param name="eventName">事件名</param>
    public static void TriggerEvent(this object sender, string eventName)
    {
        EventManager.I.TriggerEvent(eventName, sender);
    }
    /// <summary>
    /// 触发事件（有参数）
    /// </summary>
    /// <param name="sender">触发源</param>
    /// <param name="eventName">事件名</param>
    /// <param name="args">事件参数</param>
    public static void TriggerEvent(this object sender, string eventName, EventArgs args)
    {
        EventManager.I.TriggerEvent(eventName, sender, args);
    }

}
/// <summary>
/// 事件管理器
/// </summary>
public class EventManager : SingletonBase<EventManager>
{
    private Dictionary<string, EventHandler> handlerDic = new Dictionary<string, EventHandler>();

    /// <summary>
    /// 添加一个事件的监听者
    /// </summary>
    /// <param name="eventName">事件名</param>
    /// <param name="handler">事件处理函数</param>
    public void AddListener(string eventName, EventHandler handler)
    {
        if (handlerDic.ContainsKey(eventName))
            handlerDic[eventName] += handler;
        else
            handlerDic.Add(eventName, handler);
    }
    /// <summary>
    /// 移除一个事件的监听者
    /// </summary>
    /// <param name="eventName">事件名</param>
    /// <param name="handler">事件处理函数</param>
    public void RemoveListener(string eventName, EventHandler handler)
    {
        if (handlerDic.ContainsKey(eventName))
            handlerDic[eventName] -= handler;
    }
    /// <summary>
    /// 触发事件（无参数）
    /// </summary>
    /// <param name="eventName">事件名</param>
    /// <param name="sender">触发源</param>
    public void TriggerEvent(string eventName, object sender)
    {
        if (handlerDic.ContainsKey(eventName))
            handlerDic[eventName]?.Invoke(sender, EventArgs.Empty);
    }
    /// <summary>
    /// 触发事件（有参数）
    /// </summary>
    /// <param name="eventName">事件名</param>
    /// <param name="sender">触发源</param>
    /// <param name="args">事件参数</param>
    public void TriggerEvent(string eventName, object sender, EventArgs args)
    {
        if (handlerDic.ContainsKey(eventName))
            handlerDic[eventName]?.Invoke(sender, args);
    }
    /// <summary>
    /// 清空所有事件
    /// </summary>
    public void Clear()
    {
        handlerDic.Clear();
    }
}
