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
        private int timeOut = 999;
        // public List<NetworkStatus> monitorStatus = new List<NetworkStatus>();   //Read this list in mainWindow to change the img of devices that are not responding
        private static int ttl = 5;
        NetworkCollection nc = new NetworkCollection();
        private static List<NetworkStatus> networkMonitorList = new List<NetworkStatus>();
        private String tempIpAddress = "0.0.0.0";   //If reply fail use temp address (set before ping is sent), otherwise reply will be set to 0.0.0.0
        public MonitorNetwork()
        {
            //  networkMonitorList 
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
            lock (@lock)
            {
                instances -= 1;
            }
            if (networkMonitorList.FindIndex(dev => dev.Name.Equals(e.Reply.Address.ToString())) == -1)
            {
           
                if (e.Reply.Status == IPStatus.Success)
                {
                    /*This is possibly wrong, the array should be private and be accessed in NetworkStatus class, here using Add...() method*/
                    // NetworkStatus ns = new NetworkStatus(e.Reply.Address.ToString(), true);     //Device is responding
                    // monitorStatus.Add(ns);
                     long lat = e.Reply.RoundtripTime;
                     Pingers p = new Pingers();
                     MyXML.GetObject(ref p.lanDeviceDetails, "Devices.xml");    ///-----<READ FILE>------
                     foreach (DeviceDetails dd in p.lanDeviceDetails)
                     {
                         if(dd.IpAddress.Equals(e.Reply.Address.ToString()))
                         {
                             dd.Latency = lat;
                             MyXML.SaveObject(p.lanDeviceDetails, "Devices.xml");
                             break;
                         }
                     }
                    /*     DeviceDetails dd = new DeviceDetails();
                     dd = p.lanDeviceDetails.Find(w => w.IpAddress.Equals(e.Reply.Address));//Make a copy
                     int index = p.lanDeviceDetails.FindIndex(w => w.IpAddress.Equals(e.Reply.Address));//Get index of copied object
                     p.lanDeviceDetails.RemoveAt(index); //Remove old index
                     dd.Latency = e.Reply.RoundtripTime; //set new latency
                     p.lanDeviceDetails.Add(dd); //Add the object back to the list
                     //  nc.AddNetworkStatus(e.Reply.Address.ToString(), true);*/
                    NetworkStatus ns = new NetworkStatus(e.Reply.Address.ToString(), true);
                    networkMonitorList.Add(ns);
                }
                else
                {
                    long lat = -1;
                    Pingers p = new Pingers();
                    MyXML.GetObject(ref p.lanDeviceDetails, "Devices.xml");    ///-----<READ FILE>------
                    foreach (DeviceDetails dd in p.lanDeviceDetails)
                    {
                        if (dd.IpAddress.Equals(tempIpAddress))
                        {
                            dd.Latency = lat;
                            MyXML.SaveObject(p.lanDeviceDetails, "Devices.xml");
                            break;
                        }
                    }
                    //  NetworkStatus ns = new NetworkStatus(e.Reply.Address.ToString(), false);    //Device is not responding
                    // monitorStatus.Add(ns);
                    //    nc.AddNetworkStatus(e.Reply.Address.ToString(), false);
                    NetworkStatus ns = new NetworkStatus(tempIpAddress, false);
                    networkMonitorList.Add(ns);
                }
            }
         
  
        }
        public void RunPingers(string baseIP, List<int> monitorArray)
        {
            // nc.RemoveNetworkStatus();
            string t = baseIP;
            int cnt = 0;
            foreach (int value in monitorArray)
            {
                cnt++;
            }
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
                tempIpAddress = (baseIP + monitorArray[index]);     //set ip for temp in case reply fails (if it fails ip = 0.0.0.0)
                p.SendAsync((baseIP + monitorArray[index]), timeOut, data, po);
                index++;
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
