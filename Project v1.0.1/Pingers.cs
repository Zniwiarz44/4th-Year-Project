using System.Net.NetworkInformation;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System;
using System.Net;
using LanDevices;
using CurrentNetwork;
using System.Windows;
using System.ComponentModel;
using System.Net.Sockets;
using System.IO;

namespace Pingers_
{
    class Pingers
    {
        public List<DeviceDetails> lanDeviceDetails = new List<DeviceDetails>();
        private int instances = 0;
        private NetworkCollection networkList = new NetworkCollection();
        private bool gatewayFound = false;
        private static string defaultGateway;
        private List<Ping> pingers = new List<Ping>();
        private object @lock = new object();
        private object @lockSaving = new object();
      //  private int result = 0;
        private int timeOut = 900;
        private string filename = null;
        private static int ttl = 32;    // The data can go through 32 gateways or routers 
        private int completed = 0;      // Number of completed ping requests, 255 required
        public Pingers()
        {
            defaultGateway = networkList.getDefaultIPGateway();
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
        public void DestroyPingers()
        {
            foreach (Ping p in pingers)
            {
                p.PingCompleted -= Ping_completed;
                p.Dispose();
            }
            pingers.Clear();
        }
        public void Ping_completed(object s, PingCompletedEventArgs e)
        {
            lock (@lock)
            {
                instances -= 1;
                completed++;
            }
            if (e.Reply.Status == IPStatus.Success)
            {
                try
                { 
                    //  DeviceDetails dd = new DeviceDetails(e.Reply.Address.ToString(), Dns.GetHostEntry(e.Reply.Address.ToString()).HostName, deviceType.PC);  ENABLE THIS
                    /*  DeviceDetails dd = new DeviceDetails(e.Reply.Address.ToString(), "Unknown", deviceType.PC);
                      lanDeviceDetails.Add(dd);*/
                    //ENABLE THIS IF U WANT TO CONTINUE WITH HOST NAMES
                    long lat = e.Reply.RoundtripTime;
                    if ((e.Reply.Address.ToString() == defaultGateway) && (!gatewayFound))
                    {
                        DeviceDetails dd = new DeviceDetails(e.Reply.Address.ToString(), "Default Gateway", deviceType.Router, GetMacAddress(e.Reply.Address.ToString()), lat);
                        gatewayFound = true;
                        dd.Latency = lat;
                        lanDeviceDetails.Add(dd);
                    }
                    else
                    {                        
                        DeviceDetails dd = new DeviceDetails(e.Reply.Address.ToString(), "Unknown", deviceType.PC, GetMacAddress(e.Reply.Address.ToString()), lat);
                        dd.Latency = lat;
                        lanDeviceDetails.Add(dd);
                    }
                    lock (lockSaving)
                    {
                        MyXML.SaveObject(lanDeviceDetails, filename);
                    }
                }
                catch (SocketException ex)
                {
                    Debug.WriteLine("\n-----<SocketException>-----\nClass: Pingers\nParseData()\n" + ex.Message + "\n");
                }
                catch(IOException ex)
                {
                    Debug.WriteLine("\n-----<IOException>-----\nClass: Pingers\nParseData()\n" + ex.Message + "\n");
                }
               // result += 1;
            }
            else
            {
                //Console.WriteLine(String.Concat("Non-active IP: ", e.Reply.Address.ToString()));
            }
        
            
        }
        public void RunPingers(string baseIP, string file)
        {
            filename = file;
            string t = baseIP;
            CreatePingers(255);

            PingOptions po = new PingOptions(ttl, true);
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            byte[] data = enc.GetBytes("abababababababababababababababab");
            int cnt = 1;

            foreach (Ping p in pingers)
            {
                lock (@lock)                  //Critical Section make sure only 1 object does the call...
                {
                    instances += 1;
                }
                p.SendAsync((baseIP + cnt), timeOut, data, po);     // Send ping to e.g. 192.168.1.cnt
                cnt += 1;     
            }
        }
        public void AddDefaultGateway()
        {
            //FIND DEFAULT If its not in the same subnet
            if (!gatewayFound)
            {
                lock (@lock)    //Critical Section make sure only 1 object does the call...
                {
                    instances += 1;
                }
                Ping pi = new Ping();
                pi.PingCompleted += Ping_completed;
                PingOptions po2 = new PingOptions(ttl, true);
                System.Text.ASCIIEncoding enc2 = new System.Text.ASCIIEncoding();
                byte[] data2 = enc2.GetBytes("abababababababababababababababab");
                pi.SendAsync(networkList.getDefaultIPGateway(), timeOut, data2, po2);
                /* PingReply reply = pi.Send(networkList.getDefaultIPGateway(), timeOut, data2, po2);
                 long lat = reply.RoundtripTime;
                 DeviceDetails dd = new DeviceDetails(reply.Address.ToString(), "Default Gateway", deviceType.Router, GetMacAddress(reply.Address.ToString()), lat);
                 gatewayFound = true;
                 dd.Latency = lat;
                 lanDeviceDetails.Add(dd);
                 MyXML.SaveObject(lanDeviceDetails, "Devices.xml");*/
            }
        }
        public void AddToPingers(string ipAddress , string file)
        {
            filename = file;
            lock (@lock)    //Critical Section make sure only 1 object does the call...
            {
                instances += 1;
            }
            //MessageBox.Show("ADD");
            Ping pi = new Ping();
            pi.PingCompleted += Ping_completed;
            pingers.Add(pi);
            PingOptions po2 = new PingOptions(ttl, true);
            System.Text.ASCIIEncoding enc2 = new System.Text.ASCIIEncoding();
            byte[] data2 = enc2.GetBytes("abababababababababababababababab");
            pi.SendAsync(ipAddress, timeOut, data2, po2);
          }
        [System.Runtime.InteropServices.DllImport("iphlpapi.dll", ExactSpelling = true)]
        static extern int SendARP(uint DestIP, int SrcIP, byte[] pMacAddr, ref int PhyAddrLen);

        public string GetMacAddress(string ipAddress)
        {
            IPAddress dst = IPAddress.Parse(ipAddress);

            uint uintAddress = BitConverter.ToUInt32(dst.GetAddressBytes(), 0);
            byte[] macAddr = new byte[6];
            int macAddrLen = macAddr.Length;
            int retValue = SendARP(uintAddress, 0, macAddr, ref macAddrLen);
            if (retValue != 0)
            {
                Console.WriteLine("Failed to send arp request");
            }

            string[] str = new string[(int)macAddrLen];
            for (int i = 0; i < macAddrLen; i++)
                str[i] = macAddr[i].ToString("x2");

            return string.Join(":", str);
        }

 
    }
}
