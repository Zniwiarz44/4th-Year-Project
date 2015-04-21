# 4th-Year-Project

This project is based around networking and provides basic discovery capabilities.
To discover the network program performs a ping sweep for a network to which is connected. Than if not unchecked
SNMP scan launches in order to retrive more detail information about each device. To determine devices a fingerprinting
technique is uset which allows to determine the approximate OS running on the device. When the scan is completed
program draws a map of discovered devices and than listens for SSD packets and broadcast on the network. When a new 
is connected and router broadcast forwarding is enabled the program caputes those requests and adds new devices to the 
map. While scanning a graph is being drawn to visualize network performance. Ping and number of devices. User can also
compare 2 network together.
User of the program can Register/Login. This provides him with additional options for saving network maps on the cloud.
Azure SQL is used to store users information
Azure Blob storage is used for storing users files
Web API handles all the request coming from the user of the desktop version of the project and from android session.
In both cases is the API which assigns the Token to successfully recognised user.
