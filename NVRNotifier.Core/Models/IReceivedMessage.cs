using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NVRNotifier.Core.Models
{
    public interface IReceivedMessage
    {
        public string Event { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
    }
}
