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

namespace ClientGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RemoteServerInterface foob;
        public MainWindow()
        {
            InitializeComponent();

            //connect to remote server
            ChannelFactory<RemoteServerInterface> foobFactory;
            NetTcpBinding tcp = new NetTcpBinding();
            //Set the URL and create the connection!
            string URL = "net.tcp://localhost:8100/JobService";
            foobFactory = new ChannelFactory<RemoteServerInterface>(tcp, URL);
            foob = foobFactory.CreateChannel();

            addClient();
        }

        private void addClient()
        {
            Random rand = new Random();
            Client client = new Client();
            client.Id = rand.Next(99999999);
            client.IPAddress = getIPAdd();
            client.PortNumber = "8200";
            client.CompletedJobs = 0;


            RestClient restClient = new RestClient("http://localhost:50968/");
            RestRequest restRequest = new RestRequest("api/Clients", Method.Post);
            restRequest.AddJsonBody(JsonConvert.SerializeObject(client));
            RestResponse restResponse = restClient.Post(restRequest);
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
            txtIP.Text = ipAdd;
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
            foob.Upload(txtInput.Text);
            MessageBox.Show("Job Uploaded.");
        }

        private void getClients()
        {
            RestClient restClient = new RestClient("http://localhost:50968/");
            RestRequest restRequest = new RestRequest("api/Clients", Method.Get);
            RestResponse restResponse = restClient.Post(restRequest);
            List<Client> clients = JsonConvert.DeserializeObject<List<Client>>(restResponse.Content);
        }
       
    }
}
