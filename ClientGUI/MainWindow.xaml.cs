﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using RestSharp;
using Newtonsoft.Json;
using WebServer.Models;
using RemoteServer;
using System.ServiceModel;
using System.Net.Sockets;
using System.Net;
using Microsoft.Scripting.Hosting;
using IronPython.Hosting;
using System.Threading;
using System.Security.Cryptography;

namespace ClientGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string ipadd, portNum;
        private int id;
        private RemoteServerInterface foob;
        private Client client, jobPoster;
        ServiceHost host;

        public MainWindow()
        {
            InitializeComponent();
            this.Closed += new EventHandler(MainWindow_Closed);

            //get IP Address and Port Number
            string URL = GetURL();

            //Add Client
            client = AddClient(ipadd, portNum);

            StartServerThread();
            StartNetworkingThread();
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            host.Close();
            RemoveClient();
        }

        //Open IP Address Dialog and Get Info
        public string GetURL()
        {
            IPAddDialog dialog = new IPAddDialog();
            ipadd = "";
            portNum = "";
            string url;
            if (dialog.ShowDialog() == true)
            {
                ipadd = dialog.IPAddress;
                portNum = dialog.PortNumber;
            }

            url = "net.tcp://" + ipadd + ":" + portNum + "/JobService";

            txtIP.Text = "Local Endpoint: " + url;
            return url;
        }

        private Client AddClient(string ipAdd, string portNum)
        {
            Client client = new Client();
            client.IPAddress = ipAdd;
            client.PortNumber = portNum;
            client.CompletedJobs = 0;

            RestClient restClient = new RestClient("http://localhost:50968/");
            RestRequest restRequest = new RestRequest("api/Clients", Method.Post);
            restRequest.AddJsonBody(client);
            RestResponse restResponse = restClient.Execute(restRequest);
            Client returnClient = JsonConvert.DeserializeObject<Client>(restResponse.Content);
            id = returnClient.Id;

            return returnClient;
        }

        private void RemoveClient()
        {
            RestClient client = new RestClient("http://localhost:50968/");
            RestRequest request = new RestRequest("api/clients/{id}", Method.Delete);
            request.AddUrlSegment("id", id);
            client.Execute(request);
        }

        private void EditClient()
        {
            Client newClient = new Client();
            newClient.Id = client.Id;
            newClient.IPAddress = client.IPAddress;
            newClient.PortNumber = client.PortNumber;
            newClient.CompletedJobs = client.CompletedJobs + 1;

            client = newClient;
            RestClient restClient = new RestClient("http://localhost:50968/");
            RestRequest restRequest = new RestRequest("api/Clients/{id}", Method.Put);
            restRequest.AddUrlSegment("id", id);
            restRequest.AddJsonBody(newClient);
            RestResponse restResponse = restClient.Execute(restRequest);

        }
        private List<Client> GetClients()
        {
            RestClient restClient = new RestClient("http://localhost:50968/");
            RestRequest restRequest = new RestRequest("api/clients", Method.Get);
            RestResponse restResponse = restClient.Execute(restRequest);
            List<Client> clients = JsonConvert.DeserializeObject<List<Client>>(restResponse.Content);

            return clients;
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
      
        private async void StartNetworkingThread()
        {
            var progress = new Progress<bool>(value =>
            {
                string status;
                if (value)
                {
                    status = "Downloading and Executing Job...";
                }
                else
                {
                    status = "Job Successfully Executed.";
                    progBar.Value = 100;
                }
                progBar.IsIndeterminate = value;
                txtStatus.Text = status;
                txtCompletedJob.Text = String.Format("Completed Jobs: {0}", client.CompletedJobs);
            });

            var result = new Progress<string>(value =>
            {
                txtResult.Text = value;
            });


            await Task.Run (()=> InitializeNetwork(progress, result));
        }

        private void InitializeNetwork(IProgress<bool> progress, IProgress <string> result)
        {
            while(true)
            {
                // look for new clients
                List<Client> clients = GetClients();

                //shuffle client list
                Random rand = new Random();
                clients = clients.OrderBy(_ => rand.Next()).ToList();

                //check answer
                if (foob != null)
                {
                    try
                    {
                        List<string> test = foob.GetAnswers();
                        foreach (string answer in test)
                        {
                            result.Report(answer);
                        }
                    }catch (FaultException) { }
                   
                }

                //loop through each client
                foreach (Client client in clients)
                {
                    // for each client that is not itself
                    if (!client.IPAddress.Equals(ipadd) &&
                       !client.PortNumber.Equals(portNum))
                    {
                        // connect to the client's remote server
                        RemoteServerInterface remoteFoob = ConnectToRemoteServer(client.IPAddress, client.PortNumber);

                        try
                        {
                            // check for available jobs
                            if (remoteFoob.JobAvailable())
                            {

                                bool success = false;

                                while (!success)
                                {
                                    progress.Report(true);
                                    Job job = remoteFoob.Download(); // download job
                                    remoteFoob.Remove(job); //remove job after download

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

                                            //sleep for 1 sec
                                            Thread.Sleep(1000);

                                            //execute job and update GUI
                                            string txtResult = ExecuteJob(jobString); // execute job
                                            EditClient();
                                            progress.Report(false);

                                            //post back job and remove job
                                            remoteFoob.PostAnswer(txtResult);
                                            //remoteFoob.Remove(job);

                                        }
                                    }
                                }
                            }
                        }
                        catch (EndpointNotFoundException) { }
                    }

                }

                Thread.Sleep(2000);
            }
        }

        private string ExecuteJob(string job)
        {
            try
            {
                ScriptEngine engine = Python.CreateEngine();
                ScriptScope scope = engine.CreateScope();
                var result = engine.Execute(job, scope);

                return Convert.ToString(result);
            }
            catch (Exception)
            {
                return "Invalid Code";
            }
            
        }

        private RemoteServerInterface ConnectToRemoteServer(string ip, string port)
        {
            RemoteServerInterface remoteFoob;
 
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect(ip, Int32.Parse(port));
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                string localIP = endPoint.Address.ToString();

                NetTcpBinding tcp = new NetTcpBinding();
                string URL = String.Format("net.tcp://{0}:{1}/JobService", localIP, port);

                ChannelFactory<RemoteServerInterface> foobFactory;
                foobFactory = new ChannelFactory<RemoteServerInterface>(tcp, URL);
                remoteFoob = foobFactory.CreateChannel();
            }

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
            NetTcpBinding tcp = new NetTcpBinding();
            RemoteServerImpl jobServer = new RemoteServerImpl();

            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect(ipadd, Int32.Parse(portNum));
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                string localIP = endPoint.Address.ToString();

                host = new ServiceHost(jobServer);
                host.AddServiceEndpoint(typeof(RemoteServerInterface), tcp, String.Format("net.tcp://{0}:{1}/JobService", localIP, portNum));
                host.Open();

                string URL = String.Format("net.tcp://{0}:{1}/JobService", localIP, portNum);

                ChannelFactory<RemoteServerInterface> foobFactory;
                foobFactory = new ChannelFactory<RemoteServerInterface>(tcp, URL);
                foob = foobFactory.CreateChannel();
            }
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
