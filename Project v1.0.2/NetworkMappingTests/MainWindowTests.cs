using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Project_v1._0._2;
using NUnit.Framework;
using LanDevices;
using System.IO;
using TestStack.White;
using TestStack.White.UIItems.WindowItems;
using TestStack.White.Factory;
using TestStack.White.UIItems;
using TestStack.White.UIItems.WindowStripControls;
using TestStack.White.UIItems.MenuItems;
using TestStack.White.UIItems.Finders;

namespace Project_v1._0._2.Tests
{
    [TestFixture]
    public class MainWindowTests
    {
        List<DeviceDetails> devList = new List<DeviceDetails>();
        string fileNameXML = "UnitTestMap";
        [Test]
        public void Test_DefaultDeviceDetails()                // Make sure that default device can be created
        {
            DeviceDetails dd = new DeviceDetails();
            Assert.AreEqual(dd.MacAddress, "0:0:0:0:0");
            Assert.AreEqual(dd.Latency, 999);
            Assert.AreEqual(dd.Os, "Unknown");
        }
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void Test_MyXMLNull()                            // Test for null exception, makes sure that the file will not be saved with null name
        {
            MyXML.SaveObject(devList, null);
            throw new ArgumentNullException();
        }
        [Test, ExpectedException(typeof(ArgumentException))]
        public void Test_MyXMLEmpty()                           // Checks if .xml file has a name, otherwise throw an exception
        {
            MyXML.SaveObject(devList, " ");
            throw new ArgumentException();
        }
        [Test]
        public void Test_MyXMLSave()                            // This should pass unless there is some change in parameters
        {
            Assert.True(MyXML.SaveObject(devList, fileNameXML));
        }
        [Test]
        public void Test_MyXMLGet()                             // Once file has been created this should always be able to get
        {
            Assert.True(MyXML.GetObject(ref devList, fileNameXML));
        }
        //-----<Intergartion_Test>-----
        [Test]
        public void Test_MainWindowScanButton()                 // Check if program can scan a network, Automated Test
        {
            var applicationPath = Path.Combine("C:\\Users\\X0009_000\\Documents\\Visual Studio 2013\\Projects\\Project v1.0.0\\Project v1.0.0\\bin\\Debug\\", "Project v1.0.0.exe");
            Application application = Application.Launch(applicationPath);
            Window window = application.GetWindow("Network Mapping", InitializeOption.NoCache);

            Button button = window.Get<Button>("ScanButton");
            button.Click();
            Label scan = window.Get<Label>("ScanProgress");
            Label ipAddress = window.Get<Label>("MyIpAddress");
            Assert.AreEqual(scan.Text, "Scan in progress...");
            Assert.AreNotEqual(ipAddress.Text, "0.0.0.0");
        }
    }
}
