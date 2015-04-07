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
        private int counter = 0;                            // Used to determine if button was clicked
        private int monitorFrequency = 10;                  // 20sec
        private int inactiveAttempt = 3;                    // Number of attempts to see if device is online or not
        private int mapCounter = 1;                         // Can be either 0 or 1, it keeps track of number of calls for PrepareForMonitoring()

        private const int RESPOND_TIME = 65;                // On average routers need 60sec to restart, so if some devices are down check in 65sec if they are active

        private bool firstIteration = true;
        private bool click = false;                         // Play/Stop button
        private bool enablePlayButton = false;              // Determines when Play button is available
        private bool bContinueCapturing = false;            // A flag to check if packets are to be captured or not

        private string defaultIpGateway = null;             // Set default gateway
        private string networkName = null;                  // Set network name
        private string fileNameXML = null;                  // XML file name
        private string myIpAddress = null;                  // Set IP address of current machine

        private static List<string> monitorArray = new List<string>();  // Contains list of ip addresses
        private List<string> inactiveDevices = new List<string>();      // When device becomes offline is added to this list to perform a check with inactiveCheck()

        private DispatcherTimer timer = new DispatcherTimer();          // Timer for frequent monitoring
        private DispatcherTimer timerInactive = new DispatcherTimer();  // Timer for inactive devices

        private Object lockUpdateNet = new Object();
        private Object lockCounter = new Object();
        private Object lockFileRead = new Object();

        private Socket mainSocket;                                      // The socket which captures all incoming packets
        private byte[] byteData = new byte[4096];
        private EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Broadcast, 1900);
        private delegate void AddTrafficList(TrafficObjectsGrid gridObj);

        private IAsyncResult oldAsync;

        private readonly BackgroundWorker worker = new BackgroundWorker();  // This thread performs an ip sweep and at the same time keep the program responsive.
        private readonly BackgroundWorker worker2 = new BackgroundWorker(); // Worker for adding new ip addresses

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
            fileNameXML = "Devices.xml";
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
            /*  System.Net.NetworkInformation.IPGlobalProperties network = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
              System.Net.NetworkInformation.TcpConnectionInformation[] connections = network.GetActiveTcpConnections();
        
              foreach (System.Net.NetworkInformation.TcpConnectionInformation connection in connections)
              {
                  connection.RemoteEndPoint.AddressFamily;                
              }*/
        }
        private void PrepareForMonitoring()
        {
            Pingers p = new Pingers();
            var buttons = CanvasMain.Children.OfType<Button>().ToList();            //Find all objects of type button
            foreach (var button in buttons)
            {
                CanvasMain.Children.Remove(button);                     // Remove only buttons, that way this wont affect other children within CanvasMain eg: Grid
            }
            monitorArray.Clear();                                       // Remove all elements from the array every time scan runs to avoid unnecessary traffic on the network (Duplicate ip's)

            #region FindDefaultGateway
            //   Pingers p = new Pingers();
            bool foundDefault = false;
            MyXML.GetObject(ref p.lanDeviceDetails, fileNameXML);    //-----<READ FILE>------
            foreach (DeviceDetails dd in p.lanDeviceDetails)
            {
                if (dd.IpAddress.Equals(defaultIpGateway))
                {
                    foundDefault = true;
                    break;
                }
            }
            if (!foundDefault)
            {
                //MessageBox.Show("DEFAULT");   //Debug
                p.AddDefaultGateway();
            }
            #endregion
            ScanButton.Content = "Scan";
            LoadDeviceMap();
            BeginMonitoring();
            //   firstIteration = true;
        }
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Pingers p = new Pingers();
            p.RunPingers(parseIPAddress(myIpAddress), fileNameXML);     // Perform ping sweep for your IP address /24
            while (p.GetCompletedInstances < 255) ;                     // Make sure all ping requests are processed
            p.DestroyPingers();                                         // Free up the memory once ping sweep has ben completed
            Debug.WriteLine("Completed requests: {0}", p.GetCompletedInstances);
        }
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ScanButton.IsEnabled = true;                // Enable scan button
            ScanProgress.Content = "Scan complete";
            if (mapCounter <= 0)                        // Enter this section only Once, when the scan is complete this fixes an issue with multiple PrepareForMonitoring() calls
            {
                lock (lockCounter)
                {
                    mapCounter++;
                }
                PrepareForMonitoring();
            }
        }
        private void BeginMonitoring()
        {
            LabelMonitor.Content = "Monitoring in progress...";
            Start_StopButton.Content = "Pause";
            enablePlayButton = true;
            click = false;
            StartMonitoring();                                         // Listen for broadcast and add to the list if new IP is found
            InitializeComponent();
            // StartListening();          
            //   DispatcherTimer timer = new DispatcherTimer();
            if (timer.IsEnabled)
            {
                timer.Stop();
            }
            timer.Interval = TimeSpan.FromSeconds(monitorFrequency);  //Replace 20 with specified input fro Settings > Monitoring > Monitoring Frequency
            timer.Tick += timer_Tick;                                 // WARNING, ENTER THIS ONLY ONCE
            timer.Start();
            // LabelMonitor.Content = "Monitoring in progress..."+ timer.Interval.Seconds;
        }
        private void timer_Tick(object sender, EventArgs e)
        {

            Pingers p = new Pingers();
            MyXML.GetObject(ref p.lanDeviceDetails, fileNameXML);     ///-----<READ FILE>------
            foreach (DeviceDetails dd in p.lanDeviceDetails)            // Update latency on each device
            {
                if (monitorArray.Contains(dd.IpAddress)) { }            // Do not allow duplicates
                else { monitorArray.Add(dd.IpAddress); }
            }

            MonitorNetwork mn = new MonitorNetwork();
            // String ip = parseIPAddress(MyIpAddress.Content.ToString()); // Cut the last octet of IP address
            mn.RunPingers(monitorArray, fileNameXML);                                // Send heart beat packets to known devices MonitorNetwork.cs

            //  grdEmployee.Items.Clear();

            foreach (NetworkStatus ns in mn.NetworkMonitorList)
            {
                grdEmployee.Items.Add(ns);                              // Populate the grid in MainWindow.xml with ipaddresses and their status
            }

            updateDevices();
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

            if (inactiveDevices.Count > 0)
            {
                inactiveCheck();
            }

            // grdEmployee.Items.Add(nList.Current);
            mn.RemoveNetworkStatus();   // Clear the buffer and get ready to capute new status.

            /*   lock(trafficList)
               {
                   trafficList.Items.Refresh();
               }  */

        }
        private void updateDevices()
        {
            Pingers p = new Pingers();
            MyXML.GetObject(ref p.lanDeviceDetails, fileNameXML);

            //Variables for the NetworkPerformance()
            int iterator = 1;
            float daley = 0;

            foreach (DeviceDetails dd in p.lanDeviceDetails)            // Update latency on each device
            {
                if (dd.Latency >= 0)
                {
                    daley += dd.Latency / iterator;
                    NetworkPerformance(iterator, daley);
                    /*   ThreadStart start = delegate()
                       {
                           Dispatcher.Invoke(DispatcherPriority.Background, new Action<int, float>(NetworkPerformance), iterator, daley);
                       };
                       // Create the thread and kick it started! 
                       new Thread(start).Start();*/
                    iterator++;
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

            MessageBox.Show("REQUEST att: " + inactiveAttempt);
            inactiveAttempt--;
            if (inactiveAttempt <= 0)
            {
                timerInactive.Stop();
                inactiveAttempt = 3;
            }
        }
        public void inactiveCheck()     // Check if devices are still inactive
        {

            InitializeComponent();
            timerInactive.Interval = TimeSpan.FromSeconds(RESPOND_TIME);  // Check if devices will respond
            timerInactive.Tick += timerInactive_Tick;
            if (timerInactive.IsEnabled)
            { }
            else { timerInactive.Start(); MessageBox.Show("REQUEST2"); }

        }
        public string parseIPAddress(string ipAddress)       // Trim the ip address
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
        }
        private void LoadDeviceMap()
        {
            Console.WriteLine("LOADING MAP");
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
                int iterator = 1;
                float daley = 0;
                foreach (DeviceDetails dd in p.lanDeviceDetails)
                {
                    if (dd.Latency >= 0)
                    {
                        daley += dd.Latency / iterator;
                        NetworkPerformance(iterator, daley);   // Calculate average network performance
                        /*   ThreadStart start = delegate()
                           {
                               Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<int, float>(NetworkPerformance), iterator, daley);
                           };
                           // Create the thread and kick it started! 
                           new Thread(start).Start();*/
                        iterator++;
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
            }
            catch (IOException ex)
            {
                Debug.WriteLine("\n-----<IOException>-----\nClass: MainWindow\nLoadDeviceMap\n" + ex.Message + "\n");
            }
        }

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
                    MessageBox.Show("File opened from: " + Path.GetFullPath(filename), "Opened", MessageBoxButton.OK, MessageBoxImage.Information);
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
                StartListening();               // Listen for broadcast and add to the list if new IP is found
                /*     ThreadStart startListen = delegate()
                       {
                           Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(StartListening));
                       };
                     // Create the thread and kick it started! 
                     new Thread(startListen).Start();*/
                //   StartListening();               // Listen for broadcast and add to the list if new IP is found
                // RunTrafficReceiver(true);
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

        #region Animations
        private void StackPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            Border sp = sender as Border;
            DoubleAnimation db = new DoubleAnimation();
            //db.From = 12;
            db.To = 175;
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
                foreach (DeviceDetails dd in p.lanDeviceDetails)
                {
                    if (dd.IpAddress.ToString() == b.Tag.ToString())
                    {
                        mac = dd.MacAddress;
                        latency = dd.Latency;
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
                MessageBox.Show("IP Address: " + b.Tag + "\nMac Address: " + mac + "\nLatency: " + latency + "ms", b.Tag + " Details");
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