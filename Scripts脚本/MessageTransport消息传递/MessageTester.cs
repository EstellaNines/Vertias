using UnityEngine;
using GlobalMessaging;

public class MessagingTester : MonoBehaviour
{
    #region 示例消息
    public struct TestMessage : IMessage
    {
        public string Text;
        public int Number;
        public TestMessage(string text, int number)
        {
            Text = text;
            Number = number;
        }
    }
    #endregion

    private void OnEnable()
    {
        // 注册监听
        MessagingCenter.Instance.Register<TestMessage>(OnTestMessage);
    }

    private void OnDisable()
    {
        // 注销监听，避免内存泄漏
        MessagingCenter.Instance.Unregister<TestMessage>(OnTestMessage);
    }

    private void Start()
    {
        // 发送一条测试消息
        MessagingCenter.Instance.Send(new TestMessage("Hello Global Messaging", 42));
    }

    private void OnTestMessage(TestMessage msg)
    {
        Debug.Log($"收到消息：Text={msg.Text}, Number={msg.Number}");
    }
}