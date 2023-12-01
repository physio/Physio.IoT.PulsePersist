using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Physio.IoT.PulsePersist.Configuration
{
    public class AppConfig
    {
        [JsonRequired]
        public MessageBroker MessageBroker { get; set; }= new();
        public Dictionary<string, LogLevel> Logging { get; set; } = new();
        public string? SqliteConnectionString { get; set; } = null;
  
    }
}
