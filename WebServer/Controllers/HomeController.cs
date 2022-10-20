using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using RestSharp;
using WebServer.Models;
using System.ServiceModel;
using RemoteServer;
using System.Timers;

namespace WebServer.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";

            // remove dead clients every 10 seconds
            Timer timer = new Timer(TimeSpan.FromSeconds(10).TotalMilliseconds);
            timer.AutoReset = true;
            timer.Elapsed += new ElapsedEventHandler(ClearDeadClients);
            timer.Start();

            return View();
        }

        private void ClearDeadClients(object sender, ElapsedEventArgs e)
        {
            RestClient rClient = new RestClient("http://localhost:50968/");
            RestRequest getRequest = new RestRequest("api/clients", Method.Get);
            RestResponse getResponse = rClient.Execute(getRequest);
            List<Client> clients = JsonConvert.DeserializeObject<List<Client>>(getResponse.Content);

            foreach(Client client in clients)
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect(client.IPAddress, Int32.Parse(client.PortNumber));
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    string localIP = endPoint.Address.ToString();

                    
                    {
                        NetTcpBinding tcp = new NetTcpBinding();
                        string URL = String.Format("net.tcp://{0}:{1}/JobService", localIP, client.PortNumber);

                        ChannelFactory<RemoteServerInterface> foobFactory;
                        foobFactory = new ChannelFactory<RemoteServerInterface>(tcp, URL);
                        RemoteServerInterface remoteFoob = foobFactory.CreateChannel();
                        try
                        {
                            remoteFoob.JobAvailable();
                        }
                        catch(EndpointNotFoundException)
                        {
                            RestRequest request = new RestRequest("api/clients/{id}", Method.Delete);
                            request.AddUrlSegment("id", client.Id);
                            rClient.Execute(request);
                        }
                        
                    }
                }
            }
        }
    }
}
