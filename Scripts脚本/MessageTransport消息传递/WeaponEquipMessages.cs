using GlobalMessaging;
using InventorySystem;
using UnityEngine;

// 姝﹀櫒瑁呭/杈撳叆 鎸囦护涓庣粨鏋滀簨浠剁殑娑堟伅瀹氫箟
// 璇存槑锛氭墍鏈夋秷鎭疄鐜� IMessage锛屼互渚块€氳繃 GlobalMessaging.MessagingCenter 鍒嗗彂

public static class WeaponMessageBus
{
    // 鎸囦护锛氬皢姝﹀櫒瑁呭鍒版寚瀹氭Ы浣嶏紙0=涓绘鍣紝1=鍓鍣級
    public struct EquipWeaponCommand : IMessage
    {
        public int slotIndex;           // 0/1
        public ItemDataSO itemSO;       // 蹇呴』涓� Weapon 绫荤洰锛屼笖 weapon 鑺傜偣鏈� Resources 鍦板潃
    }

    // 鎸囦护锛氫粠鎸囧畾妲戒綅鍗歌浇姝﹀櫒
    public struct UnequipWeaponCommand : IMessage
    {
        public int slotIndex;           // 0/1
    }

    // 鎸囦护锛氭樉寮忚缃綋鍓嶆縺娲绘Ы浣�
    public struct SetActiveWeaponSlot : IMessage
    {
        public int slotIndex;           // 0/1
    }

    // 鎸囦护锛氭粴杞垏鎹富/鍓鍣紙delta=+1/-1锛�
    public struct ScrollSwitchWeapon : IMessage
    {
        public int delta;               // +1/-1锛堝叾浠栧€兼寜绗﹀彿鍙栨暣锛�
    }

    // 鎸囦护锛氬皠鍑昏緭鍏ワ紙true=鎸変笅锛宖alse=鏉惧紑锛�
    public struct SetFiringInput : IMessage
    {
        public bool isPressed;
    }

    // 鎸囦护锛氶噸鏂拌寮�
    public struct ReloadCommand : IMessage { }

    // 缁撴灉锛氳澶囨垚鍔�
    public struct WeaponEquippedEvent : IMessage
    {
        public int slotIndex;
        public GameObject instance;
        public string weaponName;
    }

    // 缁撴灉锛氬嵏杞藉畬鎴�
    public struct WeaponUnequippedEvent : IMessage
    {
        public int slotIndex;
    }

    // 缁撴灉锛氬垏鎹㈠畬鎴�
    public struct WeaponSwitchedEvent : IMessage
    {
        public int from;
        public int to;
    }

    // 缁撴灉锛氳澶囧け璐ワ紙璧勬簮缂哄け/绫诲埆涓嶇绛夛級
    public struct WeaponEquipFailed : IMessage
    {
        public int slotIndex;
        public string reason;
    }
}


