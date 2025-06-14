
using System.Text.Json;
using web_lab_4.Core.Interface;

namespace web_lab_4.Core.Manager
{
    class SessionManager : ISessionManager
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SessionManager(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Set(string key, object value)
        {
            _httpContextAccessor.HttpContext!.Session.SetString(key, JsonSerializer.Serialize(value));
        }

        public T Get<T>(string key)
        {
            var value = _httpContextAccessor.HttpContext!.Session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}