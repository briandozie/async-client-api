using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace ClientGUI
{
    /// <summary>
    /// Interaction logic for IPAddDialog.xaml
    /// </summary>
    public partial class IPAddDialog : Window
    {
        public IPAddDialog()
        {
            InitializeComponent();
        }

        public string IPAddress
        {
            get { return txtIPAdd.Text; }
        }

        public string PortNumber
        {
            get { return txtPortNum.Text; }
        }

        
        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    
    }
}
