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
        private List<NetworkStatus> networkStatus = new List<NetworkStatus>();

        public Collection<NetworkStatus> NettworkStatusList
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
                networkName = "" + network.GetName();
            }
            if (String.IsNullOrEmpty(networkName))
                networkName = "Network not found";

            return networkName;
        }
        public void AddNetwork()   //Add Remove later
        {
            var manager = new NetworkListManager();
            var connectedNetworks = manager.GetNetworks(NLM_ENUM_NETWORK.NLM_ENUM_NETWORK_ALL).Cast<INetwork>();
            var net = manager.GetNetworkConnections();
            var connectedNetworks2 = manager.GetNetworks(NLM_ENUM_NETWORK.NLM_ENUM_NETWORK_CONNECTED).Cast<INetwork>();
            foreach (var network in connectedNetworks)
            {
                /*   Console.Write(network.GetName() + " ");
                   var cat = network.GetCategory();
                   if (network.IsConnected)
                       Console.WriteLine("--<Connected>--");
                   if (cat == NLM_NETWORK_CATEGORY.NLM_NETWORK_CATEGORY_PRIVATE)
                       Console.WriteLine("[PRIVATE]");
                   else if (cat == NLM_NETWORK_CATEGORY.NLM_NETWORK_CATEGORY_PUBLIC)
                       Console.WriteLine("[PUBLIC]");
                   else if (cat == NLM_NETWORK_CATEGORY.NLM_NETWORK_CATEGORY_DOMAIN_AUTHENTICATED)
                       Console.WriteLine("[DOMAIN]");*/
                NetworkStatus netStat = new NetworkStatus(network.GetName(), network.IsConnected);
                if (netStat == null)
                {
                    networkStatus.Add(netStat);
                }
                else
                {
                    if (networkStatus.Exists(ns => ns.Name == netStat.Name))
                    {
                        throw new ArgumentException(netStat.Name + " already exists");
                    }
                    else
                    {
                        networkStatus.Add(netStat);
                    }
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
                    if (queryObj["IPAddress"] == null) { return "0.0.0.0"; }
                    else
                    {
                        String[] arrIPAddress = (String[])(queryObj["IPAddress"]);
                        foreach (String arrValue in arrIPAddress)
                        {
                          //  Console.WriteLine("IPAddress: {0}", arrValue);
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
            p.RunPingers(result);                          /*----->ENABLE THIS<-----*/
            //Add pinger class here
        }
       
    }
}