using System.Net.NetworkInformation;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System;
using System.Net;
using LanDevices;
using CurrentNetwork;

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
        private int result = 0;
        private int timeOut = 500;

        private static int ttl = 32; // The data can go through 64 gateways or routers 
        public Pingers()
        {
            defaultGateway = networkList.getDefaultIPGateway();
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

        private void DestroyPingers()
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
                }
                catch (System.Net.Sockets.SocketException)
                {
                    //    Console.Write(e.Reply.Address.ToString() + "\tHostname not available\n");
                    /*    if ((e.Reply.Address.ToString() == defaultGateway) && (gatewayFound == false))
                        {
                            DeviceDetails dd = new DeviceDetails(e.Reply.Address.ToString(), "Default Gateway", deviceType.Router);
                            gatewayFound = true;
                            lanDeviceDetails.Add(dd);
                        }
                        else
                        {
                            DeviceDetails dd = new DeviceDetails(e.Reply.Address.ToString(), "Unknown", deviceType.PC);
                            lanDeviceDetails.Add(dd);
                        }   */
                }
                result += 1;
            }
            else
            {
                //Console.WriteLine(String.Concat("Non-active IP: ", e.Reply.Address.ToString()));
            }
            MyXML.SaveObject(lanDeviceDetails, "Devices.xml");
        }
        public void RunPingers(string baseIP)
        {
            string t = baseIP;
            CreatePingers(255);

            PingOptions po = new PingOptions(ttl, true);
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            byte[] data = enc.GetBytes("abababababababababababababababab");

            int cnt = 1;

            foreach (Ping p in pingers)
            {
                lock (@lock)    //Critical Section make sure only 1 object does the call...
                {
                    instances += 1;
                }
                p.SendAsync((baseIP + cnt), timeOut, data, po);
                cnt += 1;
            }//FIND DEFAULT If its not in the same subnet
            if (!gatewayFound)
            {
                lock (@lock)    //Critical Section make sure only 1 object does the call...
                {
                    instances += 1;
                }
                Ping pi = new Ping();
                pi.PingCompleted += Ping_completed;
                pingers.Add(pi);
                PingOptions po2 = new PingOptions(ttl, true);
             //   pi.SendAsync(networkList.getDefaultIPGateway(), timeOut, data, po2);
            }
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
