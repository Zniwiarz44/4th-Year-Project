using CurrentNetwork;
using NETWORKLIST;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Net.NetworkInformation;
using System.Threading;
using System.Diagnostics;
using System.Net;
using Lan_Users;
using Pingers_;
using System.Windows.Media.Imaging;
using LanDevices;
using System.Xml;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
//http://www.dreamincode.net/forums/topic/191471-reading-and-writing-xml-using-serialization/
namespace Project_v1._0._0
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int counter = 0;
        public MainWindow()
        {
            InitializeComponent();
            NetworkCollection networkList = new NetworkCollection();
            NetworkName.Content = networkList.GetCurrentNetwork();
            //defaultIpGateway = networkList.getDefaultIPGateway();
            networkList.getNetworkAddress();
            MyIpAddress.Content = networkList.getMyIpAddress();
         //   FillDataGrid();
        }
        private void FillDataGrid() //Create database connection
        {
            string ConString = ConfigurationManager.ConnectionStrings["MovieDBContext"].ConnectionString;   //Database name
            string CmdString = string.Empty;
            using (SqlConnection con = new SqlConnection(ConString))
            {
                CmdString = "SELECT id, title FROM Movies";
                SqlCommand cmd = new SqlCommand(CmdString, con);
                SqlDataAdapter sda = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable("Movies");
                sda.Fill(dt);
                grdEmployee.ItemsSource = dt.DefaultView;
            }
        }

        private void AvailableNetworks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        
        private void Button_Click(object sender, RoutedEventArgs e)
        {
           // LoadDeviceMap();
           // List<DeviceDetails> ld = new List<DeviceDetails>();
          /*  string FileName = "MyUnit.xml";
            DeviceDetails devD1 = new DeviceDetails("192.168.1.1", "Router", deviceType.Router);
            DeviceDetails devD2 = new DeviceDetails("192.168.1.2", "Anna", deviceType.PC);
            DeviceDetails devD3 = new DeviceDetails("192.168.1.3", "Tony", deviceType.PC);
            DeviceDetails devD4 = new DeviceDetails("192.168.1.4", "Krystian", deviceType.PC);
            DeviceDetails devD5 = new DeviceDetails("192.168.1.5", "Printer", deviceType.Printer);
            ld.Add(devD1);
            ld.Add(devD2);
            ld.Add(devD3);
            ld.Add(devD4);
            ld.Add(devD5);
            MyXML.SaveObject(ld, FileName);*/

            if(counter == 0)
            {  
                NetworkCollection networkList = new NetworkCollection();
                networkList.parseIPAddress(networkList.getNetworkAddress().ToString());
                ScanProgress.Content = "Scan complete";           
                ScanButton.Content = "Display";
                counter++;
            }
            else
            {
                LoadDeviceMap();
                ScanButton.Content = "Scan";
                counter--;
            } 
           
        }
        private void LoadDeviceMap()
        {
            List<DeviceDetails> ld = new List<DeviceDetails>();
            Pingers p = new Pingers();
           // p.RunPingers("10.10.20.");
           // ld = p.lanDeviceDetails;
           /* DeviceDetails devD = new DeviceDetails("10.10.20.254", "Unknown", deviceType.Router);
            p.lanDeviceDetails.Add(devD);*/

            MyXML.GetObject(ref p.lanDeviceDetails, "Devices.xml");    ///-----<READ FILE>------

           /* XmlDocument doc = new XmlDocument();
            doc.Load("C:/Users/X0009_000/Documents/Visual Studio 2013/Projects/Project v1.0.0/Project v1.0.0/NetworkMaps/SampleMap.xml");
            foreach(XmlNode node in doc.DocumentElement)
            {
                String ipAddress = node.Attributes[0].Value;
                String hostName = node.Attributes[0].InnerText;
                String type = node.Attributes[0].InnerText;
                deviceType dType;
                Enum.TryParse<deviceType>(type, out dType);
                DeviceDetails devD = new DeviceDetails(ipAddress, hostName, dType);
               // ld.Add(new DeviceDetails(ipAddress, hostName, dType));
                ld.Add(devD);
                if (deviceType.Laptop == dType)
                {
                    ld.Add(new DeviceDetails(ipAddress, hostName, dType));
                } 
                if (deviceType.PC == dType)
                { 
                    ld.Add(new DeviceDetails(ipAddress, hostName, deviceType.PC));
                }      
            }
            */
            int i = 100;
            int iPrint = 100;
            foreach (DeviceDetails dd in p.lanDeviceDetails)
            {
                switch(dd.Type)
                {
                    case deviceType.PC:
                    Image image = new Image
                    {
                        Width = 50,
                        Source = new BitmapImage(new Uri(@"Images/PC.jpg", UriKind.Relative))
                    };
                    CanvasMain.Children.Add(image);
                    if(i>400)
                    {
                        Canvas.SetTop(image, 60);
                        i = 100;
                    }
                    Canvas.SetLeft(image, i);
                    i += 100;
                    break;

                    case deviceType.Router:
                    Image router = new Image
                    {
                        Width = 50,
                        Source = new BitmapImage(new Uri(@"Images/Router.jpg", UriKind.Relative))
                    };
                    CanvasMain.Children.Add(router);
                    Canvas.SetTop(router, 50);
                    break;

                    case deviceType.Printer:
                    Image printer = new Image
                    {
                        Width = 50,
                        Source = new BitmapImage(new Uri(@"Images/Printer.jpg", UriKind.Relative))
                    };
                    CanvasMain.Children.Add(printer);
                    Canvas.SetTop(printer, 70);
                    Canvas.SetLeft(printer, iPrint);
                    iPrint += 100;
                    break;

                    default :
                    Image und = new Image
                    {
                        Width = 50,
                        Source = new BitmapImage(new Uri(@"Images/PC.jpg", UriKind.Relative))
                    };
                    CanvasMain.Children.Add(und);
                    Canvas.SetLeft(und, i);
                    break;
                }
            }
        }
        private void ListView_Users_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void grdEmployee_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}

namespace Lan_Users
{
    class LanUsers
    {
        private String ipAddress;
        private String host;

        public LanUsers(String ipAddress, String host)
        {
            IpAddress = ipAddress;
            Host = host;
        }
        public String IpAddress
        {
            get { return ipAddress; }
            set { ipAddress = value; }
        }
        public String Host
        {
            get { return host; }
            set { host = value; }
        }
    }
}