// 文件名：MessagingCenter.cs
using System;
using System.Collections.Generic;

namespace GlobalMessaging
{
    // 全局消息收发中
    public sealed class MessagingCenter
    {
        #region 单例
        private static readonly Lazy<MessagingCenter> _instance =
            new Lazy<MessagingCenter>(() => new MessagingCenter());
        public static MessagingCenter Instance => _instance.Value;
        private MessagingCenter() { }
        #endregion

        private readonly Dictionary<Type, List<Delegate>> _handlers = new();

        // 订阅消息
        public void Register<T>(Action<T> handler) where T : IMessage
        {
            var key = typeof(T);
            if (!_handlers.TryGetValue(key, out var list))
            {
                list = new List<Delegate>();
                _handlers[key] = list;
            }
            list.Add(handler);
        }

        // 取消订阅
        public void Unregister<T>(Action<T> handler) where T : IMessage
        {
            var key = typeof(T);
            if (_handlers.TryGetValue(key, out var list))
                list.Remove(handler);
        }

        // 发送消息
        public void Send<T>(T message) where T : IMessage
        {
            var key = typeof(T);
            if (_handlers.TryGetValue(key, out var list))
            {
                // ToArray 防止回调里注销导致迭代异常
                foreach (Action<T> handler in list.ToArray())
                    handler.Invoke(message);
            }
        }

        // 场景卸载时调用，防止内存泄漏。
        public void Clear()
        {
            _handlers.Clear();
        }
    }
}