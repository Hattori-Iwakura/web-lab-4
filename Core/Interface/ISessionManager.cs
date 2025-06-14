
namespace web_lab_4.Core.Interface
{
    public interface ISessionManager
    {

        void Set(string key, object value);
        T Get<T>(string key);
    }
}