using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace RemoteServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Remote Job Server");

            ServiceHost host;
            NetTcpBinding tcp = new NetTcpBinding();
            RemoteServerImpl jobServer = new RemoteServerImpl();

            host = new ServiceHost(jobServer);
            host.AddServiceEndpoint(typeof(RemoteServerInterface), tcp, "net.tcp://0.0.0.0:8100/JobService");
            host.Open();

            Console.WriteLine("System Online");
            Console.ReadLine();

            host.Close();
        }
    }
}
