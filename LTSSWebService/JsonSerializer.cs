using Newtonsoft.Json;

namespace LTSSWebService
{
    public static class JsonSerializer
    {
        public static JsonSerializerSettings Settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
    }
}