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
        }

        private void btnBrowseFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
                txtInput.Text = File.ReadAllText(openFileDialog.FileName);
        }

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {

        }

        private void getClients()
        {
            RestClient restClient = new RestClient("http://localhost:50968/");
            RestRequest restRequest = new RestRequest("api/Clients");
            RestResponse restResponse = restClient.Post(restRequest);
            List<Client> clients = JsonConvert.DeserializeObject<List<Client>>(restResponse.Content);
        }
       
    }
}
