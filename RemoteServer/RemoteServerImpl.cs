using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace RemoteServer
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = true,
        InstanceContextMode = InstanceContextMode.Single)]
    public class RemoteServerImpl : RemoteServerInterface
    {
        private List<Job> jobs;

        public RemoteServerImpl()
        {
            jobs = new List<Job>();
        }

        public Job Download()
        {
            Job job = jobs[0];
            
            return job;
        }

        public void Remove(Job job)
        {
            jobs.RemoveAll(x => x.encodedJob.Equals(job.encodedJob));
        }

        public bool Upload(Job job)
        {
            bool success = false;

            // create hash to verify job
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] hash = sha256Hash.ComputeHash(
                    System.Text.Encoding.UTF8.GetBytes(job.encodedJob));

                if (CompareByteArray(hash, job.hash))
                {
                    jobs.Add(job);
                    success = true;
                }
            }

            return success;
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

        private bool CompareByteArray(byte[] arr1, byte[] arr2)
        {
            if(arr1.Length != arr2.Length)
            {
                return false;
            }

            for(int i = 0; i < arr1.Length; i++)
            {
                if(arr1[i] != arr2[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
