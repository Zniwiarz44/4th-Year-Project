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
using LanDevices;
using MonitorNetwork_;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Net.Sockets;
using MJsniffer;

//http://www.dreamincode.net/forums/topic/191471-reading-and-writing-xml-using-serialization/
//TIMER
//http://stackoverflow.com/questions/2258830/dispatchertimer-vs-a-regular-timer-in-wpf-app-for-a-task-scheduler
//http://www.wpf-tutorial.com/misc/dispatchertimer/
namespace Project_v1._0._0
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// KrystianAdmin - Database Admin + my pass, server name m6xjrd8efo ,m6xjrd8efo.database.windows.net
    /// </summary>

    public enum Protocol
    {
        TCP = 6,
        UDP = 17,
        Unknown = -1
    };
    public partial class MainWindow : Window
    {
        private int counter = 0;                                // Used to determine if button was clicked
        private List<int> monitorArray = new List<int>();       // Contains list of
        private int monitorFrequency = 10;  //20sec
        private DispatcherTimer timer = new DispatcherTimer();
        bool firstIteration = true;
        bool click = false;                                     // Play/Stop button
        bool enablePlayButton = false;                          // Determines when Play button is available
        private Socket mainSocket;                          //The socket which captures all incoming packets
        private byte[] byteData = new byte[4096];
        private bool bContinueCapturing = false;            //A flag to check if packets are to be captured or not
        private EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Broadcast, 1900);
        private delegate void AddTrafficList(TrafficObjectsGrid gridObj);
       
        public MainWindow()
        {
            InitializeComponent();
            NetworkCollection networkList = new NetworkCollection();
            NetworkName.Content = networkList.GetCurrentNetwork();
            //defaultIpGateway = networkList.getDefaultIPGateway();
            networkList.getNetworkAddress();
            MyIpAddress.Content = networkList.getMyIpAddress();
            /*Initialize Combobox*/
            ComboBoxItem newitem = new ComboBoxItem();
            //newitem.Content = "test 1";
            /*  AvailableNetworks.Items.Add("Settings");
              AvailableNetworks.Items.Add("Monitoring");
              AvailableNetworks.SelectedIndex = 0;*/
            //   FillDataGrid();
            RunGridAnimation();
            
        }
        public void RunGridAnimation()
        {
            //  grdEmployee.BorderBrush.
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

        // Respond to the right mouse button down event by setting up a hit test results callback. 
        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            /*    // Retrieve the coordinate of the mouse position.
                Point pt = e.GetPosition((UIElement)sender);

                // Clear the contents of the list used for hit test results.
                hitResultsList.Clear();
            
                // Set up a callback to receive the hit test result enumeration.
                VisualTreeHelper.HitTest(myCanvas, null, new HitTestResultCallback(MyHitTestResult), new PointHitTestParameters(pt));

                // Perform actions on the hit test results list. 
                if (hitResultsList.Count > 0)
                {
                    Console.WriteLine("Number of Visuals Hit: " + hitResultsList.Count);
                }*/
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {

            if (counter == 0)
            {
                NetworkCollection networkList = new NetworkCollection();
                networkList.parseIPAddress(networkList.getNetworkAddress().ToString());
                ScanProgress.Content = "Scan complete";
                ScanButton.Content = "Display";
                counter++;
            }
            else
            {
                var buttons = CanvasMain.Children.OfType<Button>().ToList();    //Find all objects of type button
                foreach (var button in buttons)
                {
                    CanvasMain.Children.Remove(button);                         // Remove only buttons, that way this wont affect other children within CanvasMain eg: Grid
                }
                //   monitorArray.Clear();       // Remove all elements from the array every time scan runs to avoid unnecessary traffic on the network (Duplicate ip's)
                LoadDeviceMap();
                ScanButton.Content = "Scan";
                counter--;
                BeginMonitoring();   //Start monitoring the network
            }
            firstIteration = true;

            /*  System.Net.NetworkInformation.IPGlobalProperties network = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
              System.Net.NetworkInformation.TcpConnectionInformation[] connections = network.GetActiveTcpConnections();
        
              foreach (System.Net.NetworkInformation.TcpConnectionInformation connection in connections)
              {
                  connection.RemoteEndPoint.AddressFamily;                
              }*/
        }

        private void BeginMonitoring()
        {
            LabelMonitor.Content = "Monitoring in progress...";
            Start_StopButton.Content = "Pause";
            enablePlayButton = true;
            click = true;
            InitializeComponent();
            StartListening();
            //   DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(monitorFrequency);  //Replace 20 with specified input fro Settings > Monitoring > Monitoring Frequency
            timer.Tick += timer_Tick;       // WARNING, ENTER THIS ONLY ONCE
            timer.Start();
            // LabelMonitor.Content = "Monitoring in progress..."+ timer.Interval.Seconds;
        }
        private void timer_Tick(object sender, EventArgs e)
        {

            MonitorNetwork mn = new MonitorNetwork();
            String ip = parseIPAddress(MyIpAddress.Content.ToString());     // Cut the last octet of IP address
            mn.RunPingers(ip, monitorArray);                                // Send heart beat packets to known devices

            //Variables for the NetworkPerformance()
            int iterator = 1;
            float daley = 0;
            Pingers p = new Pingers();
            MyXML.GetObject(ref p.lanDeviceDetails, "Devices.xml");    ///-----<READ FILE>------
            foreach (NetworkStatus ns in mn.NetworkMonitorList)
            {
                grdEmployee.Items.Add(ns);
            }
            foreach (DeviceDetails dd in p.lanDeviceDetails)
            {
                if(dd.Latency >= 0)
                {
                    daley += dd.Latency / iterator;
                    NetworkPerformance(iterator, daley);
                    iterator++;
                }
                
            }
            // grdEmployee.Items.Add(nList.Current);
            mn.RemoveNetworkStatus();

            /*   lock(trafficList)
               {
                   trafficList.Items.Refresh();
               }  */

        }
        public string parseIPAddress(string ipAddress)
        {
            int lastIndex = 0;
            int i = 0;
            while ((i = ipAddress.IndexOf('.', i)) != -1)    //while didnt get to the end
            {
                lastIndex = i;  //Assing last index
                i++;
            }
            //Start removing from 192.168.1.++
            return ipAddress.Remove(lastIndex + 1);  //End result: 192.168.1.          
        }
        private void NetworkPerformance(int iterator, float daley)
        {
            AvgDaley.Content = "Average daley: " + daley + "ms";
            NoDevices.Content = "No. devices: " + iterator;
            if (daley <= 50)
            {
                NetworkPerformance_Label.Foreground = Brushes.CadetBlue;
            }
            else if (daley > 50 && daley <= 150)
            {
                NetworkPerformance_Label.Foreground = Brushes.Green;
            }
            else if (daley > 150 && daley <= 500)
            {
                NetworkPerformance_Label.Foreground = Brushes.Orange;
            }
            else
            {
                NetworkPerformance_Label.Foreground = Brushes.Red;
            }
        }
        private void LoadDeviceMap()
        {
            List<DeviceDetails> ld = new List<DeviceDetails>();
            Pingers p = new Pingers();
            MyXML.GetObject(ref p.lanDeviceDetails, "Devices.xml");    ///-----<READ FILE>------

            int i = 100;
            int iPrint = 100;
            int topCount = 1;
            int mapWidth = 1;
            bool iterate = false;
            //Variables for the NetworkPerformance()
            int iterator = 1;
            float daley = 0;
            foreach (DeviceDetails dd in p.lanDeviceDetails)
            {
                if (dd.Latency >= 0)
                {
                    daley += dd.Latency / iterator;
                    NetworkPerformance(iterator, daley);
                    iterator++;
                }
          
                if (monitorArray.Contains(GetLastOctet(dd.IpAddress))) { }  // Do not allow duplicates
                else { monitorArray.Add(GetLastOctet(dd.IpAddress)); }
                switch (dd.Type)
                {
                    case deviceType.PC:
                        Button myButton2 = new Button
                        {
                            Width = 50,
                            Height = 50,
                            Content = new Image
                            {
                                Source = new BitmapImage(new Uri(@"Images/PC.jpg", UriKind.Relative)),
                                VerticalAlignment = VerticalAlignment.Center,
                                Stretch = Stretch.Fill
                            }
                        };

                        myButton2.Tag = dd.IpAddress;
                        myButton2.Click += new RoutedEventHandler(MakeButtonClickHandler());
                        CanvasMain.Children.Add(myButton2);
                        if (i > 400 || topCount > 1 || iterate)
                        {
                            iterate = true;
                            Canvas.SetTop(myButton2, 60 * topCount);
                            if (mapWidth > 4)
                            {
                                mapWidth = 1;
                                i = 100;
                                if (firstIteration == false)
                                {
                                    topCount++;
                                }
                                firstIteration = false;//Kind of working

                            }
                        }
                        Canvas.SetLeft(myButton2, i);
                        i += 100;
                        mapWidth++;
                        break;

                    case deviceType.Router:
                        Button myButton = new Button
                        {
                            Width = 70,
                            Height = 50,
                            Background = Brushes.Transparent,
                            BorderBrush = Brushes.Transparent,
                            Content = new Image
                            {
                                Source = new BitmapImage(new Uri(@"Images/GUI_Router.png", UriKind.Relative)),
                                VerticalAlignment = VerticalAlignment.Center,
                                Stretch = Stretch.Fill
                            }
                        };
                        myButton.Tag = dd.IpAddress;
                        myButton.Click += new RoutedEventHandler(MakeButtonClickHandler());
                        /* Image router = new Image
                         {
                             Width = 50,
                             Source = new BitmapImage(new Uri(@"Images/GUI_Cluster2.png", UriKind.Relative))
                         };*/
                        CanvasMain.Children.Add(myButton);
                        Canvas.SetTop(myButton, 50);
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

                    default:
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

        private Action<object, RoutedEventArgs> MakeButtonClickHandler()
        {
            return (object sender, RoutedEventArgs e) =>
            {
                Button b = (Button)sender;
                Pingers p = new Pingers();
                MyXML.GetObject(ref  p.lanDeviceDetails, "Devices.xml");    ///-----<READ FILE>------
                String mac = null;
                long latency = 0;
                foreach (DeviceDetails dd in p.lanDeviceDetails)
                {
                    if (dd.IpAddress.ToString() == b.Tag.ToString())
                    {
                        mac = dd.MacAddress;
                        latency = dd.Latency;    
                        if(latency < 0)
                        {
                          /*  ToolTip tt = new ToolTip();
                            tt.Content = "Offline!";
                            b.ToolTip = tt;*/
                            b.Background = Brushes.Red;
                        }
                        else
                        {
                            b.Background = Brushes.Transparent;
                        }
                        break;
                    }
                    else
                    {
                        mac = "Not found";
                        // latency = "Unknown";
                    }
                }
                MessageBox.Show("IP Address: " + b.Tag + "\nMac Address: " + mac + "\nLatency: " + latency + "ms");
            };
        }
        private void Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private int GetLastOctet(string ipAddress)
        {
            //Example 192.168.1.25
            int lastOctet = 0;
            int lastIndex = 0;
            int i = 0;
            while ((i = ipAddress.IndexOf('.', i)) != -1)    //while didnt get to the end
            {
                lastIndex = i;  //Assing last index
                i++;
            }
            //Start removing from 192.++ up to 168.
            String result = ipAddress.Remove(0, lastIndex + 1);  //End result: 25
            lastOctet = int.Parse(result);
            return lastOctet;
        }


        private void Set20sec(object sender, RoutedEventArgs e)
        {
            monitorFrequency = 20;
            timer.Interval = TimeSpan.FromSeconds(monitorFrequency);

        }
        private void Set60sec(object sender, RoutedEventArgs e)
        {
            monitorFrequency = 60;
            timer.Interval = TimeSpan.FromSeconds(monitorFrequency);
        }
        private void Set5min(object sender, RoutedEventArgs e)
        {
            monitorFrequency = 300;
            timer.Interval = TimeSpan.FromSeconds(monitorFrequency);
        }
        private void Set30min(object sender, RoutedEventArgs e)
        {
            monitorFrequency = 1800;
            timer.Interval = TimeSpan.FromSeconds(monitorFrequency);
        }

        private void Start_StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!click && enablePlayButton)          // Start monitoring
            {
                Start_StopButton.Content = "Pause";
                click = true;
                LabelMonitor.Content = "Monitoring in progress...";
                timer.Start();
                StartListening();
                //  RunTrafficReceiver(true);
            }
            else
            {
                Start_StopButton.Content = "Play";  // Stop monitoring
                click = false;
                LabelMonitor.Content = "Monitoring in paused...";
                timer.Stop();
                StartListening();
                //  RunTrafficReceiver(false);
            }
        }
        private void StartListening()
        {
            try
            {
                if (!bContinueCapturing)
                {
                    //Start capturing the packets...
                    bContinueCapturing = true;

                    //For sniffing the socket to capture the packets has to be a raw socket, with the
                    //address family being of type internetwork, and protocol being IP
                    mainSocket = new Socket(AddressFamily.InterNetwork,
                        SocketType.Raw, ProtocolType.IP);

                    //Bind the socket to the selected IP address
                    mainSocket.Bind(new IPEndPoint(IPAddress.Parse(MyIpAddress.Content.ToString()), 0));

                    //Set the socket  options
                    mainSocket.SetSocketOption(SocketOptionLevel.IP,            //Applies only to IP packets
                                               SocketOptionName.HeaderIncluded, //Set the include the header
                                               true);                           //option to true

                    byte[] byTrue = new byte[4] { 1, 0, 0, 0 };
                    byte[] byOut = new byte[4] { 1, 0, 0, 0 }; //Capture outgoing packets

                    //Socket.IOControl is analogous to the WSAIoctl method of Winsock 2
                    mainSocket.IOControl(IOControlCode.ReceiveAll,              //Equivalent to SIO_RCVALL constant
                        //of Winsock 2
                                         byTrue,
                                         byOut);

                    //Start receiving the packets asynchronously
                    mainSocket.BeginReceiveFrom(byteData, 0, byteData.Length, SocketFlags.None, ref remoteEndPoint,
                        new AsyncCallback(OnReceive), null);
                }
                else
                {
                    bContinueCapturing = false;
                    //To stop capturing the packets close the socket
                    mainSocket.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n\nFix: Run as administrator.", "Network Mapping", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                int nReceived = mainSocket.EndReceive(ar);

                //Analyze the bytes received...

                ParseData(byteData, nReceived);

                if (bContinueCapturing)
                {
                    byteData = new byte[4096];

                    //Another call to BeginReceive so that we continue to receive the incoming
                    //packets
                    mainSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None,
                        new AsyncCallback(OnReceive), null);
                }
            }
            catch (ObjectDisposedException ex)
            {
                MessageBox.Show(ex.Message, "Network Mapping", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Network Mapping", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ParseData(byte[] byteData, int nReceived)
        {
            try
            {
                IPHeader ipHeader = new IPHeader(byteData, nReceived);

                if (ipHeader.SourceAddress.ToString().Equals("0.0.0.0") || ipHeader.DestinationAddress.ToString().Equals("239.255.255.250"))    //Capute only broadcast messages
                {
                    TrafficObjectsGrid tObjGrid = new TrafficObjectsGrid();
                    switch (ipHeader.ProtocolType)
                    {
                        case Protocol.TCP:
                            TCPHeader tcpHeader = new TCPHeader(ipHeader.Data,              //IPHeader.Data stores the data being 
                                //carried by the IP datagram
                                                        ipHeader.MessageLength);
                            tObjGrid = new TrafficObjectsGrid(ipHeader.SourceAddress.ToString(), ipHeader.DestinationAddress.ToString(), tcpHeader.DestinationPort);
                            break;
                        case Protocol.UDP:
                            UDPHeader udpHeader = new UDPHeader(ipHeader.Data,              //IPHeader.Data stores the data being 
                                //carried by the IP datagram
                                                    (int)ipHeader.MessageLength);//Length of the data field        
                            tObjGrid = new TrafficObjectsGrid(ipHeader.SourceAddress.ToString(), ipHeader.DestinationAddress.ToString(), udpHeader.DestinationPort);

                            break;
                        case Protocol.Unknown:
                            tObjGrid = new TrafficObjectsGrid(ipHeader.SourceAddress.ToString(), ipHeader.DestinationAddress.ToString(), "0");
                            break;
                    }
                    //Dispacher will allow this thread to safely update the main UI thread
                    Action dataGridWork = delegate
                    {
                        trafficList.Items.Add(tObjGrid);
                    };
                    trafficList.Dispatcher.BeginInvoke(DispatcherPriority.Background, dataGridWork);

                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, "Network Mapping");
            }
        }
    }
    class TrafficObjectsGrid
    {
        private string source;
        private string destination;
        private string port;
        public TrafficObjectsGrid(string s, string d, string port)
        {
            Source = s;
            Destination = d;
            Port = port;
        }

        public TrafficObjectsGrid()
        {
            // TODO: Complete member initialization
        }
        public String Source
        {
            get { return source; }
            set { source = value; }
        }
        public String Destination
        {
            get { return destination; }
            set { destination = value; }
        }
        public String Port
        {
            get { return port; }
            set { port = value; }
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