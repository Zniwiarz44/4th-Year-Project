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
        private string defaultGateway;
        private List<Ping> pingers = new List<Ping>();
        private object @lock = new object();
        private int result = 0;
        private int timeOut = 500;

        private static int ttl = 5;
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
              //  Console.Write(e.Reply.Address.ToString() + "\tHostname not available\n");
                //  Console.Write(string.Concat("Active IP: ", e.Reply.Address.ToString()));
                try
                {       
                    DeviceDetails dd = new DeviceDetails(e.Reply.Address.ToString(), Dns.GetHostEntry(e.Reply.Address.ToString()).HostName, deviceType.PC);
                    lanDeviceDetails.Add(dd);
                }
                catch (System.Net.Sockets.SocketException)
                {
                //    Console.Write(e.Reply.Address.ToString() + "\tHostname not available\n");
                    if ((e.Reply.Address.ToString() == defaultGateway) && (gatewayFound == false))
                    {
                        DeviceDetails dd = new DeviceDetails(e.Reply.Address.ToString(), "Default Gateway", deviceType.Router);
                        gatewayFound = true;
                        lanDeviceDetails.Add(dd);
                    }
                    else
                    {
                        DeviceDetails dd = new DeviceDetails(e.Reply.Address.ToString(), "Unknown", deviceType.PC);
                        lanDeviceDetails.Add(dd);
                    }    
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
           // Console.WriteLine("Pinging 255 destinations of D-class in {0}*", baseIP);
            string t = baseIP;
            CreatePingers(255);

            PingOptions po = new PingOptions(ttl, true);
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            byte[] data = enc.GetBytes("abababababababababababababababab");

          //  SpinWait wait = new SpinWait();
            int cnt = 1;

           // Stopwatch watch = Stopwatch.StartNew();

            foreach (Ping p in pingers)
            {
                lock (@lock)    //Critical Section make sure only 1 object does the call...
                {
                    instances += 1;
                }

                p.SendAsync((baseIP+cnt), timeOut, data, po);
                cnt += 1;
            }
          
           /* while (instances > 0)
            {
                wait.SpinOnce();
            }*/
           
            
            // watch.Stop();
            //DestroyPingers();

            //Console.WriteLine("Finished in {0}. Found {1} active IP-addresses.", watch.Elapsed.ToString(), result);
         //   Console.WriteLine("Found {0} active IP-addresses.", result);
            // Console.WriteLine(Dns.GetHostEntry("192.168.1.1").HostName);

        //    Console.ReadKey();

        }
    }
}
