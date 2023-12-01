using System.Text.Json.Serialization;


namespace Physio.IoT.PulsePersist.Configuration
{
    public class MessageBroker
    {
        [JsonRequired]
        public string Hostname { get; set; } = default!;

        public string? Username { get; set; } = null;

        public string? Password { get; set; } = null;

        [JsonRequired]
        public bool DispatchConsumersAsync { get; set; } = true!;

        public String[] Queues { get; set; } = default!;

    }
}
