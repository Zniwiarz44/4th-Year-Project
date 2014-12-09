using System;
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
        public DeviceDetails(String ipAddress, String hostName, deviceType type)
        {
            IpAddress = ipAddress;
            HostName = hostName;
            Type = type;

        }
        public DeviceDetails()
        {
            IpAddress = "192.168.1.1";
            HostName = "Default";
            Type = deviceType.PC;
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
            catch
            {
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
            catch
            {
            }
            return false;
        }
    }
}