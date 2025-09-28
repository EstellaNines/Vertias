using GlobalMessaging;

// 广播：当任意中立敌人被玩家攻击后，通知所有中立敌人进入敌对
namespace GlobalMessaging
{
    public struct NeutralHostileMessage : IMessage
    {
        public int sourceInstanceId; // 触发者实例ID，用于避免重复广播
    }
}


