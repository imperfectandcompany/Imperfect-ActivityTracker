using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ImperfectActivityTracker.Helpers
{
    public static class JsonHelpers
    {
        public static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance
        };

        public static T DeserializeJson<T>(string jsonData)
        {
            T? deserializedJson = default;

            if (!string.IsNullOrEmpty(jsonData))
            {
                try
                {
                    deserializedJson = JsonSerializer.Deserialize<T>(jsonData, _jsonSerializerOptions);
                }
                catch (Exception ex)
                {
                    ImperfectActivityTracker._logger.LogError("An error occurred deserializing the JSON: {message}", ex.Message);
                }
            }
            else
            {

            }


            return deserializedJson;
        }

        public static string SerializeJson(object objecToSerialize)
        {
            string serializedJson = "";

            try
            {
                serializedJson = JsonSerializer.Serialize(objecToSerialize, _jsonSerializerOptions);
            }
            catch (Exception ex)
            {
                ImperfectActivityTracker._logger.LogError("An error occurred serializing the JSON: {message}", ex.Message);
            }

            return serializedJson;
        }
    }

    public class SnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public static SnakeCaseNamingPolicy Instance { get; } = new SnakeCaseNamingPolicy();

        public override string ConvertName(string name)
        {
            return name.ToSnakeCase();
        }
    }
}
