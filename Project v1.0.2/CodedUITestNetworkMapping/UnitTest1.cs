using System;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;

namespace CodedUITestNetworkMapping
{
  /*  [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
    
        }
    }*/
    [TestFixture]
    public class MainWindow
    {
        [Test]
        public void CanCreateAndShowWpfWindow()
        {
            NetworkMapping runner = new CrossThreadTestRunner();
            runner.RunInSTA(
              delegate
              {
                  Console.WriteLine(Thread.CurrentThread.GetApartmentState());

                  System.Windows.Window window = new System.Windows.Window();
                  window.Show();
              });
        }

        [Test]
        public void PositiveTest()
        {
            int x = 0;
            int y = 0;
            Assert.AreEqual(x, y);
        }
    }
}
