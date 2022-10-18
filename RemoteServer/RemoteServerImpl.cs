using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace RemoteServer
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false,
        InstanceContextMode = InstanceContextMode.Single)]
    internal class RemoteServerImpl : RemoteServerInterface
    {
        private List<string> jobs;

        public RemoteServerImpl()
        {
            jobs = new List<string>();
        }

        public string Download()
        {
            string job = jobs[0];
            jobs.RemoveAt(0);
            return job;
        }

        public void Upload(string job)
        {
            jobs.Add(job);
        }

        public bool JobAvailable()
        {
            if(jobs.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
