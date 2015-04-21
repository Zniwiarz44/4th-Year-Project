package networkmap.krystian.com.networkmapping;

import android.app.Activity;
import android.app.Fragment;
import android.app.FragmentManager;
import android.app.SearchManager;
import android.content.Context;
import android.content.Intent;
import android.content.res.Configuration;
import android.net.DhcpInfo;
import android.net.wifi.WifiManager;
import android.os.Bundle;
import android.support.v4.view.GravityCompat;
import android.support.v7.widget.Toolbar;
import android.support.v4.widget.DrawerLayout;
import android.support.v7.app.ActionBarActivity;
import android.support.v7.app.ActionBarDrawerToggle;
import android.text.InputType;
import android.view.LayoutInflater;
import android.view.Menu;
import android.text.format.Formatter;
import android.util.Log;
import android.view.MenuInflater;
import android.view.MenuItem;
import android.view.View;
import android.view.ViewGroup;
import android.widget.AdapterView;
import android.widget.ArrayAdapter;
import android.widget.ImageView;
import android.widget.ListView;
import android.widget.TextView;
import android.widget.Toast;

import java.io.BufferedReader;
import java.io.DataOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.util.Locale;

/**
 * Created by Krystian on 2015-04-16.
 */
public class NetMap extends ActionBarActivity {

    private static final String TAG = "Network.java";
    public static String pingError = null;
    TextView outputTV, wifiInfoTV;
    TextView info;
    DhcpInfo d;
    WifiManager wifii;

    public void pingSweep()
    {
        //for i in {1..254}; do ping -c 1 -W 1 10.1.1.$i | grep 'from' | cut -d' ' -f 4 | tr -d ':'; done
    }
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.netmap);
        outputTV = (TextView)findViewById(R.id.tvOutput);
        wifiInfoTV = (TextView)findViewById(R.id.tvWifiInfo);
        wifii= (WifiManager) getSystemService(Context.WIFI_SERVICE);
        d=wifii.getDhcpInfo();

        wifiInfoTV.setText("Default Gateway: "+FormatIP(d.gateway)+"\n"+"IP Address: "+FormatIP(d.ipAddress)+"\nSubnet Mask: "+FormatIP(d.netmask));

    }
    /* private class DrawerItemClickListener implements ListView.OnItemClickListener {
        @Override
        public void onItemClick(AdapterView parent, View view, int position, long id) {
            //Toast.makeText(NetMap.this, ((TextView)view).getText(), Toast.LENGTH_LONG).show();
            message("TEST");
            drawerLayout.closeDrawer(drawerListView);
        }
    }*/
    public String FormatIP(int IpAddress)
    {
        return Formatter.formatIpAddress(IpAddress);
    }
    public void message(String msg)
    {
        Toast.makeText(getApplicationContext(), msg, Toast.LENGTH_SHORT).show();
    }
    /**
     * Ping a host and return an int value of 0 or 1 or 2 0=success, 1=fail, 2=error
     *
     * Does not work in Android emulator and also delay by '1' second if host not pingable
     * In the Android emulator only ping to 127.0.0.1 works
     *
     */
    public int pingHost(View v) throws IOException, InterruptedException {
        outputTV.setText("");
        String host= FormatIP(d.gateway);
        Runtime runtime = Runtime.getRuntime();

        Process proc = runtime.exec("for i in 'seq 1 254'; do ping -c 1 192.168.1.$i; done");//"for i in {1..254}; do ping -c 1 192.168.1.$i"

        StringBuffer echo = new StringBuffer();
        InputStreamReader reader = new InputStreamReader(proc.getInputStream());
        BufferedReader buffer = new BufferedReader(reader);
        String line = "";
        while ((line = buffer.readLine()) != null) {
            echo.append(line + "\n");
        }
        outputTV.setText(echo.toString());
        proc.waitFor();
        int exit = proc.exitValue();
        return exit;
    }

    public String ping(View v) throws IOException, InterruptedException {
        String host= "192.168.1.254";
        StringBuffer echo = new StringBuffer();
        Runtime runtime = Runtime.getRuntime();
        Log.v(TAG, "About to ping using runtime.exec");
        Process proc = runtime.exec("ping -c 1 " + host);
        proc.waitFor();
        int exit = proc.exitValue();
        if (exit == 0) {
            InputStreamReader reader = new InputStreamReader(proc.getInputStream());
            BufferedReader buffer = new BufferedReader(reader);
            String line = "";
            while ((line = buffer.readLine()) != null) {
                echo.append(line + "\n");
            }
            message(getPingStats(echo.toString()));
            return getPingStats(echo.toString());
        } else if (exit == 1) {
            pingError = "failed, exit = 1";
            return null;
        } else {
            pingError = "error, exit = 2";
            return null;
        }
    }

    /**
     * getPingStats interprets the text result of a Linux ping command
     *
     * Set pingError on error and return null
     *
     * http://en.wikipedia.org/wiki/Ping
     *
     * PING 127.0.0.1 (127.0.0.1) 56(84) bytes of data.
     * 64 bytes from 127.0.0.1: icmp_seq=1 ttl=64 time=0.251 ms
     * 64 bytes from 127.0.0.1: icmp_seq=2 ttl=64 time=0.294 ms
     * 64 bytes from 127.0.0.1: icmp_seq=3 ttl=64 time=0.295 ms
     * 64 bytes from 127.0.0.1: icmp_seq=4 ttl=64 time=0.300 ms
     *
     * --- 127.0.0.1 ping statistics ---
     * 4 packets transmitted, 4 received, 0% packet loss, time 0ms
     * rtt min/avg/max/mdev = 0.251/0.285/0.300/0.019 ms
     *
     * PING 192.168.0.2 (192.168.0.2) 56(84) bytes of data.
     *
     * --- 192.168.0.2 ping statistics ---
     * 1 packets transmitted, 0 received, 100% packet loss, time 0ms
     *
     * # ping 321321.
     * ping: unknown host 321321.
     *
     * 1. Check if output contains 0% packet loss : Branch to success -> Get stats
     * 2. Check if output contains 100% packet loss : Branch to fail -> No stats
     * 3. Check if output contains 25% packet loss : Branch to partial success -> Get stats
     * 4. Check if output contains "unknown host"
     *
     * @param s
     */
    public String getPingStats(String s) {
        if (s.contains("0% packet loss")) {
            int start = s.indexOf("/mdev = ");
            int end = s.indexOf(" ms\n", start);
            s = s.substring(start + 8, end);
            String stats[] = s.split("/");
            return stats[2];
        } else if (s.contains("100% packet loss")) {
            pingError = "100% packet loss";
            return null;
        } else if (s.contains("% packet loss")) {
            pingError = "partial packet loss";
            return null;
        } else if (s.contains("unknown host")) {
            pingError = "unknown host";
            return null;
        } else {
            pingError = "unknown error in getPingStats";
            return null;
        }
    }

}
