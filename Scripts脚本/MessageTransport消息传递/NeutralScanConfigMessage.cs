using System;
using GlobalMessaging;

namespace GlobalMessaging
{
    // 运行期通过 MCP 动态配置站岗扫视参数；
    // targetInstanceId 为空或 <0 表示广播给全部中立敌人。
    public class NeutralScanConfigMessage : IMessage
    {
        public int? targetInstanceId; // 目标实例；null 或 <0 为全部

        public bool? enableGuardScan; // 是否启用扫视
        public float? scanAngleMin;   // 扫视最小角度（度）
        public float? scanAngleMax;   // 扫视最大角度（度）
        public float? scanSpeedHz;    // 扫视速度（Hz）
    }
}


