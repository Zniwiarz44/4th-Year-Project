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
using System.Windows.Media.Animation;
using System.IO;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using Microsoft.Research.DynamicDataDisplay.PointMarkers;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using PacketDotNet;
using SharpPcap;
using SnmpSharpNet;


//http://www.dreamincode.net/forums/topic/191471-reading-and-writing-xml-using-serialization/
//TIMER
//http://stackoverflow.com/questions/2258830/dispatchertimer-vs-a-regular-timer-in-wpf-app-for-a-task-scheduler
//http://www.wpf-tutorial.com/misc/dispatchertimer/
namespace Project_v1._0._2
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
        #region Local variables

        private int counter = 0;                            // Used for worker2
        private int monitorFrequency = 10;                  // 20sec
        private int inactiveAttempt = 3;                    // Number of attempts to see if device is online or not
        private int mapCounter = 1;                         // Can be either 0 or 1, it keeps track of number of calls for PrepareForMonitoring()
        private int timerCounter = 0;                       // Make sure the timer is entered only once
        private int panelHeight = 165;                      // Initial login panel height
        private int borderCount = 0;                        // Keeps track of border animation
        private int ndevice = 0;                            // Index of selected interface
        private int snmpCounter = 0;                        // Keep track of numbers of calls for snmp
        private int performanceCounter = 1;                 // Network 1 or 2
        private const int RESPOND_TIME = 65;                // On average routers need 60sec to restart, so if some devices are down check in 65sec if they are active

        private bool firstIteration = true;
        private bool click = false;                         // Play/Stop button
        private bool enablePlayButton = false;              // Determines when Play button is available
        private bool bContinueCapturing = false;            // A flag to check if packets are to be captured or not
        private bool wifi = false;                          // Check for wireless connection
        private bool scanWithSNMP = true;
        private bool completedSNMPScan = false;             // Once SNMP completed scanning draw the map
        private bool compareNetworks = false;               // Hides device map and opens window for comparing networks

        private string defaultIpGateway = null;             // Set default gateway
        private string networkName = null;                  // Set network name
        private string fileNameXML = null;                  // XML file name
        private string myIpAddress = null;                  // Set IP address of current machine
        private string networkInfoFile = null;              // File used to store network performance data
        private string blobUsername = null;
        private string communitySNMP = "public";            // Default SNMP community
        private string network1CSV = null;                  // Store filenames to later use them to load data using graph
        private string network2CSV = null;                  // They are used just for network comparing

        private static List<string> monitorArray = new List<string>();                      // Contains list of ip addresses
        private List<NetworkInfo> netInfoList = new List<NetworkInfo>();                    // Contains information necessary for the graph to be drawn
        private List<NetworkInfo> net1InfoList = new List<NetworkInfo>();                   // These lists store information for network comparison
        private List<NetworkInfo> net2InfoList = new List<NetworkInfo>();
        private List<string> inactiveDevices = new List<string>();                          // When device becomes offline is added to this list to perform a check with inactiveCheck()
        private static List<NetworkStatus> monitorTempList = new List<NetworkStatus>();     // Temp array to store values from MonitorNetwork.cs so the can be added to the UI

        private DispatcherTimer timer = new DispatcherTimer();              // Timer for frequent monitoring
        private DispatcherTimer timerInactive = new DispatcherTimer();      // Timer for inactive devices

        private Object lockUpdateNet = new Object();
        private Object lockCounter = new Object();
        private Object lockFileRead = new Object();
        private Object lockTimer = new Object();
        private Object lockSNMP = new Object();                             // Make sure that this is only called when necessary

        private Socket mainSocket;                                          // The socket which captures all incoming packets
        private byte[] byteData = new byte[4096];
        private EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Broadcast, 1900);
        private delegate void AddTrafficList(TrafficObjectsGrid gridObj);

        private IAsyncResult oldAsync;

        private readonly BackgroundWorker worker = new BackgroundWorker();  // This thread performs an ip sweep and at the same time keep the program responsive.
        private readonly BackgroundWorker worker2 = new BackgroundWorker(); // Worker for adding new ip addresses
        private readonly BackgroundWorker worker3 = new BackgroundWorker(); // Try SNMP to get more detail information

        #endregion

        //    private object @lock = new object();
        //   private IAsyncResult currentAynchResult;
        public MainWindow()
        {
            InitializeComponent();
            NetworkCollection networkList = new NetworkCollection();
            networkName = networkList.GetCurrentNetwork();
            NetworkName.Content = networkName;                          // Sets network name
            defaultIpGateway = networkList.getDefaultIPGateway();
            myIpAddress = networkList.getNetworkAddress();              // Sets IP address
            MyIpAddress.Content = myIpAddress;
            wifi = networkList.checkNetworkType();
            if (!wifi)       // If not using wifi let the user to pick the interface to listen for packets CDP
            {

            }
            /*Initialize Combobox*/
            ComboBoxItem newitem = new ComboBoxItem();
            //newitem.Content = "test 1";
            /*  AvailableNetworks.Items.Add("Settings");
              AvailableNetworks.Items.Add("Monitoring");
              AvailableNetworks.SelectedIndex = 0;*/
            //   FillDataGrid();
            RunGridAnimation();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string[] name = DateTime.Now.ToLongTimeString().Split(':');

            if (myIpAddress.Equals("0.0.0.0") || myIpAddress == null)
            {
                MessageBox.Show("Please connect to a network", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                if (timer.IsEnabled)
                {
                    timer.Stop();
                }

                fileNameXML = "Devices_" + DateTime.Now.ToShortDateString() + "_" + name[0] + '-' + name[1] + '-' + name[2] + ".xml";                                // Create this file in local directory
                networkInfoFile = "NetworkInfo" + DateTime.Now.ToShortDateString() + "_" + name[0] + '-' + name[1] + '-' + name[2] + ".csv";
                Pingers p = new Pingers();
                if (worker.IsBusy != true)
                {
                    lock (lockCounter)
                    {
                        mapCounter--;
                    }
                    ScanProgress.Content = "Scan in progress...";
                    ScanButton.IsEnabled = false;                           // Disable Scan button to not interrupt the scan
                    worker.DoWork += worker_DoWork;                         // Perform ping sweep on this new thread
                    worker.RunWorkerCompleted += worker_RunWorkerCompleted; // Update UI once the scan is completed
                    worker.RunWorkerAsync();                                // Kick off new thread
                }
                firstIteration = true;
            }
        }
        private void PrepareForMonitoring()
        {
            Debug.WriteLine("\n<<<<PrepareForMonitoring>>>>\n");
            Pingers p = new Pingers();
            var buttons = CanvasMain.Children.OfType<Button>().ToList();//Find all objects of type button
            foreach (var button in buttons)
            {
                CanvasMain.Children.Remove(button);                     // Remove only buttons, that way this wont affect other children within CanvasMain eg: Grid
            }
            monitorArray.Clear();                                       // Remove all elements from the array every time scan runs to avoid unnecessary traffic on the network (Duplicate ip's)

            #region FindDefaultGateway
            //   Pingers p = new Pingers();
            bool foundDefault = false;
            lock (lockFileRead)
            {
                MyXML.GetObject(ref p.lanDeviceDetails, fileNameXML);       //-----<READ FILE>------
                foreach (DeviceDetails dd in p.lanDeviceDetails)
                {
                    if (dd.IpAddress.Equals(defaultIpGateway))
                    {
                        foundDefault = true;
                        break;
                    }
                }
            }
            if (!foundDefault)
            {
                //MessageBox.Show("DEFAULT");   //Debug
                p.AddDefaultGateway();
            }
            #endregion
            ScanButton.Content = "Scan";
            if (scanWithSNMP)
            {
                if (worker2.IsBusy != true)
                {
                    lock (lockCounter)
                    {
                        snmpCounter++;
                    }
                    worker3.DoWork += worker3_DoWork;                           // Perform ping sweep on this new thread
                    worker3.RunWorkerCompleted += worker3_RunWorkerCompleted;   // Update UI once the scan is completed
                    worker3.RunWorkerAsync();                                   // Kick off new thread
                }
            }
            else
            {
                ScanButton.IsEnabled = true;                                    // Enable scan button
                LoadDeviceMap();
                BeginMonitoring();
            }
        }
        public bool checkForSNMP()
        {
            // SNMP community name
            OctetString community = new OctetString(communitySNMP);

            // Define agent parameters class
            AgentParameters param = new AgentParameters(community);
            // Set SNMP version to 1 (or 2)
            param.Version = SnmpVersion.Ver2;
            // Construct the agent address object
            // IpAddress class is easy to use here because
            //  it will try to resolve constructor parameter if it doesn't
            //  parse to an IP address

            Pingers p = new Pingers();
            MyXML.GetObject(ref p.lanDeviceDetails, fileNameXML);        //-----<READ FILE>------
            foreach (DeviceDetails dd in p.lanDeviceDetails)
            {
                lock (lockSNMP)
                {
                    IpAddress agent = new IpAddress(dd.IpAddress);
                    // Construct target
                    UdpTarget target = new UdpTarget((IPAddress)agent, 161, 2000, 1);
                    // Pdu class used for all requests
                    Pdu pdu = new Pdu(PduType.Get);
                    // pdu.VbList.Add("1.3.6.1.2.1.1.1.0");  //sysDescr      //1.3.6.1.4.1.8072.3.2.10  default//1.3.6.1.4.1.5923.1.1.1.7
                    pdu.VbList.Add("1.3.6.1.2.1.1.2.0");    //sysObjectID   //1.3.6.1.2.1.2.2.1.2.5 eth01
                    pdu.VbList.Add("1.3.6.1.2.1.1.3.0");    //sysUpTime      1.3.6.1.2.1.17.4.3.1.2
                    pdu.VbList.Add("1.3.6.1.2.1.1.4.0");    //sysContact
                    pdu.VbList.Add("1.3.6.1.2.1.1.5.0");    //sysName       //Cisco 1.3.6.1.2.1.4.1 ipforwarding 1-true 2-false

                    try
                    {
                        // Make SNMP request
                        SnmpV2Packet result = (SnmpV2Packet)target.Request(pdu, param);
                        // If result is null then agent didn't reply or we couldn't parse the reply.
                        if (result != null)
                        {
                            // ErrorStatus other then 0 is an error returned by 
                            // the Agent - see SnmpConstants for error definitions
                            if (result.Pdu.ErrorStatus != 0)
                            {
                                // agent reported an error with the request
                                Debug.WriteLine("Error in SNMP reply. Error {0} index {1}",
                                    result.Pdu.ErrorStatus,
                                    result.Pdu.ErrorIndex);
                            }
                            else
                            {
                                dd.Description = "Description:\nsysObjectID: " + result.Pdu.VbList[0].Value.ToString()
                                    + "\nsysUpTime: " + result.Pdu.VbList[1].Value.ToString()
                                    + "\nsysContact: " + result.Pdu.VbList[2].Value.ToString()
                                    + "\nsysName: " + result.Pdu.VbList[3].Value.ToString();
                                dd.IsSNMPEnabled = true;
                            }
                        }
                        else
                        {
                            Debug.WriteLine("No response received from SNMP agent.");
                        }
                    }
                    catch (SnmpException ex)
                    {
                        Debug.WriteLine("\n-----<SnmpException>-----\nClass: MainWindow\ncheckForSNMP()\n" + ex.Message + "\n");
                    }
                    target.Close();
                }
            }
            MyXML.SaveObject(p.lanDeviceDetails, fileNameXML);
            return true;
        }

        #region Background_Worker
        //-----<Ping Sweep>-----
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Pingers p = new Pingers();
            p.RunPingers(parseIPAddress(myIpAddress), fileNameXML);     // Perform ping sweep for your IP address /24
            while (p.GetCompletedInstances < 255) ;                     // Make sure all ping requests are processed
            p.DestroyPingers();                                         // Free up the memory once ping sweep has ben completed
            Debug.WriteLine("Worker1 Completed requests: {0}", p.GetCompletedInstances);
        }
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (scanWithSNMP)
            {
                ScanProgress.Content = "SNMP scan...";
            }
            else
            {
                ScanProgress.Content = "Scan complete";
            }
            if (mapCounter <= 0)                        // Enter this section only Once, when the scan is complete this fixes an issue with multiple PrepareForMonitoring() calls
            {
                lock (lockCounter)
                {
                    mapCounter++;
                    timerCounter = 1;      // This will allow the tickTime to run
                }
                PrepareForMonitoring();
            }
        }
        //-----<Monitoring>-----
        private void worker2_DoWork(object sender, DoWorkEventArgs e)
        {
            Pingers p = new Pingers();

            MyXML.GetObject(ref p.lanDeviceDetails, fileNameXML);       ///-----<READ FILE>------

            foreach (DeviceDetails dd in p.lanDeviceDetails)            // Update latency on each device
            {
                if (monitorArray.Contains(dd.IpAddress)) { }            // Do not allow duplicates
                else { monitorArray.Add(dd.IpAddress); }
            }

            int monitorLength = monitorArray.Count;
            MonitorNetwork mn = new MonitorNetwork();

            mn.RunPingers(monitorArray, fileNameXML);                                   // Send heart beat packets to known devices MonitorNetwork.cs
            while (mn.GetCompletedInstances < monitorLength) ;
            Debug.WriteLine("Worker2 Completed requests: {0}", mn.GetCompletedInstances);
            monitorTempList.Clear();
            foreach (NetworkStatus ns in mn.NetworkMonitorList)
            {
                monitorTempList.Add(ns);
            }
            mn.RemoveNetworkStatus();
        }
        private void worker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //grdEmployee.Items.Clear();
            foreach (NetworkStatus ns in monitorTempList)
            {
                //  MessageBox.Show(ns.Name + "");
                grdEmployee.Items.Add(ns);                              // Populate the grid in MainWindow.xml with ipaddresses and their status
            }
            monitorTempList.Clear();                                    // Clear the buffer and get ready to capute new status.      
            if (counter == 1)
            {
                lock (lockCounter)
                {
                    counter--;
                    updateDevices();                                    // WARNING this should also be a separate thread
                    //MessageBox.Show("UPDATE");
                    if (inactiveDevices.Count > 0)
                    {
                        inactiveCheck();
                    }
                }
            }
        }
        //-----<SNMP>-----
        private void worker3_DoWork(object sender, DoWorkEventArgs e)
        {
            if (snmpCounter == 1)                                                    // Enter this section only once
            {
                completedSNMPScan = checkForSNMP();
            }
            Debug.WriteLine("Worker3 Completed SNMP requests");
        }
        private void worker3_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ScanProgress.Content = "Scan complete";
            lock (lockCounter)
            {
                snmpCounter--;
            }
            if (snmpCounter == 0)                                                   // Enter this section only once
            {
                if (completedSNMPScan)
                {
                    ScanButton.IsEnabled = true;                                    // Enable scan button
                    LoadDeviceMap();
                    BeginMonitoring();
                }
            }
        }
        #endregion
        private void BeginMonitoring()
        {
            LabelMonitor.Content = "Monitoring in progress...";
            Start_StopButton.Content = "Pause";
            enablePlayButton = true;
            click = false;
            StartMonitoring();                                          // Listen for broadcast and add to the list if new IP is found
            InitializeComponent();
            // StartListening();          
            //   DispatcherTimer timer = new DispatcherTimer();
            if (timer.IsEnabled)
            {
                timer.Stop();
            }
            timer.Interval = TimeSpan.FromSeconds(monitorFrequency);    //Replace 20 with specified input fro Settings > Monitoring > Monitoring Frequency
            lock (lockTimer)
            {
                if (timerCounter == 1)// WARNING, ENTER THIS ONLY ONCE
                {
                    timerCounter--;
                    timer.Tick += timer_Tick;
                }
            }
            timer.Start();
            // LabelMonitor.Content = "Monitoring in progress..."+ timer.Interval.Seconds;
        }
        private void timer_Tick(object sender, EventArgs e)
        {
            if (worker2.IsBusy != true)
            {
                Debug.WriteLine("\n-----<timer_Tick>-----\n");
                lock (lockCounter)
                {
                    counter++;
                }
                worker2.DoWork += worker2_DoWork;                           // Perform ping sweep on this new thread
                worker2.RunWorkerCompleted += worker2_RunWorkerCompleted;   // Update UI once the scan is completed
                worker2.RunWorkerAsync();                                   // Kick off new thread
            }
        }
        private void updateDevices()
        {   //Variables for the NetworkPerformance()
            int iterator = 0;
            float daley = 0;

            Pingers p = new Pingers();
            MyXML.GetObject(ref p.lanDeviceDetails, fileNameXML);

            foreach (DeviceDetails dd in p.lanDeviceDetails)            // Update latency on each device
            {
                if (dd.Latency >= 0)
                {
                    iterator++;
                    daley += dd.Latency / iterator;                     // Calculate average daley and pass that to the NetworkPerformance() when the forloop finishes

                    /*   ThreadStart start = delegate()
                       {
                           Dispatcher.Invoke(DispatcherPriority.Background, new Action<int, float>(NetworkPerformance), iterator, daley);
                       };
                       // Create the thread and kick it started! 
                       new Thread(start).Start();*/

                }
                foreach (Button b in FindVisualChildren<Button>(this))  // Find all buttons, we're looking for the ones dynamically created
                {
                    try
                    {
                        if (dd.IpAddress.Equals(b.Tag))                 // Dynamically created buttons have tags
                        {
                            if (dd.Latency < 0)                         // if device is not responding change its icon
                            {
                                inactiveDevices.Add(dd.IpAddress);      // Add device to list with not responding devices
                                switch (dd.Type)
                                {
                                    case deviceType.PC:
                                        b.Content = new Image
                                        {
                                            Source = new BitmapImage(new Uri(@"Images/GUI_PC_Warning.png", UriKind.Relative)),
                                            VerticalAlignment = VerticalAlignment.Center,
                                            Stretch = Stretch.Fill
                                        };
                                        break;
                                    case deviceType.Router:
                                        b.Content = new Image
                                        {
                                            Source = new BitmapImage(new Uri(@"Images/GUI_Router_Warning.png", UriKind.Relative)),
                                            VerticalAlignment = VerticalAlignment.Center,
                                            Stretch = Stretch.Fill
                                        };
                                        break;
                                }
                            }
                            else
                            {
                                switch (dd.Type)
                                {
                                    case deviceType.PC:
                                        b.Content = new Image
                                        {
                                            Source = new BitmapImage(new Uri(@"Images/GUI_PC.png", UriKind.Relative)),
                                            VerticalAlignment = VerticalAlignment.Center,
                                            Stretch = Stretch.Fill
                                        };
                                        break;
                                    case deviceType.Router:
                                        b.Content = new Image
                                        {
                                            Source = new BitmapImage(new Uri(@"Images/GUI_Router.png", UriKind.Relative)),
                                            VerticalAlignment = VerticalAlignment.Center,
                                            Stretch = Stretch.Fill
                                        };
                                        break;
                                }
                            }
                            break;
                        }
                    }
                    catch (ArgumentNullException ex)
                    {
                        MessageBox.Show(ex.Message, "Network Mapping", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            NetworkPerformance(iterator, daley);
        }
        private void timerInactive_Tick(object sender, EventArgs e)
        {
            MonitorNetwork mn = new MonitorNetwork();
            mn.RunPingers(inactiveDevices, fileNameXML);

            Thread thread = new Thread(
               new ThreadStart(
                 delegate()
                 {
                     CanvasMain.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(
                         delegate()
                         {
                             updateDevices();
                         }
                     ));
                 }
             ));
            thread.Start();

            // MessageBox.Show("REQUEST att: " + inactiveAttempt);
            inactiveAttempt--;
            if (inactiveAttempt <= 0)
            {
                timerInactive.Stop();
                inactiveAttempt = 3;
            }
        }
        public void inactiveCheck()                                     // Check if devices are still inactive
        {

            InitializeComponent();
            timerInactive.Interval = TimeSpan.FromSeconds(RESPOND_TIME);  // Check if devices will respond
            timerInactive.Tick += timerInactive_Tick;
            if (timerInactive.IsEnabled)
            { }
            else { timerInactive.Start(); /*MessageBox.Show("REQUEST2");*/ }

        }
        public string parseIPAddress(string ipAddress)                  // Trim the ip address
        {
            int lastIndex = 0;
            int i = 0;
            while ((i = ipAddress.IndexOf('.', i)) != -1)    // while didnt get to the end
            {
                lastIndex = i;  //Assing last index
                i++;
            }
            //Start removing from 192.168.1.++
            return ipAddress.Remove(lastIndex + 1);         //End result: 192.168.1.          
        }
        private void NetworkPerformance(int iterator, float daley)      // Set the label colour depending on network average response time
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
            if (!File.Exists(networkInfoFile))      // File for the graph
            {
                // Create a file to write to. 
                using (StreamWriter sw = File.CreateText(networkInfoFile))
                {
                    sw.Write(DateTime.Now.ToLongTimeString() + "," + iterator + "," + daley);
                }
            }
            else
            {
                File.AppendAllText(networkInfoFile, Environment.NewLine + DateTime.Now.ToLongTimeString() + "," + iterator + "," + daley);
            }
            DrawGraph();
        }
        private void LoadDeviceMap()
        {
            Debug.WriteLine("LOADING MAP");
            var buttons = CanvasMain.Children.OfType<Button>().ToList();    // Find all objects of type button
            foreach (var button in buttons)
            {
                CanvasMain.Children.Remove(button);                         // Remove only buttons, that way this wont affect other children within CanvasMain eg: Grid
            }
            //MessageBox.Show("MAP"); //DEBUG
            //  List<DeviceDetails> ld = new List<DeviceDetails>();
            try
            {
                Pingers p = new Pingers();

                MyXML.GetObject(ref p.lanDeviceDetails, fileNameXML);    //-----<READ FILE>------
                int i = 100;
                int iPrint = 100;
                int topCount = 1;
                int mapWidth = 1;
                bool iterate = false;
                //Variables for the NetworkPerformance()
                int iterator = 0;
                float daley = 0;
                foreach (DeviceDetails dd in p.lanDeviceDetails)
                {
                    if (dd.Latency >= 0)
                    {
                        iterator++;
                        daley += dd.Latency / iterator;     // Average daley, pass this parameter to NetworkPerformance() when forloop finishes

                        /*   ThreadStart start = delegate()
                           {
                               Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<int, float>(NetworkPerformance), iterator, daley);
                           };
                           // Create the thread and kick it started! 
                           new Thread(start).Start();*/

                    }

                    if (monitorArray.Contains(dd.IpAddress)) { }  // Do not allow duplicates
                    else { monitorArray.Add(dd.IpAddress); }
                    switch (dd.Type)
                    {
                        case deviceType.PC:
                            Button myButton2 = new Button
                            {
                                Width = 70,
                                Height = 70,
                                Content = new Image
                                {
                                    Source = new BitmapImage(new Uri(@"Images/GUI_PC.png", UriKind.Relative)),
                                    VerticalAlignment = VerticalAlignment.Center,
                                    Stretch = Stretch.Fill
                                }
                            };
                            myButton2.Background = Brushes.Transparent;
                            myButton2.BorderBrush = Brushes.Transparent;
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
                                Height = 66,
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
                                Source = new BitmapImage(new Uri(@"Images/GUI_PC.png", UriKind.Relative))
                            };
                            CanvasMain.Children.Add(und);
                            Canvas.SetLeft(und, i);
                            break;
                    }
                }
                NetworkPerformance(iterator, daley);   // Calculate average network performance


            }
            catch (IOException ex)
            {
                Debug.WriteLine("\n-----<IOException>-----\nClass: MainWindow\nLoadDeviceMap\n" + ex.Message + "\n");
            }
        }
        private void Start_StopButton_Click(object sender, RoutedEventArgs e)
        {
            StartMonitoring();
        }
        private void StartMonitoring()
        {
            if (!click && enablePlayButton)          // Start monitoring
            {
                bContinueCapturing = true;
                Start_StopButton.Content = "Pause";
                click = true;
                LabelMonitor.Content = "Monitoring in progress...";
                timer.Start();
                StartListening();                   // Listen for broadcast and add to the list if new IP is found
            }
            else
            {
                Start_StopButton.Content = "Play";  // Stop monitoring
                click = false;
                LabelMonitor.Content = "Monitoring paused";
                timer.Stop();
                bContinueCapturing = false;
                StartListening();                   // Listen for broadcast and add to the list if new IP is found
                // RunTrafficReceiver(false);
            }
        }

        #region Broadcasts
        private void StartListening()
        {
            try
            {
                // MessageBox.Show("bContinueCapturing " + bContinueCapturing, "Network Mapping", MessageBoxButton.OK, MessageBoxImage.Asterisk); //DEBUG
                if (bContinueCapturing)
                {
                    //Start capturing the packets...
                    // bContinueCapturing = true;

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
                    if (mainSocket != null)
                    {
                        // bContinueCapturing = false;
                        //To stop capturing the packets close the socket
                        mainSocket.EndReceive(oldAsync);
                        mainSocket.Dispose();
                        mainSocket.Close();
                    }
                }
            }
            catch (SocketException ex)
            {
                MessageBox.Show("Run this application as an Administrator to have an access to all its features.", "Network Mapping", MessageBoxButton.OK, MessageBoxImage.Warning);
                Debug.WriteLine("\n-----<SocketException>-----\nStartListening()\n" + ex.Message + "\n");
            }
            catch (ObjectDisposedException ex)   // No msg box here, don't bother the user if he has not influence on the error
            {
                //MessageBox.Show(ex.Message + "\n\nStartListening()\nDisposed", "Network Mapping", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine("\n-----<ObjectDisposedException>-----\nStartListening()\n" + ex.Message + "\n");
            }
            catch (NullReferenceException ex)
            {
                //MessageBox.Show(ex.Message + "\n\nStartListening()\nDisposed", "Network Mapping", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine("\n-----<NullReferenceException>-----\nStartListening()\n" + ex.Message + "\n");
            }
        }
        private void OnReceive(IAsyncResult ar)
        {
            Thread.Sleep(10);
            try
            {
                int nReceived = mainSocket.EndReceive(ar);
                //Analyze the bytes received...

                ParseData(byteData, nReceived);

                if (bContinueCapturing)
                {
                    byteData = new byte[4096];

                    //Another call to BeginReceive so that we continue to receive the incoming packets
                    // currentAynchResult = mainSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None,
                    // new AsyncCallback(OnReceive), null);
                    oldAsync = mainSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None,
                        new AsyncCallback(OnReceive), null);
                }
            }
            catch (ObjectDisposedException ex)
            {
                // MessageBox.Show("OnReceive()\n"+ex.Message, "Network Mapping", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine("\n-----<ObjectDisposedException>-----\nOnReceive()\n" + ex.Message + "\n");
            }
            catch (Exception ex)
            {
                // MessageBox.Show("OnReceive()\n" + ex.Message, "Network Mapping", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine("\n-----<Exception>-----\nOnReceive()\n" + ex.Message + "\n");
            }
        }
        private void ParseData(byte[] byteData, int nReceived)
        {
            try
            {
                IPHeader ipHeader = new IPHeader(byteData, nReceived);

                if (ipHeader.DestinationAddress.ToString().Equals("224.0.0.2") || ipHeader.DestinationAddress.ToString().Equals("239.255.255.250"))    // Capute only broadcast messages
                {
                    Pingers p = new Pingers();
                    MyXML.GetObject(ref p.lanDeviceDetails, fileNameXML);    ///-----<READ FILE>------
                    lock (lockUpdateNet)
                    {
                        if (p.lanDeviceDetails.FindIndex(dev => dev.IpAddress.Equals(ipHeader.SourceAddress.ToString())) == -1)
                        {
                            p.AddToPingers(ipHeader.SourceAddress.ToString(), fileNameXML);
                            while (p.GetCompletedInstances <= 0) Thread.Sleep(100);
                            ThreadStart start = delegate()
                            {
                                CanvasMain.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(LoadDeviceMap));
                            };
                            new Thread(start).Start();
                        }
                        TrafficObjectsGrid tObjGrid = new TrafficObjectsGrid();
                        switch (ipHeader.ProtocolType)
                        {
                            case Protocol.TCP:
                                TCPHeader tcpHeader = new TCPHeader(ipHeader.Data,              //IPHeader.Data stores the data being 
                                    //carried by the IP datagram
                                                            ipHeader.MessageLength);
                                tObjGrid = new TrafficObjectsGrid(ipHeader.SourceAddress.ToString(), ipHeader.DestinationAddress.ToString(), tcpHeader.DestinationPort);
                                //  Debug.WriteLine("\n-----<Window Size>-----\n"+tcpHeader.WindowSize+"\nTTL:"+ipHeader.TTL);
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
                        Action dataGridWork = delegate
                        {
                            trafficList.Items.Add(tObjGrid);
                        };
                        trafficList.Dispatcher.BeginInvoke(DispatcherPriority.Background, dataGridWork);
                    }
                }

            }
            catch (ObjectDisposedException ex)
            {
                // MessageBox.Show(ex.Message, "Network Mapping", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine("\n-----<ObjectDisposedException>-----\nParseData()\n" + ex.Message + "\n");
            }
            catch (Exception ex)
            {
                // MessageBox.Show(ex.Message, "Network Mapping", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine("\n-----<Exception>-----\nParseData()\n" + ex.Message + "\n");
            }
        }
        #endregion

        private Action<object, RoutedEventArgs> MakeButtonClickHandler()
        {
            return (object sender, RoutedEventArgs e) =>
            {
                Button b = (Button)sender;
                Pingers p = new Pingers();
                MyXML.GetObject(ref p.lanDeviceDetails, fileNameXML);    ///-----<READ FILE>------
                String mac = null;
                long latency = 0;
                string OS = "Unknown";
                string description = "";
                foreach (DeviceDetails dd in p.lanDeviceDetails)
                {
                    if (dd.IpAddress.ToString() == b.Tag.ToString())
                    {
                        mac = dd.MacAddress;
                        latency = dd.Latency;
                        OS = dd.Os;
                        description = dd.Description;
                        /*  ToolTip tt = new ToolTip();
                        tt.Content = "Offline!";
                        b.ToolTip = tt;*/
                        //b.Background = Brushes.Red;
                        break;
                    }
                    else
                    {
                        mac = "Not found";
                        // latency = "Unknown";
                    }
                }
                MessageBox.Show("IP Address: " + b.Tag + "\nMac Address: " + mac + "\nLatency: " + latency + "ms" + "\nOS: " + OS + "\n" + description, b.Tag + " Details");
            };

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

        #region Settings Panel

        #region Shortcuts
        private void OpenCanExecute(object sender, CanExecuteRoutedEventArgs e)     // Enable command shortcuts
        {
            e.CanExecute = true;
            e.Handled = true;
        }
        private void OpenExecuted(object sender, ExecutedRoutedEventArgs e)         // Ctrl+O command event
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Text files (*.xml)|*.xml|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    //lbFiles.Items.Add(Path.GetFileName(filename));
                    //       MessageBox.Show("Path" + Path.GetFileName(filename));
                    // MessageBox.Show("Path" + Path.GetFullPath(filename));
                    string path = Path.GetFullPath(filename).ToString();
                    // MessageBox.Show("File opened from: " + t, "Opened", MessageBoxButton.OK, MessageBoxImage.Information);
                    networkInfoFile = "NetworkInfo.csv";        // Create this file in local directory
                    fileNameXML = path;
                    LoadDeviceMap();
                    BeginMonitoring();
                    break;
                }
            }
            e.Handled = true;
        }
        private void SaveCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }
        private void SaveExecuted(object sender, ExecutedRoutedEventArgs e)         // Ctrl+O command event
        {
            SaveFileDialog saveFile = new SaveFileDialog();
            saveFile.Filter = "Text files (*.xml)|*.xml";
            saveFile.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (saveFile.ShowDialog() == true)
            {
                foreach (string filename in saveFile.FileNames)
                {
                    MessageBox.Show("File saved in: " + Path.GetFullPath(filename), "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                    Pingers p = new Pingers();
                    MyXML.GetObject(ref p.lanDeviceDetails, fileNameXML);
                    MyXML.SaveObject(p.lanDeviceDetails, Path.GetFullPath(filename));
                }
            }

            e.Handled = true;
        }
        #endregion

        #region MonitorFrequency
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
        #endregion
        private void EnableSNMP(object sender, RoutedEventArgs e)
        {
            if ((bool)checkEnableSNMP.IsChecked)
            {
                scanWithSNMP = true;
            }
            else
            {
                scanWithSNMP = false;
            }
        }
        private void Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        #endregion

        #region Graph
        private void DrawGraph()
        {
            //  MessageBox.Show(DateTime.Now.ToLongTimeString()+"");
            var charts = plotter.Children.OfType<LineGraph>().ToList();     // Find all objects of type LineGraph

            foreach (var chart in charts)
            {
                plotter.Children.Remove(chart);                             // Remove only LineGraph
            }
            netInfoList = LoadNetInfo(networkInfoFile);                     // Load a file containing all the necessary information

            TimeSpan[] times = new TimeSpan[netInfoList.Count];
            int[] noOfDevices = new int[netInfoList.Count];
            int[] avgPing = new int[netInfoList.Count];

            for (int i = 0; i < netInfoList.Count; ++i)
            {
                times[i] = netInfoList[i].Time;
                noOfDevices[i] = netInfoList[i].NoOfDevices;
                avgPing[i] = netInfoList[i].AvgPing;
            }

            var timesDataSource = new EnumerableDataSource<TimeSpan>(times);
            timesDataSource.SetXMapping(x => timeAxis.ConvertToDouble(x));

            var devicesDataSource = new EnumerableDataSource<int>(noOfDevices);
            devicesDataSource.SetYMapping(y => Convert.ToDouble(y));

            var pingersDataSource = new EnumerableDataSource<int>(avgPing);
            pingersDataSource.SetYMapping(y => y);

            CompositeDataSource compositeDataSource1 = new CompositeDataSource(timesDataSource, devicesDataSource);
            CompositeDataSource compositeDataSource2 = new CompositeDataSource(timesDataSource, pingersDataSource);



            plotter.AddLineGraph(compositeDataSource1,
                      new Pen(Brushes.Blue, 2),     // Line color & thickness
                      new CirclePointMarker { Size = 5.0, Fill = Brushes.Orange },
                      new PenDescription("Number of Devices"));

            //plotter.AddLineGraph(compositeDataSource1, Colors.Red, 1, "Number Open");

            //Pen dashedPen = new Pen(Brushes.Magenta, 3);
            //dashedPen.DashStyle = DashStyles.DashDot;
            //plotter.AddLineGraph(compositeDataSource1, dashedPen, new PenDescription("Open bugs"));

            plotter.AddLineGraph(compositeDataSource2,
                     new Pen(Brushes.Green, 2),
                     new TrianglePointMarker { Size = 8.0, Pen = new Pen(Brushes.Black, 2.0), Fill = Brushes.GreenYellow },
                     new PenDescription("Ping"));
            plotter.LegendVisible = false;
            plotter.Viewport.FitToView();
        }
        private List<NetworkInfo> LoadNetInfo(string fileName)
        {
            var result = new List<NetworkInfo>();
            try
            {
                FileStream fs = new FileStream(fileName, FileMode.Open);
                StreamReader sr = new StreamReader(fs);

                string line = "";
                while ((line = sr.ReadLine()) != null)
                {
                    string[] pieces = line.Split(',');
                    TimeSpan d = TimeSpan.Parse(pieces[0]);
                    int numDevices = int.Parse(pieces[1]);
                    int averPing = int.Parse(pieces[2]);
                    NetworkInfo ni = new NetworkInfo(d, numDevices, averPing);
                    result.Add(ni);
                }
                sr.Close();
            }
            catch (IOException ex)
            {
                Debug.WriteLine("-----<IOException>-----\nLoadNetInfo()\n" + ex.Message);
            }
            catch (FormatException ex)
            {
                Debug.WriteLine("-----<FormatException>-----\nLoadNetInfo()\n" + ex.Message);
            }
            return result;
        }
        #endregion

        #region Register/Login

        public string parseUsername(string username)                  // Trim the username from krystianhoro@gmail.com to just krystianhoro
        {
            int lastIndex = 0;
            int i = 0;
            while ((i = username.IndexOf('@', i)) != -1)    // while didnt get to the end
            {
                lastIndex = i;  //Assing last index
                i++;
            }
            //Start removing from krystianhoro++
            return username.Remove(lastIndex);         //End result: krystianhoro        
        }
        private void bLogin_Click(object sender, RoutedEventArgs e)
        {
            if (bLogin.Content.Equals("Login"))
            {
                if (string.IsNullOrWhiteSpace(tbLogin.Text) || tbLogin.Text.Contains(" "))
                {

                }
                else
                {
                    Service service = new Service();
                    Task result = service.LoginUser(tbLogin.Text, bPassword.Password);      // Pass login and password to the website
                    blobUsername = parseUsername(tbLogin.Text);
                    if (service.getSuccess())
                    {
                        LoggedIn.Content = tbLogin.Text;
                        StackAnimation.IsEnabled = false;
                        saveCloud.IsEnabled = true;
                        openCloud.IsEnabled = true;
                    }
                }
            }
            else if (bLogin.Content.Equals("Register"))
            {
                if (string.IsNullOrWhiteSpace(tbLogin.Text) || tbLogin.Text.Contains(" "))
                {

                }
                else
                {
                    if (bPassword.Password.Equals(bConfirmPass.Password))
                    {
                        Service service = new Service();
                        Task result = service.RegisterUser(tbLogin.Text, bPassword.Password);
                        /*   if (service.getRegSuccess())  // Wont work since there is no await function
                           {*/
                        // Retrieve storage account from connection string.
                        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);

                        // Create the blob client.
                        CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                        try
                        {
                            blobUsername = parseUsername(tbLogin.Text);
                            // MessageBox.Show(parseUsername(tbLogin.Text));
                            // Retrieve a reference to a container. 
                            CloudBlobContainer container = blobClient.GetContainerReference(blobUsername);      // e.g. krystianhoro@gmail.com

                            // Create the container if it doesn't already exist.
                            container.CreateIfNotExists();

                            container.SetPermissions(new BlobContainerPermissions
                            {
                                PublicAccess =
                                    BlobContainerPublicAccessType.Blob
                            });
                        }
                        catch (StorageException ex)
                        {
                            Debug.WriteLine("\n-----<StorageException>-----\nClass: MainWindow\nbLogin_Click()\n" + ex.Message + "\n");
                        }

                        //   }

                    }
                }

            }
        }
        #endregion

        #region BLOB Storage
        private void SaveCloud(object sender, RoutedEventArgs e)
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            try
            {
                // Retrieve reference to a previously created container.
                CloudBlobContainer container = blobClient.GetContainerReference(blobUsername);

                // Retrieve reference to a blob named "myblob".
                CloudBlockBlob blockBlob = container.GetBlockBlobReference("myblob");
                // Create or overwrite the "myblob" blob with contents from a local file.
                using (var fileStream = System.IO.File.OpenRead(fileNameXML))
                {
                    blockBlob.UploadFromStream(fileStream);
                }
                MessageBox.Show("File " + fileNameXML + " uploaded", "Cloud", MessageBoxButton.OK);
            }
            catch (StorageException ex)
            {
                Debug.WriteLine("\n-----<StorageException>-----\nClass: MainWindow\nSaveCloud()\n" + ex.Message + "\n");
            }
            catch (ArgumentNullException ex)
            {
                Debug.WriteLine("\n-----<ArgumentNullException>-----\nClass: MainWindow\nSaveCloud()\n" + ex.Message + "\n");
            }
        }
        private void OpenCloud(object sender, RoutedEventArgs e)
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference(blobUsername);

            try
            {
                Debug.WriteLine("\n-----<ACCESSING BLOB>-----");
                // Loop over items within the container and output the length and URI.
                foreach (IListBlobItem item in container.ListBlobs(null, false))
                {
                    if (item.GetType() == typeof(CloudBlockBlob))
                    {
                        CloudBlockBlob blob = (CloudBlockBlob)item;

                        Debug.WriteLine("Block blob of length {0}: {1}", blob.Properties.Length, blob.Uri);

                    }
                }
                // Retrieve reference to a blob named "photo1.jpg".
                CloudBlockBlob blockBlob = container.GetBlockBlobReference("myblob");
                // Save blob contents to a file.
                using (var fileStream = System.IO.File.OpenWrite("CloudMap.xml"))
                {
                    blockBlob.DownloadToStream(fileStream);
                }
                fileNameXML = "CloudMap.xml";
                LoadDeviceMap();
                BeginMonitoring();
            }
            catch (StorageException ex)
            {
                Debug.WriteLine("\n-----<StorageException>-----\nClass: MainWindow\nSaveCloud()\n" + ex.Message + "\n");
            }
            catch (ArgumentNullException ex)
            {
                Debug.WriteLine("\n-----<ArgumentNullException>-----\nClass: MainWindow\nSaveCloud()\n" + ex.Message + "\n");
            }
        }
        #endregion

        #region Animations
        private void StackPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            Border sp = sender as Border;
            DoubleAnimation db = new DoubleAnimation();
            //db.From = 12;
            db.To = panelHeight;
            db.Duration = TimeSpan.FromSeconds(0.5);
            db.AutoReverse = false;
            db.RepeatBehavior = new RepeatBehavior(1);
            sp.BeginAnimation(StackPanel.HeightProperty, db);
        }

        private void StackPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            Border sp = sender as Border;
            DoubleAnimation db = new DoubleAnimation();
            //db.From = 12;
            db.To = 12;
            db.Duration = TimeSpan.FromSeconds(0.5);
            db.AutoReverse = false;
            db.RepeatBehavior = new RepeatBehavior(1);
            sp.BeginAnimation(StackPanel.HeightProperty, db);
            panelHeight = 165;
            borderCount = 0;
            bLogin.Content = "Login";
        }
        private void bRegister_Click(object sender, RoutedEventArgs e)
        {
            if (borderCount == 0)
            {
                bLogin.Content = "Register";
                panelHeight = 230;
                DoubleAnimation db = new DoubleAnimation();
                db.From = 165;
                db.To = panelHeight;
                db.Duration = TimeSpan.FromSeconds(0.5);
                db.AutoReverse = false;
                db.RepeatBehavior = new RepeatBehavior(1);
                BorderAnim.BeginAnimation(StackPanel.HeightProperty, db);
                borderCount++;
            }
            else
            {
                bLogin.Content = "Login";
                panelHeight = 165;
                DoubleAnimation db = new DoubleAnimation();
                db.From = 230;
                db.To = panelHeight;
                db.Duration = TimeSpan.FromSeconds(0.5);
                db.AutoReverse = false;
                db.RepeatBehavior = new RepeatBehavior(1);
                BorderAnim.BeginAnimation(StackPanel.HeightProperty, db);
                borderCount--;
            }
        }

        #endregion

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject  // Allows for search for specific item e.g. Buttons
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
        #region Compare Networks
        private void CompareNetworks(object sender, RoutedEventArgs e)
        {
            if (compareNetworks)
            {
                CanvasMain.Visibility = Visibility.Visible;                 // Make device map visible again
                GridWindow.Visibility = Visibility.Hidden;                  // Hide network comparison window
                compareNetworks = false;
            }
            else
            {
                CanvasMain.Visibility = Visibility.Hidden;                  // Hide device map
                GridWindow.Visibility = Visibility.Visible;                 // Make window with the graph and options for loading visible


                compareNetworks = true;
            }
        }
        private void LoadPerformance(object sender, RoutedEventArgs e)
        {
            string path = null;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select network " + performanceCounter + " performance";
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "Text files (*.csv)|*.csv";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    path = Path.GetFullPath(filename).ToString();
                    break;
                }
            }
            if (path != null)
            {
                if (performanceCounter == 1)                                                    // Open network 1 or network 2 file
                {
                    network1CSV = path;                                                         // Set network 1 file           
                    DrawPerformanceGraph(network1CSV, net1InfoList, performanceCounter);        // draw a graph for network 1
                    performanceCounter = 2;
                }
                else
                {
                    network2CSV = path;                                                         // Set network 2 file
                    DrawPerformanceGraph(network2CSV, net2InfoList, performanceCounter);        // draw a graph for network 2
                    performanceCounter = 1;
                }
            }
        }
        /*
         
                  var charts = plotterCompare.Children.OfType<LineGraph>().ToList();     // Find all objects of type LineGraph

            foreach (var chart in charts)
            {
                plotter.Children.Remove(chart);                             // Remove only LineGraph
            }
         * */
        private void DrawPerformanceGraph(string fileName, List<NetworkInfo> netInfoList, int network)
        {
            netInfoList = LoadNetInfo(fileName);                            // Load file for a network
            drawGraphDetails(netInfoList, network);                         // Draw a graph for net 1 or 2
        }

        private void drawGraphDetails(List<NetworkInfo> netInfoList, int network)
        {
            TimeSpan[] times = new TimeSpan[netInfoList.Count];
            int[] noOfDevices = new int[netInfoList.Count];
            int[] avgPing = new int[netInfoList.Count];

            for (int i = 0; i < netInfoList.Count; ++i)
            {
                times[i] = netInfoList[i].Time;
                noOfDevices[i] = netInfoList[i].NoOfDevices;
                avgPing[i] = netInfoList[i].AvgPing;
            }

            var timesDataSource = new EnumerableDataSource<TimeSpan>(times);
            timesDataSource.SetXMapping(x => timeAxis.ConvertToDouble(x));

            var devicesDataSource = new EnumerableDataSource<int>(noOfDevices);
            devicesDataSource.SetYMapping(y => Convert.ToDouble(y));

            var pingersDataSource = new EnumerableDataSource<int>(avgPing);
            pingersDataSource.SetYMapping(y => y);

            CompositeDataSource compositeDataSource1 = new CompositeDataSource(timesDataSource, devicesDataSource);
            CompositeDataSource compositeDataSource2 = new CompositeDataSource(timesDataSource, pingersDataSource);
            if (network == 1)
            {
                plotterCompare.AddLineGraph(compositeDataSource1,
              new Pen(Brushes.Blue, 2),     // Line color & thickness
              new CirclePointMarker { Size = 5.0, Fill = Brushes.Orange },
              new PenDescription("Number of Devices"));

                plotterCompare.AddLineGraph(compositeDataSource2,
                        new Pen(Brushes.Green, 2),
                        new TrianglePointMarker { Size = 8.0, Pen = new Pen(Brushes.Black, 2.0), Fill = Brushes.GreenYellow },
                        new PenDescription("Ping"));
                plotterCompare.LegendVisible = false;
                plotterCompare.Viewport.FitToView();
            }
            else
            {
                plotterCompare.AddLineGraph(compositeDataSource1,
                  new Pen(Brushes.Red, 2),     // Line color & thickness
                  new CirclePointMarker { Size = 5.0, Fill = Brushes.Orange },
                  new PenDescription("Number of Devices"));

                plotterCompare.AddLineGraph(compositeDataSource2,
                         new Pen(Brushes.Orange, 2),
                         new TrianglePointMarker { Size = 8.0, Pen = new Pen(Brushes.Black, 2.0), Fill = Brushes.Honeydew },
                         new PenDescription("Ping"));
                plotterCompare.LegendVisible = false;
                plotterCompare.Viewport.FitToView();
            }


        }
        #endregion

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
    public class MyWpfObject : DispatcherObject
    {
        public void DoSomething()
        {
            VerifyAccess();
            // Do some work 
        }
        public void DoSomethingElse()
        {
            if (CheckAccess())
            {
                // Something, only if called 
                // on the right thread 
            }
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