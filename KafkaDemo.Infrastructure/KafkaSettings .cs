using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaDemo.Infrastructure
{
    public class KafkaSettings
    {
        public string BootstrapServers { get; set; } = default!;
        public string GroupId { get; set; } = default!;
        public string Topic { get; set; } = default!;
        public string AutoOffsetReset { get; set; } = default!;
    }
}
