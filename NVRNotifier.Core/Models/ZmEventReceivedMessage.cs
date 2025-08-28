using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NVRNotifier.Core.Models
{
    public class ZmEventReceivedMessage
    {
        public string Event { get; set; } = string.Empty;
        public ZmMonitor Monitor { get; set; }
        public ZmEventData EventData { get; set; }
    }

    public class ZmMonitor
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class ZmEventData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Cause { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
    }
}
