using System.Net.NetworkInformation;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System;
using System.Net;
using CurrentNetwork;
using System.Collections;
using System.Collections.ObjectModel;
using Pingers_;
using LanDevices;
namespace MonitorNetwork_
{
    /*This class is responsible for gathering the data about the LAN, it checks if devices are (alive) with default frequency of 60sec*/
    class MonitorNetwork
    {
        private int instances = 0;
        private List<Ping> pingers = new List<Ping>();
        private object @lock = new object();
        private int timeOut = 900;
        // public List<NetworkStatus> monitorStatus = new List<NetworkStatus>();   //Read this list in mainWindow to change the img of devices that are not responding
        private static int ttl = 5;
        NetworkCollection nc = new NetworkCollection();
        private static List<NetworkStatus> networkMonitorList = new List<NetworkStatus>();
        private String tempIpAddress = "0.0.0.0";   //If reply fail use temp address (set before ping is sent), otherwise reply will be set to 0.0.0.0
        private string filename;
        private int completed = 0;
        private object @lockSaving = new object();
        public MonitorNetwork()
        {
            //  networkMonitorList 
        }
        public int GetCompletedInstances
        {
            get { return completed; }
        }

        private void CreatePingers(int cnt)
        {
            for (int i = 1; i <= cnt; i++)
            {
                Ping p = new Ping();
                p.PingCompleted += Ping_completed;
                pingers.Add(p);
            }
        }
        public void Ping_completed(object s, PingCompletedEventArgs e)
        {
            var token = (DeviceDetails)e.UserState;            
            lock (@lock)
            {
                instances -= 1;

                if (networkMonitorList.FindIndex(dev => dev.Name.Equals(e.Reply.Address.ToString())) == -1) //Avoid duplicates
                {
                    if (e.Reply.Status == IPStatus.Success)
                    {
                        /*This is possibly wrong, the array should be private and be accessed in NetworkStatus class, here using Add...() method*/
                        // NetworkStatus ns = new NetworkStatus(e.Reply.Address.ToString(), true);     //Device is responding
                        // monitorStatus.Add(ns);
                        long lat = e.Reply.RoundtripTime;
                        Pingers p = new Pingers();
                        Debug.WriteLine("Class: MonitorNetwork Ping_completed\nGetObject");
                        MyXML.GetObject(ref p.lanDeviceDetails, filename);    ///-----<READ FILE>------
                        foreach (DeviceDetails dd in p.lanDeviceDetails)
                        {
                            if (dd.IpAddress.Equals(e.Reply.Address.ToString()))
                            {
                                dd.Latency = lat;
                                lock (@lockSaving)
                                {
                                    Debug.WriteLine("Class: MonitorNetwork Ping_completed\nSaveObject");
                                    MyXML.SaveObject(p.lanDeviceDetails, filename);
                                }
                                break;
                            }
                        }
                        NetworkStatus ns = new NetworkStatus(e.Reply.Address.ToString(), "Online");
                        networkMonitorList.Add(ns);
                    }
                    else
                    {
                        long lat = -1;
                        Pingers p = new Pingers();
                        Debug.WriteLine("Class: MonitorNetwork Ping_completed else\nGetObject");
                        MyXML.GetObject(ref p.lanDeviceDetails, filename);    ///-----<READ FILE>------
                        Debug.WriteLine("\n-----<_TOKEN_>-----\n" + token.IpAddress);
                        foreach (DeviceDetails dd in p.lanDeviceDetails)
                        {
                            if (dd.IpAddress.Equals(token.IpAddress))
                            {
                                dd.Latency = lat;
                                lock (@lockSaving)
                                {
                                    Debug.WriteLine("Class: MonitorNetwork Ping_completed else\nSaveObject");
                                    MyXML.SaveObject(p.lanDeviceDetails, filename);     // Update the latency in the file
                                }
                                break;
                            }
                        }
                        //  NetworkStatus ns = new NetworkStatus(e.Reply.Address.ToString(), false);    //Device is not responding
                        // monitorStatus.Add(ns);
                        //    nc.AddNetworkStatus(e.Reply.Address.ToString(), false);
                        NetworkStatus ns = new NetworkStatus(token.IpAddress, "Offline");
                        networkMonitorList.Add(ns);
                    }
                }
            }
            completed++;
        }
        public void RunPingers(List<string> monitorArray, string file)
        {
            // nc.RemoveNetworkStatus();
            //  string t = baseIP;
            filename = file;
            int cnt = monitorArray.Count;
            if (cnt > 0)                             // Catch null exception
            {
                CreatePingers(cnt);

                PingOptions po = new PingOptions(ttl, true);
                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                byte[] data = enc.GetBytes("abababababababababababababababab");

                int index = 0;

                foreach (Ping p in pingers)
                {
                    lock (@lock)    //Critical Section make sure only 1 object does the call...
                    {
                        instances += 1;
                    }
                    try
                    {
                        DeviceDetails token = new DeviceDetails();
                        token.IpAddress = monitorArray[index];
                        // ERROR HERE, temp will not work, send > wait for reply > send another one thats the only way
                        Debug.WriteLine("Class: MonitorNetwork.cs Sending ping " + monitorArray[index]);
                        tempIpAddress = (monitorArray[index]);     //set ip for temp in case reply fails (if it fails ip = 0.0.0.0)
                        p.SendAsync((monitorArray[index]), timeOut, data, po, token);

                        index++;
                    }
                    catch (ArgumentException ex)
                    {
                        completed++;
                        Debug.WriteLine("\n-----<ArgumentException>-----\nClass: MonitorNetwork\nRunPingers()\n" + ex.Message + "\n");
                    }
                }
            }
        }
        public Collection<NetworkStatus> NetworkMonitorList
        {
            get
            {
                return new Collection<NetworkStatus>(networkMonitorList);
            }
        }
        public void RemoveNetworkStatus()
        {
            if (networkMonitorList.Count <= 0)
            {

            }
            else
            {
                networkMonitorList.Clear();
            }
        }
    }
}
