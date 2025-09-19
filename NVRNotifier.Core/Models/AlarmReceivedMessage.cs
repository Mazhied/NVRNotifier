using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NVRNotifier.Core.Models
{
    public class AlarmReceivedMessage: IReceivedMessage
    {
        [JsonPropertyName("event")]
        public string Event { get; set; } = string.Empty;
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
        [JsonPropertyName("events")]
        public List<AlarmEvent> Events { get; set; } = new List<AlarmEvent>();
    }

    public class AlarmEvent
    {
        public string EventId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string MonitorId { get; set; } = string.Empty;
        public string Cause { get; set; } = string.Empty;
    }
}
