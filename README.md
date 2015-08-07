# 4th-Year-Project

This project is based around networking and provides basic discovery capabilities.
To discover a network program performs a ping sweep for a network to which is connected. Than if not unchecked
SNMP scan launches in order to retrive more detail information about each device. To determine devices a fingerprinting
technique is uset which allows to determine the approximate OS running on a device. When the scan is completed
program draws a map of discovered devices and then listens for SSD packets and broadcasts on the network. When a new device is connected and router broadcast forwarding is enabled the program caputes SSD requests and adds new devices to the map.

While scanning a graph is being drawn to visualize the network performance. Ping and number of devices, each is color coded to make the visual more plesent to an eye. User can also compare 2 networks together.
User of the program can Register/Login. This provides him with additional options for saving network maps on the cloud.
Azure SQL is used to store users information
Azure Blob storage is used for storing users files
Web API handles all the requests coming from a user of a desktop version of the project and from android session.
In both cases is the API which assigns the Token to successfully recognised user.

Project was developed in Visual Studio with connection to Microsoft Azure.

Technologies used: C#, WPF Application, Microsoft Azure, ASP.NET, ASP.NET MVC 5, XML, SNMP, SQL Database, BLOB storage and Android.

API's: Pcap.Net, WinPcap.

