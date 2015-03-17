using NETWORKLIST;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management;
using System.Management.Automation;
using System.Net;
using System.Net.Sockets;
using Pingers_;

namespace CurrentNetwork
{
    class NetworkStatus
    {
        private String name;
        private bool state;     //True netwrok up, false network down
        
        public NetworkStatus()
        {
            name = "0.0.0.0";
            state = false;
        }
        public NetworkStatus(String name, bool state)
        {
            Name = name;
            State = state;
        }
        public String Name
        {
            get
            {
                return name;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                    throw new ArgumentException("Network name must not be empty");
                else
                    name = value;
            }
        }
        public bool State
        {
            get
            {
                return state;
            }
            set
            {
                state = value;
            }
        }

    }
    class NetworkCollection : IEnumerable<NetworkStatus>
    {
        private String ipAddress = null;
        private static List<NetworkStatus> networkStatus = new List<NetworkStatus>();

        public NetworkCollection()
        {

        }
        public Collection<NetworkStatus> NetworkStatusList
        {
            get
            {
                return new Collection<NetworkStatus>(networkStatus);
            }
        }
        public String GetCurrentNetwork()
        {
            String networkName = null;
            var manager = new NetworkListManager();
            var connectedNetworks = manager.GetNetworks(NLM_ENUM_NETWORK.NLM_ENUM_NETWORK_CONNECTED).Cast<INetwork>();
            foreach (var network in connectedNetworks)
            {
                if (String.IsNullOrEmpty(network.GetName()))
                {
                    networkName = "Network not found";
                }
                else
                {
                    networkName = "" + network.GetName();   //Improve this        
                    break;
                }
            }
        /*    if (String.IsNullOrEmpty(networkName))
                networkName = "Network not found";*/

            return networkName;
        }
        public void AddNetworkStatus(string addres, bool state)   //Add Remove later
        {
            NetworkStatus netStat = new NetworkStatus(addres, state);
            if(networkStatus == null)
            {
                networkStatus.Add(netStat);
            }
            else
            {
                networkStatus.Add(netStat);
            }
        }
        public void RemoveNetworkStatus()
        {
            if(networkStatus == null)
            {

            }
            else
            {
                foreach (NetworkStatus netS in networkStatus)
                {
                    networkStatus.Remove(netS);
                }
            }         
        }
        //Iterate over networks
        public IEnumerator<NetworkStatus> GetEnumerator()
        {
            foreach (NetworkStatus netS in networkStatus)
            {
                yield return netS;
            }
        }
        // Implementation for the GetEnumerator method.
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }
        public string getNetworkAddress()
        {
            NetworkCollection networkList = new NetworkCollection();
            try
            {
                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT * FROM Win32_NetworkAdapterConfiguration");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    if (queryObj["IPAddress"] == null) 
                    {
                        ipAddress = "0.0.0.0";
                    }
                    else
                    {
                        String[] arrIPAddress = (String[])(queryObj["IPAddress"]);
                        foreach (String arrValue in arrIPAddress)
                        {
                            ipAddress = arrValue;
                            return arrValue;
                        }
                        break;
                    }
                }
            }
            catch (ManagementException e)
            {
                //MessageBox.Show("An error occurred while querying for WMI data: " + e.Message);
            }
            return ipAddress;
        }
        public string getDefaultIPGateway()
        {
            NetworkCollection networkList = new NetworkCollection();
            try
            {
                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT * FROM Win32_NetworkAdapterConfiguration");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    if (queryObj["DefaultIPGateway"] == null) { return "0.0.0.0"; }
                    else
                    {
                        String[] arrIPAddress = (String[])(queryObj["DefaultIPGateway"]);
                        foreach (String arrValue in arrIPAddress)
                        {
                      //      Console.WriteLine("DefaultIPGateway: {0}", arrValue);
                            //  parseIPAddress(arrValue);   //Pass the ip address to this method
                            ipAddress = arrValue;
                            return arrValue;
                        }
                    }
                    break;
                }
            }
            catch (ManagementException e)
            {
                //MessageBox.Show("An error occurred while querying for WMI data: " + e.Message);
            }
            return "0.0.0.0";
        }
        public string getMyIpAddress()
        {
            return ipAddress;
        }
        public void parseIPAddress(string ipAddress)
        {
            int lastIndex = 0;
            int i = 0;
            while ((i = ipAddress.IndexOf('.', i)) != -1)    //while didnt get to the end
            {
                lastIndex = i;  //Assing last index
                i++;
            }
            //Start removing from 192.168.1.++
            String result = ipAddress.Remove(lastIndex + 1);  //End result: 192.168.1.
            Pingers p = new Pingers();
            p.RunPingers(result);        
        }     
    }
}