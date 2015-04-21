package networkmap.krystian.com.networkmapping;

import android.app.Activity;
import android.content.Context;
import android.media.ExifInterface;
import android.net.DhcpInfo;
import android.net.wifi.WifiManager;
import android.os.Bundle;
import android.text.format.Formatter;
import android.util.Log;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.TextView;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;

/**
 * Created by Krystian on 2015-04-21.
 */
public class Ping extends Activity{
    EditText eTargetIp;
    Button bPing, bDefault;
    TextView tvOutput;
    private static final String TAG = "Ping.java";
    public static String pingError = null;
    DhcpInfo d;
    WifiManager wifii;
    public String defaultGateway = "";
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.ping);
        wifii= (WifiManager) getSystemService(Context.WIFI_SERVICE);
        d=wifii.getDhcpInfo();
        tvOutput = (TextView)findViewById(R.id.tvOutput);
        bPing = (Button)findViewById(R.id.bPing);
        bDefault = (Button)findViewById(R.id.bDefault);
        defaultGateway = FormatIP(d.gateway);
    }
    public String FormatIP(int IpAddress) throws NullPointerException
    {
        return Formatter.formatIpAddress(IpAddress);
    }
    public void pingDefault(View v)throws IOException, InterruptedException
    {
        StringBuffer echo = new StringBuffer();
        Runtime runtime = Runtime.getRuntime();
        Log.v(TAG, "About to ping using runtime.exec");
        Process proc = runtime.exec("ping -c 2 " + defaultGateway);
        proc.waitFor();
        int exit = proc.exitValue();
        if (exit == 0) {
            InputStreamReader reader = new InputStreamReader(proc.getInputStream());
            BufferedReader buffer = new BufferedReader(reader);
            String line = "";
            while ((line = buffer.readLine()) != null) {
                echo.append(line + "\n");
            }
            tvOutput.setText("Output:\n" + getPingStats(echo.toString()));
        } else if (exit == 1) {
            pingError = "failed, exit = 1";
        } else {
            pingError = "error, exit = 2";
        }
    }
    public void pingTarget(View v)throws IOException, InterruptedException
    {
        eTargetIp = (EditText)findViewById(R.id.eTarget);
        String host= eTargetIp.getText().toString();

        StringBuffer echo = new StringBuffer();
        Runtime runtime = Runtime.getRuntime();
        Log.v(TAG, "About to ping using runtime.exec");
        Process proc = runtime.exec("ping -c 2 " + host);
        proc.waitFor();
        int exit = proc.exitValue();
        if (exit == 0) {
            InputStreamReader reader = new InputStreamReader(proc.getInputStream());
            BufferedReader buffer = new BufferedReader(reader);
            String line = "";
            while ((line = buffer.readLine()) != null) {
                echo.append(line + "\n");
            }
            tvOutput.setText("Output:\n" + getPingStats(echo.toString()));
        } else if (exit == 1) {
            pingError = "failed, exit = 1";
        } else {
            pingError = "error, exit = 2";
        }
    }
    public String getPingStats(String s) {
        if (s.contains("0% packet loss")) {
            return s;
        } else if (s.contains("100% packet loss")) {
            pingError = "100% packet loss";
            return pingError;
        } else if (s.contains("% packet loss")) {
            pingError = "partial packet loss";
            return pingError;
        } else if (s.contains("unknown host")) {
            pingError = "unknown host";
            return pingError;
        } else {
            pingError = "unknown error in getPingStats";
            return pingError;
        }
    }
}
