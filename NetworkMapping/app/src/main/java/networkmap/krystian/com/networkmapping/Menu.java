package networkmap.krystian.com.networkmapping;

import android.app.Activity;
import android.content.ActivityNotFoundException;
import android.content.Context;
import android.content.Intent;
import android.net.DhcpInfo;
import android.net.wifi.WifiManager;
import android.os.Bundle;
import android.text.format.Formatter;
import android.view.View;
import android.widget.AdapterView;
import android.widget.ArrayAdapter;
import android.widget.ListView;
import android.widget.TextView;
import android.widget.Toast;

import java.util.List;

/**
 * Created by Krystian on 2015-04-21.
 */
public class Menu extends Activity {
    TextView dns1Tv, dns2Tv, defGTv, ipAddTv, leaseTv, subnetTv, serverTv;
    ListView listView;
    DhcpInfo d;
    WifiManager wifii;
    String classes[] = {"PING", "NETMAP", "NetworkDetails", "NetworkMap"};
    String names[] = {"Ping", "Scan network", "Network details", "Network map"};
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.menu);
        listView = (ListView)findViewById(R.id.lvMenu);
        wifii= (WifiManager) getSystemService(Context.WIFI_SERVICE);
        dns1Tv = (TextView)findViewById(R.id.tvDNS1);
        dns2Tv =(TextView)findViewById(R.id.tvDNS2);
        defGTv =(TextView)findViewById(R.id.tvDefault);
        ipAddTv =(TextView)findViewById(R.id.tvIpAddress);
        leaseTv =(TextView)findViewById(R.id.tvLease);
        subnetTv =(TextView)findViewById(R.id.tvSubnet);
        serverTv =(TextView)findViewById(R.id.tvServer);

        d=wifii.getDhcpInfo();

        dns1Tv.append(" "+FormatIP(d.dns1));
        dns2Tv.append(" "+FormatIP(d.dns2));
        defGTv.append(" "+FormatIP(d.gateway));
        ipAddTv.append(" "+FormatIP(d.ipAddress));
        leaseTv.append(" "+FormatIP(d.leaseDuration));
        subnetTv.append(" "+FormatIP(d.netmask));
        serverTv.append(" "+FormatIP(d.serverAddress));

               ArrayAdapter<String> arrayAdapter = new ArrayAdapter<String>(this, android.R.layout.select_dialog_item, names);
        listView.setAdapter(arrayAdapter);
        listView.setOnItemClickListener(new AdapterView.OnItemClickListener() {
            @Override
            public void onItemClick(AdapterView<?> parent, View view, int position, long id) {
                //  Toast.makeText(getApplicationContext(), "Hello "+menuList[position],Toast.LENGTH_SHORT).show();
                try{
                    Intent newClass= new Intent("networkmap.krystian.com.networkmapping."+classes[position]);
                  //  rateMovie.putExtra("movieTitle",menuList[position]);
                    startActivity(newClass);
                }catch (ActivityNotFoundException e)
                {
                    message("Not available");
                    e.printStackTrace();
                }
                catch (NullPointerException ex)
                {
                    message("Not available");
                    ex.printStackTrace();
                }
            }
        });
    }
    public String FormatIP(int IpAddress)
    {
        return Formatter.formatIpAddress(IpAddress);
    }
    public void message(String msg)
    {
        Toast.makeText(getApplicationContext(), msg, Toast.LENGTH_SHORT).show();
    }
}
