using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteServer
{
    public class Job
    {
        public string encodedJob { get; set; }
        public byte[] hash { get; set; }
    }
}
