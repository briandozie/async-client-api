using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RestSharp;
using Newtonsoft.Json;
using IronPython;
using WebServer.Models;
using RemoteServer;
using System.ServiceModel;
using System.Net.Sockets;
using System.Net;
using System.Security.Policy;
using Microsoft.Scripting.Hosting;
using IronPython.Hosting;
using System.Threading;
using System.Buffers.Text;
using System.Security.Cryptography;
using static Community.CsharpSqlite.Sqlite3;

namespace ClientGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string ipadd, portNum;
        private RemoteServerInterface foob;

        public MainWindow()
        {
            InitializeComponent();

            //get IP Address and Port Number
            string URL = getURL();

            //Add Client
            addClient(ipadd, portNum);

            StartServerThread();
            StartNetworkingThread();
        }

        public string getURL()
        {
            IPAddDialog dialog = new IPAddDialog();
            ipadd = "";
            portNum = "";
            string url = "";
            if (dialog.ShowDialog() == true)
            {
                //ipadd = dialog.IPAddress;
                ipadd = getIPAdd();
                portNum = dialog.PortNumber;
            }

            url = "net.tcp://" + ipadd + ":" + portNum + "/JobService";

            txtIP.Text = url;
            return url;
        }

        private Client addClient(string ipAdd, string portNum)
        {
            Random rand = new Random();
            Client client = new Client();
            //client.Id = rand.Next(99999999);
            client.IPAddress = ipAdd;
            client.PortNumber = portNum;
            client.CompletedJobs = 0;


            RestClient restClient = new RestClient("http://localhost:50968/");
            RestRequest restRequest = new RestRequest("api/Clients", Method.Post);
            restRequest.AddJsonBody(client);
            RestResponse restResponse = restClient.Execute(restRequest);

            return client;
        }

        private string getIPAdd()
        {
            IPAddress[] hostAddresses = Dns.GetHostAddresses("");
            string ipAdd ="";
            foreach (IPAddress hostAddress in hostAddresses)
            {
                if (hostAddress.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(hostAddress) &&  // ignore loopback addresses
                    !hostAddress.ToString().StartsWith("169.254."))  // ignore link-local addresses
                    ipAdd = hostAddress.ToString();
            }
            //txtIP.Text = ipAdd;
            return ipAdd;

        }

        private void btnBrowseFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
                txtInput.Text = File.ReadAllText(openFileDialog.FileName);
        }

        private void btnPost_Click(object sender, RoutedEventArgs e)
        {
            string jobString = txtInput.Text;

            // encode the job
            byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(jobString);
            string encodedJob =  Convert.ToBase64String(textBytes);

            Job job = new Job();
            job.encodedJob = encodedJob;
            
            // create a hash
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] hash = sha256Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(encodedJob));
                job.hash = hash;
            }

            // upload job
            while (!foob.Upload(job));
            MessageBox.Show("Job Uploaded.");
        }

        private void retrieveJob()
        {
        
        }

        private List<Client> getClients()
        {
            RestClient restClient = new RestClient("http://localhost:50968/");
            RestRequest restRequest = new RestRequest("api/clients", Method.Get);
            RestResponse restResponse = restClient.Execute(restRequest);
            List<Client> clients = JsonConvert.DeserializeObject<List<Client>>(restResponse.Content);

            return clients;
        }

        private async void StartNetworkingThread()
        {
            Task task = new Task(InitializeNetwork);
            task.Start();
            await task;
        }

        private void InitializeNetwork()
        {
            while(true)
            {
                // look for new clients
                List<Client> clients = getClients();

                foreach(Client client in clients)
                {
                    // for each client that is not itself
                    /*if(!client.IPAddress.Equals(ipadd) &&
                         !client.PortNumber.Equals(portNum))*/
                    if(!client.PortNumber.Equals(portNum))
                    {
                        // connect to the client's remote server
                        RemoteServerInterface remoteFoob = connectToRemoteServer(client.IPAddress, client.PortNumber);
                        
                        // check for available jobs
                        if(remoteFoob.JobAvailable())
                        {
                            bool success = false;

                            while(!success)
                            {
                                Job job = remoteFoob.Download(); // download job

                                using (SHA256 sha256Hash = SHA256.Create())
                                {
                                    byte[] hash = sha256Hash.ComputeHash(
                                        System.Text.Encoding.UTF8.GetBytes(job.encodedJob));

                                    if (CompareByteArray(hash, job.hash))
                                    {
                                        success = true;

                                        // decode job
                                        byte[] encodedBytes = Convert.FromBase64String(job.encodedJob);
                                        string jobString = System.Text.Encoding.UTF8.GetString(encodedBytes);

                                        ExecuteJob(jobString); // execute job
                                    }
                                }
                            }
                        }
                    }
                }

                Thread.Sleep(2000);
            }
        }

        private string ExecuteJob(string job)
        {
            ScriptEngine engine = Python.CreateEngine();
            ScriptScope scope = engine.CreateScope();
            var result = engine.Execute(job, scope);
            // TODO: post answer back to client ? idk

            /*
            // TODO: still need to test if this works
            using(var reader = new StringReader(job))
            {
                // read first line of job string
                string line = reader.ReadLine();
                
                // get function name
                int from = job.IndexOf("def") + "def".Length;
                int to = job.LastIndexOf("(");

                string funcName = job.Substring(from, to - from);
                return funcName;
            }*/

            return null;
        }

        private RemoteServerInterface connectToRemoteServer(string ip, string port)
        {
            NetTcpBinding tcp = new NetTcpBinding();
            string URL = String.Format("net.tcp://{0}:{1}/JobService", ip, port);

            ChannelFactory<RemoteServerInterface> foobFactory;
            foobFactory = new ChannelFactory<RemoteServerInterface>(tcp, URL);
            RemoteServerInterface remoteFoob = foobFactory.CreateChannel();

            return remoteFoob;
        }


        private async void StartServerThread()
        {
            Task task = new Task(InitializeServer);
            task.Start();
            await task;
        }

        private void InitializeServer()
        {
            ServiceHost host;
            NetTcpBinding tcp = new NetTcpBinding();
            RemoteServerImpl jobServer = new RemoteServerImpl();

            host = new ServiceHost(jobServer);
            //host.AddServiceEndpoint(typeof(RemoteServerInterface), tcp, String.Format("net.tcp://{0}:{1}/JobService", ipadd, portNum));
            host.AddServiceEndpoint(typeof(RemoteServerInterface), tcp, String.Format("net.tcp://{0}:{1}/JobService", ipadd, portNum));
            host.Open();

            //string URL = String.Format("net.tcp://{0}:{1}/JobService", ipadd, portNum);
            string URL = String.Format("net.tcp://{0}:{1}/JobService", ipadd, portNum);

            ChannelFactory<RemoteServerInterface> foobFactory;
            foobFactory = new ChannelFactory<RemoteServerInterface>(tcp, URL);
            foob = foobFactory.CreateChannel();

            //Console.ReadLine();
            //host.Close();
        }

        private bool CompareByteArray(byte[] arr1, byte[] arr2)
        {
            if (arr1.Length != arr2.Length)
            {
                return false;
            }

            for (int i = 0; i < arr1.Length; i++)
            {
                if (arr1[i] != arr2[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
