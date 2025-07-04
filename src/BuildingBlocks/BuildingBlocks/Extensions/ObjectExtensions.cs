using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections;
using System.Reflection;
using System.Web;

namespace BuildingBlocks.Extensions;

public static class ObjectExtensions
{
    private static readonly JsonSerializerSettings _camelCaseSerializeSettings = new()
    {
        ContractResolver = new DefaultContractResolver()
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        }
    };
    public static string ToJson(this object obj, bool camelCase = false)
    {
        var settings = camelCase
            ? new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented  // برای زیباسازی JSON
            }
            : new JsonSerializerSettings
            {
                Formatting = Formatting.Indented  // برای زیباسازی JSON حتی بدون camelCase
            };

        return JsonConvert.SerializeObject(obj, settings);
    }
    public static T? JsonToType<T>(this string json) => JsonConvert.DeserializeObject<T>(json);
    public static string ToRowTextBodyFormat<T>(this T model)
    {
        var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var keyValuePairs = new List<string>();

        foreach (var property in properties)
        {
            var value = property.GetValue(model);
            if (value != null)
            {
                string encodedValue = HttpUtility.UrlEncode(value.ToString());
                keyValuePairs.Add($"{property.Name.ToLower()}={encodedValue}");
            }
        }

        return string.Join("&", keyValuePairs);
    }
    public static List<KeyValuePair<string, string?>> ToKeyValuePairs<T>(this T model)
    {
        var result = new List<KeyValuePair<string, string?>>();

        // استفاده از Reflection برای دسترسی به پراپرتی‌های مدل
        foreach (PropertyInfo prop in typeof(T).GetProperties())
        {
            var value = prop.GetValue(model, null);
            result.Add(new KeyValuePair<string, string?>(prop.Name.ToLower(), value?.ToString()));
        }

        return result;
    }
    public static long GenerateUniqueNumber()
    {
        var guid = Guid.NewGuid(); // تولید یک Guid یکتا
        var byteArray = guid.ToByteArray(); // تبدیل Guid به آرایه بایت
        var longValue = BitConverter.ToInt64(byteArray, 0); // تبدیل اولین 8 بایت به عدد long
        return Math.Abs(longValue); // اطمینان از اینکه عدد منفی نیست
    }

    public static DateTime UnixToDateTime(this long unixTime)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
    }

    public static long DateTimeToUnix(this DateTime dateTime)
    {
        return ((DateTimeOffset)dateTime.ToUniversalTime()).ToUnixTimeSeconds();
    }

    #region QueryStringExtensions
    public static string ToQueryString(this object obj)
    {
        if (obj == null)
            return "";

        var queryParams = new List<string>();
        BuildQueryString(obj, string.Empty, queryParams);
        return queryParams.Count == 0 ? "" : "?" + string.Join("&", queryParams);
    }

    public static string ToQueryStringFromString(this string input, string key = "data")
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return $"?{Uri.EscapeDataString(key)}={Uri.EscapeDataString(input)}";
    }


    private static void BuildQueryString(object obj, string prefix, List<string> queryParams)
    {
        if (obj is IDictionary dict)
        {
            foreach (DictionaryEntry item in dict)
            {
                if (item.Value != null)
                    queryParams.Add($"{Uri.EscapeDataString(item.Key.ToString())}={Uri.EscapeDataString(item.Value.ToString())}");
            }

            return;
        }

        if (obj is IEnumerable enumerable && !(obj is string))
        {
            foreach (var item in enumerable)
            {
                if (item != null)
                    queryParams.Add($"{Uri.EscapeDataString(prefix)}={Uri.EscapeDataString(item.ToString())}");
            }

            return;
        }

        var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in props)
        {
            var value = prop.GetValue(obj, null);
            if (value == null) continue;

            var propName = string.IsNullOrWhiteSpace(prefix) ? prop.Name : $"{prefix}.{prop.Name}";

            if (IsSimple(value.GetType()))
            {
                queryParams.Add($"{Uri.EscapeDataString(propName)}={Uri.EscapeDataString(value.ToString())}");
            }
            else
            {
                BuildQueryString(value, propName, queryParams);
            }
        }
    }

    private static bool IsSimple(Type type)
    {
        return
            type.IsPrimitive ||
            new Type[] {
            typeof(string),
            typeof(decimal),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(TimeSpan),
            typeof(Guid),
            typeof(bool),
            typeof(byte[]),
            typeof(Enum)
            }.Contains(type) ||
            Convert.GetTypeCode(type) != TypeCode.Object;
    }
    #endregion
}
