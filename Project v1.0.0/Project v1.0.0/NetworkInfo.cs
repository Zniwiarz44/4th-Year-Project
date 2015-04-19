using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_v1._0._2
{
    class NetworkInfo
    {
        private TimeSpan time;
        private int noOfDevices;
        private int avgPing;

        public NetworkInfo(TimeSpan time, int noOfDevices, int avgPing)
        {
            Time = time;
            NoOfDevices = noOfDevices;
            AvgPing = avgPing;
        }

        public TimeSpan Time { get { return time; } set { time = value; } }

        public int NoOfDevices { get { return noOfDevices; } set { noOfDevices = value; } }

        public int AvgPing { get { return avgPing; } set { avgPing = value; } }
    }
}
