using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InventorySystem.SaveSystem
{
    /// <summary>
    /// 保存数据序列化器 - 负责ISaveable对象数据的JSON序列化和反序列化
    /// 提供统一的数据转换接口，支持复杂对象结构和类型安全的序列化
    /// </summary>
    public class SaveDataSerializer : MonoBehaviour
    {
        #region 字段和属性
        [Header("序列化配置")]
        [SerializeField] private bool prettyPrint = true; // 是否格式化JSON输出
        [SerializeField] private bool enableTypeHandling = true; // 是否启用类型处理
        [SerializeField] private bool enableLogging = true; // 是否启用日志记录

        // JSON序列化设置
        private JsonSerializerSettings jsonSettings;

        // 自定义类型转换器字典
        private Dictionary<Type, JsonConverter> customConverters = new Dictionary<Type, JsonConverter>();

        // 序列化统计信息
        private int serializationCount = 0;
        private int deserializationCount = 0;
        private long totalSerializedBytes = 0;
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化序列化器
        /// </summary>
        public void Initialize()
        {
            SetupJsonSettings();
            RegisterCustomConverters();
            LogMessage("SaveDataSerializer已初始化");
        }

        /// <summary>
        /// 设置JSON序列化配置
        /// </summary>
        private void SetupJsonSettings()
        {
            jsonSettings = new JsonSerializerSettings
            {
                // 基础设置
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
                NullValueHandling = NullValueHandling.Include,
                DefaultValueHandling = DefaultValueHandling.Include,

                // 类型处理
                TypeNameHandling = enableTypeHandling ? TypeNameHandling.Auto : TypeNameHandling.None,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,

                // 错误处理
                Error = HandleSerializationError,

                // 日期格式
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,

                // 引用处理
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            };
        }

        /// <summary>
        /// 注册自定义类型转换器
        /// </summary>
        private void RegisterCustomConverters()
        {
            // Vector3转换器
            var vector3Converter = new Vector3Converter();
            customConverters[typeof(Vector3)] = vector3Converter;
            jsonSettings.Converters.Add(vector3Converter);

            // Quaternion转换器
            var quaternionConverter = new QuaternionConverter();
            customConverters[typeof(Quaternion)] = quaternionConverter;
            jsonSettings.Converters.Add(quaternionConverter);

            // Color转换器
            var colorConverter = new ColorConverter();
            customConverters[typeof(Color)] = colorConverter;
            jsonSettings.Converters.Add(colorConverter);

            // GameObject引用转换器
            var gameObjectConverter = new GameObjectReferenceConverter();
            customConverters[typeof(GameObject)] = gameObjectConverter;
            jsonSettings.Converters.Add(gameObjectConverter);

            LogMessage($"已注册{customConverters.Count}个自定义转换器");
        }
        #endregion

        #region 序列化方法
        /// <summary>
        /// 将对象序列化为JSON字符串
        /// </summary>
        /// <param name="obj">要序列化的对象</param>
        /// <returns>JSON字符串</returns>
        public string SerializeToJson(object obj)
        {
            if (obj == null)
            {
                LogWarning("尝试序列化空对象");
                return "null";
            }

            try
            {
                string json = JsonConvert.SerializeObject(obj, jsonSettings);

                // 更新统计信息
                serializationCount++;
                totalSerializedBytes += System.Text.Encoding.UTF8.GetByteCount(json);

                LogMessage($"序列化成功: {obj.GetType().Name}, 大小: {json.Length}字符");
                return json;
            }
            catch (Exception ex)
            {
                LogError($"序列化失败: {obj.GetType().Name}, 错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 将ISaveable对象的保存数据序列化为JSON
        /// </summary>
        /// <param name="saveable">ISaveable对象</param>
        /// <returns>JSON字符串</returns>
        public string SerializeSaveableData(ISaveable saveable)
        {
            if (saveable == null)
            {
                LogWarning("尝试序列化空的ISaveable对象");
                return null;
            }

            try
            {
                // 获取保存数据 - 使用SerializeToJson方法
                var saveDataJson = saveable.SerializeToJson();

                // 创建包装对象，包含类型信息
                var wrappedData = new SaveableDataWrapper
                {
                    saveId = saveable.GetSaveID(),
                    objectType = saveable.GetType().AssemblyQualifiedName,
                    saveData = saveDataJson,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return SerializeToJson(wrappedData);
            }
            catch (Exception ex)
            {
                LogError($"序列化ISaveable对象失败: {saveable.GetSaveID()}, 错误: {ex.Message}");
                return null;
            }
        }
        #endregion

        #region 反序列化方法
        /// <summary>
        /// 从JSON字符串反序列化对象
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="json">JSON字符串</param>
        /// <returns>反序列化的对象</returns>
        public T DeserializeFromJson<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                LogWarning("尝试反序列化空的JSON字符串");
                return default(T);
            }

            try
            {
                T result = JsonConvert.DeserializeObject<T>(json, jsonSettings);

                // 更新统计信息
                deserializationCount++;

                LogMessage($"反序列化成功: {typeof(T).Name}");
                return result;
            }
            catch (Exception ex)
            {
                LogError($"反序列化失败: {typeof(T).Name}, 错误: {ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// 从JSON字符串反序列化为指定类型
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <param name="targetType">目标类型</param>
        /// <returns>反序列化的对象</returns>
        public object DeserializeFromJson(string json, Type targetType)
        {
            if (string.IsNullOrEmpty(json) || targetType == null)
            {
                LogWarning("反序列化参数无效");
                return null;
            }

            try
            {
                object result = JsonConvert.DeserializeObject(json, targetType, jsonSettings);

                // 更新统计信息
                deserializationCount++;

                LogMessage($"反序列化成功: {targetType.Name}");
                return result;
            }
            catch (Exception ex)
            {
                LogError($"反序列化失败: {targetType.Name}, 错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 反序列化ISaveable对象的保存数据
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <returns>SaveableDataWrapper对象</returns>
        public SaveableDataWrapper DeserializeSaveableData(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                LogWarning("尝试反序列化空的ISaveable数据");
                return null;
            }

            try
            {
                var wrapper = DeserializeFromJson<SaveableDataWrapper>(json);
                if (wrapper != null)
                {
                    LogMessage($"反序列化ISaveable数据成功: {wrapper.saveId}");
                }
                return wrapper;
            }
            catch (Exception ex)
            {
                LogError($"反序列化ISaveable数据失败: {ex.Message}");
                return null;
            }
        }
        #endregion

        #region 验证和实用方法
        /// <summary>
        /// 验证JSON字符串格式
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <returns>是否为有效的JSON格式</returns>
        public bool ValidateJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            try
            {
                JToken.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取JSON字符串的大小（字节）
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <returns>字节大小</returns>
        public long GetJsonSize(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return 0;
            }

            return System.Text.Encoding.UTF8.GetByteCount(json);
        }

        /// <summary>
        /// 压缩JSON字符串（移除格式化）
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <returns>压缩后的JSON字符串</returns>
        public string CompressJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return json;
            }

            try
            {
                var obj = JsonConvert.DeserializeObject(json);
                var compressedSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.None,
                    NullValueHandling = NullValueHandling.Ignore
                };
                return JsonConvert.SerializeObject(obj, compressedSettings);
            }
            catch (Exception ex)
            {
                LogError($"JSON压缩失败: {ex.Message}");
                return json;
            }
        }

        /// <summary>
        /// 获取序列化器统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public string GetStatistics()
        {
            return $"序列化次数: {serializationCount}, " +
                   $"反序列化次数: {deserializationCount}, " +
                   $"总序列化字节数: {totalSerializedBytes}, " +
                   $"自定义转换器数: {customConverters.Count}";
        }

        /// <summary>
        /// 重置统计信息
        /// </summary>
        public void ResetStatistics()
        {
            serializationCount = 0;
            deserializationCount = 0;
            totalSerializedBytes = 0;
            LogMessage("统计信息已重置");
        }
        #endregion

        #region 错误处理
        /// <summary>
        /// 处理序列化错误
        /// </summary>
        private void HandleSerializationError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs e)
        {
            LogError($"序列化错误: {e.ErrorContext.Error.Message}, 路径: {e.ErrorContext.Path}");

            // 标记错误已处理，继续序列化其他部分
            e.ErrorContext.Handled = true;
        }
        #endregion

        #region 日志方法
        /// <summary>
        /// 记录日志消息
        /// </summary>
        private void LogMessage(string message)
        {
            if (enableLogging)
            {
                Debug.Log($"[SaveDataSerializer] {message}");
            }
        }

        /// <summary>
        /// 记录警告消息
        /// </summary>
        private void LogWarning(string message)
        {
            if (enableLogging)
            {
                Debug.LogWarning($"[SaveDataSerializer] {message}");
            }
        }

        /// <summary>
        /// 记录错误消息
        /// </summary>
        private void LogError(string message)
        {
            if (enableLogging)
            {
                Debug.LogError($"[SaveDataSerializer] {message}");
            }
        }
        #endregion
    }

    #region 数据包装类
    /// <summary>
    /// ISaveable对象数据包装器
    /// </summary>
    [Serializable]
    public class SaveableDataWrapper
    {
        public string saveId;           // 保存ID
        public string objectType;       // 对象类型
        public object saveData;         // 保存数据
        public string timestamp;        // 时间戳
    }
    #endregion

    #region 自定义JSON转换器
    /// <summary>
    /// Vector3 JSON转换器
    /// </summary>
    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WriteEndObject();
        }

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            return new Vector3(
                obj["x"]?.Value<float>() ?? 0f,
                obj["y"]?.Value<float>() ?? 0f,
                obj["z"]?.Value<float>() ?? 0f
            );
        }
    }

    /// <summary>
    /// Quaternion JSON转换器
    /// </summary>
    public class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WritePropertyName("w");
            writer.WriteValue(value.w);
            writer.WriteEndObject();
        }

        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            return new Quaternion(
                obj["x"]?.Value<float>() ?? 0f,
                obj["y"]?.Value<float>() ?? 0f,
                obj["z"]?.Value<float>() ?? 0f,
                obj["w"]?.Value<float>() ?? 1f
            );
        }
    }

    /// <summary>
    /// Color JSON转换器
    /// </summary>
    public class ColorConverter : JsonConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("r");
            writer.WriteValue(value.r);
            writer.WritePropertyName("g");
            writer.WriteValue(value.g);
            writer.WritePropertyName("b");
            writer.WriteValue(value.b);
            writer.WritePropertyName("a");
            writer.WriteValue(value.a);
            writer.WriteEndObject();
        }

        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            return new Color(
                obj["r"]?.Value<float>() ?? 0f,
                obj["g"]?.Value<float>() ?? 0f,
                obj["b"]?.Value<float>() ?? 0f,
                obj["a"]?.Value<float>() ?? 1f
            );
        }
    }

    /// <summary>
    /// GameObject引用JSON转换器
    /// </summary>
    public class GameObjectReferenceConverter : JsonConverter<GameObject>
    {
        public override void WriteJson(JsonWriter writer, GameObject value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("name");
            writer.WriteValue(value.name);
            writer.WritePropertyName("instanceId");
            writer.WriteValue(value.GetInstanceID());
            writer.WritePropertyName("tag");
            writer.WriteValue(value.tag);
            writer.WriteEndObject();
        }

        public override GameObject ReadJson(JsonReader reader, Type objectType, GameObject existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var obj = JObject.Load(reader);
            string name = obj["name"]?.Value<string>();
            int instanceId = obj["instanceId"]?.Value<int>() ?? 0;
            string tag = obj["tag"]?.Value<string>();

            // 尝试通过实例ID查找对象
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            GameObject foundObject = null;
            foreach (var go in allObjects)
            {
                if (go.GetInstanceID() == instanceId)
                {
                    foundObject = go;
                    break;
                }
            }

            if (foundObject != null)
            {
                return foundObject;
            }

            // 如果通过实例ID找不到，尝试通过名称和标签查找
            if (!string.IsNullOrEmpty(name))
            {
                var allGameObjects = GameObject.FindObjectsOfType<GameObject>();
                foreach (var go in allGameObjects)
                {
                    if (go.name == name)
                    {
                        if (string.IsNullOrEmpty(tag) || go.tag == tag)
                        {
                            return go;
                        }
                    }
                }
            }

            return null;
        }
    }
    #endregion
}