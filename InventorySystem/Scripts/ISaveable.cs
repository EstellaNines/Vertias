using System;
using UnityEngine;

/// <summary>
/// 可保存接口 - 为需要保存和加载数据的对象提供统一的保存接口
/// 实现此接口的类可以支持数据的序列化、反序列化和状态管理
/// </summary>
public interface ISaveable
{
    /// <summary>
    /// 获取对象的唯一标识ID
    /// </summary>
    /// <returns>对象的唯一ID字符串</returns>
    string GetSaveID();

    /// <summary>
    /// 设置对象的唯一标识ID
    /// </summary>
    /// <param name="id">新的ID字符串</param>
    void SetSaveID(string id);

    /// <summary>
    /// 生成新的唯一标识ID
    /// </summary>
    void GenerateNewSaveID();

    /// <summary>
    /// 验证保存ID是否有效
    /// </summary>
    /// <returns>ID是否有效</returns>
    bool IsSaveIDValid();

    /// <summary>
    /// 序列化对象数据为JSON字符串
    /// </summary>
    /// <returns>序列化后的JSON字符串</returns>
    string SerializeToJson();

    /// <summary>
    /// 从JSON字符串反序列化对象数据
    /// </summary>
    /// <param name="jsonData">JSON数据字符串</param>
    /// <returns>反序列化是否成功</returns>
    bool DeserializeFromJson(string jsonData);

    /// <summary>
    /// 标记对象为已修改状态
    /// </summary>
    void MarkAsModified();

    /// <summary>
    /// 重置修改标记
    /// </summary>
    void ResetModifiedFlag();

    /// <summary>
    /// 检查对象是否已被修改
    /// </summary>
    /// <returns>是否已修改</returns>
    bool IsModified();

    /// <summary>
    /// 验证对象数据的完整性
    /// </summary>
    /// <returns>数据是否有效</returns>
    bool ValidateData();

    /// <summary>
    /// 获取对象的最后修改时间
    /// </summary>
    /// <returns>最后修改时间字符串</returns>
    string GetLastModified();

    /// <summary>
    /// 更新最后修改时间为当前时间
    /// </summary>
    void UpdateLastModified();
}