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
        PC, Laptop, Router, Server, Smartphone, Printer, AccessPoint, Undefined
    }
    public class DeviceDetails
    {
        private string ipAddress;
        private string hostName;
        private deviceType type;
        private string macAddress;
        private long latency;       // Ping lathency in milliseconds
        private string os;
        private string description;
        private bool isSNMPEnabled;
        public DeviceDetails(string ipAddress, string hostName, deviceType type, string macAddress, long lathency, string os, string description, bool isSNMPEnabled)
        {
            IpAddress = ipAddress;
            HostName = hostName;
            Type = type;
            MacAddress = macAddress;
            Latency = latency;
            Os = os;
            Description = description;
            IsSNMPEnabled = isSNMPEnabled;
        }
        public DeviceDetails()
        {
            IpAddress = "192.168.1.1";
            HostName = "Default";
            Type = deviceType.PC;
            MacAddress = "0:0:0:0:0";
            Latency = 999;
            Os = "Unknown";
        }
        public string Os
        {
            get { return os; }
            set { os = value; }
        }
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("IpAddress", IpAddress);
            info.AddValue("Hostname", HostName);
            info.AddValue("Type", Type);
        }

        public DeviceDetails(SerializationInfo info, StreamingContext ctxt)
        {
            IpAddress = (string)info.GetValue("IpAddress", typeof(string));
            HostName = (string)info.GetValue("HostName", typeof(string));
            Type = (deviceType)info.GetValue("Type", typeof(deviceType));
        }
        public bool IsSNMPEnabled
        {
            get { return isSNMPEnabled; }
            set { isSNMPEnabled = value; }
        }
        public string HostName
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
        public string IpAddress
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
        public string MacAddress
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
            public  string Description { get{return description;} set{description=value;}}
    }

    /// <Reference>
    /// Title: Reading And Writing XML Using Serialization - C# Tutorials
    /// Author: Robin19
    /// Website name: Dreamincode.net
    /// Published: 22-September-2010
    /// Access date: 19-January-2015
    /// Available: http://www.dreamincode.net/forums/topic/191471-reading-and-writing-xml-using-serialization/
    ///</Reference>
    
    ///<Note>
    /// Exceptions done by Krystian Horoszkiewicz
    ///</Note>
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
            catch(ArgumentNullException ex)
            {
                Debug.WriteLine("\n-----<ArgumentNullException>-----\nClass: DeviceDetails-MyXML\nSaveObject()\n" + ex.Message + "\n");
                return false;
            }catch(InvalidOperationException ex)
            {
                Debug.WriteLine("\n-----<InvalidOperationException>-----\nClass: DeviceDetails-MyXML\nSaveObject()\n" + ex.Message + "\n");
                return false;
            }
            catch (ArgumentException ex)
            {
                Debug.WriteLine("\n-----<ArgumentException>-----\nClass: DeviceDetails-MyXML\nSaveObject()\n" + ex.Message + "\n");
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
                    stream.Close();
                    return true;
                }
            }
            catch(IOException ex)
            {
                Debug.WriteLine("\n-----<IOException>-----\nClass: DeviceDetails-MyXML\nGetObject()\n" + ex.Message + "\n");
            }
            catch(ArgumentNullException ex)
            {
                Debug.WriteLine("\n-----<ArgumentNullException>-----\nClass: DeviceDetails-MyXML\nGetObject()\n" + ex.Message + "\n");
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine("\n-----<InvalidOperationException>-----\nClass: DeviceDetails-MyXML\nSaveObject()\n" + ex.Message + "\n");
                return false;
            }
            return false;
        }
    }
}