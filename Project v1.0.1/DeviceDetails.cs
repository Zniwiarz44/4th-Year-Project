using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace LanDevices
{
    public enum deviceType
    {
        PC, Laptop, Router, Server, Smartphone, Printer, Undefined
    }
    public class DeviceDetails
    {
        private String ipAddress;
        private String hostName;
        private deviceType type;
        private String macAddress;
        private long latency;    //Ping lathency in milliseconds
        public DeviceDetails(String ipAddress, String hostName, deviceType type, String macAddress, long lathency)
        {
            IpAddress = ipAddress;
            HostName = hostName;
            Type = type;
            MacAddress = macAddress;
            Latency = latency;
        }
        public DeviceDetails()
        {
            IpAddress = "192.168.1.1";
            HostName = "Default";
            Type = deviceType.PC;
            MacAddress = "0:0:0:0:0";
            Latency = 999;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("IpAddress", IpAddress);
            info.AddValue("Hostname", HostName);
            info.AddValue("Type", Type);
        }

        public DeviceDetails(SerializationInfo info, StreamingContext ctxt)
        {
            IpAddress = (String)info.GetValue("IpAddress", typeof(String));
            HostName = (String)info.GetValue("HostName", typeof(String));
            Type = (deviceType)info.GetValue("Type", typeof(deviceType));
        }
        public String HostName
        {
            get
            {
                return hostName;
            }
            set
            {
                hostName = value;
            }
        }
        public String IpAddress
        {
            get
            {
                return ipAddress;
            }
            set
            {
                ipAddress = value;
            }
        }
        public deviceType Type
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
            }
        }
        public String MacAddress
        {
            get
            {
                return macAddress;
            }
            set
            {
                macAddress = value;
            }
        }

        public long Latency
        { 
            get 
            {
                return latency; 
            }
            set 
            { 
                latency = value; 
            } 
        }
    }
    public class MyXML
    {
        public static bool SaveObject<T>(T obj, string FileName)
        {
            try
            {
                var x = new XmlSerializer(obj.GetType());
                using (var Writer = new StreamWriter(FileName, false))
                {
                    x.Serialize(Writer, obj);
                }
                return true;
            }
            catch(IOException ex)
            {
                Debug.WriteLine("\n-----<IOException>-----\nClass: DeviceDetails-MyXML\nSaveObject()\n" + ex.Message + "\n");
                return false;
            }
        }

        public static bool GetObject<T>(ref T obj, string FileName)
        {
            try
            {
                using (FileStream stream = new FileStream(FileName, FileMode.Open))
                {
                    XmlTextReader reader = new XmlTextReader(stream);
                    var x = new XmlSerializer(obj.GetType());
                    obj = (T)x.Deserialize(reader);
                    return true;
                }
            }
            catch(IOException ex)
            {
                Debug.WriteLine("\n-----<IOException>-----\nClass: DeviceDetails-MyXML\nGetObject()\n" + ex.Message + "\n");
            }
            return false;
        }
    }
}