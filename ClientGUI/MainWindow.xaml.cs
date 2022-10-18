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

namespace ClientGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //get IP Address and Port Number
            string URL = getURL();

            //Add Client
            addClient(ipadd, portNum);

            //connect to remote server
            ChannelFactory<RemoteServerInterface> foobFactory;
            NetTcpBinding tcp = new NetTcpBinding();
            //Set the URL and create the connection!
            
            foobFactory = new ChannelFactory<RemoteServerInterface>(tcp, URL);
            foob = foobFactory.CreateChannel();
        }

        public string getURL()
        {
            IPAddDialog dialog = new IPAddDialog();
            ipadd = "";
            portNum = "";
            string url = "";
            if (dialog.ShowDialog() == true)
            {
                ipadd = dialog.IPAddress;
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
            client.Id = rand.Next(99999999);
            client.IPAddress = ipAdd;
            client.PortNumber = portNum;
            client.CompletedJobs = 0;


            RestClient restClient = new RestClient("http://localhost:50968/");
            RestRequest restRequest = new RestRequest("api/Clients", Method.Post);
            restRequest.AddJsonBody(JsonConvert.SerializeObject(client));
            RestResponse restResponse = restClient.Post(restRequest);

            return client;
        }

     /*   private string getIPAdd()
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
            txtIP.Text = ipAdd;
            return ipAdd;

        }
*/
        private void btnBrowseFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
                txtInput.Text = File.ReadAllText(openFileDialog.FileName);
        }

        private void btnPost_Click(object sender, RoutedEventArgs e)
        {
            foob.Upload(txtInput.Text);
            MessageBox.Show("Job Uploaded.");
        }

        private void retrieveJob()
        {

        }

        private void getClients()
        {
            RestClient restClient = new RestClient("http://localhost:50968/");
            RestRequest restRequest = new RestRequest("api/Clients", Method.Get);
            RestResponse restResponse = restClient.Post(restRequest);
            List<Client> clients = JsonConvert.DeserializeObject<List<Client>>(restResponse.Content);


        }


        private async void StartServerThread(object sender, RoutedEventArgs e)
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
            host.AddServiceEndpoint(typeof(RemoteServerInterface), tcp, String.Format("net.tcp://{0}:{1}/JobService", ip, portNum));
            host.Open();

            NetTcpBinding tcp = new NetTcpBinding();
            string URL = String.Format("net.tcp://{0}:{1}/JobService", ip, portNum);
            foobFactory = new ChannelFactory<RemoteServerInterface>(tcp, URL);
            foob = foobFactory.CreateChannel();

            Console.ReadLine();
            host.Close();
        }
    }
}
