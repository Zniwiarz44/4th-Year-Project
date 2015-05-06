package networkmap.krystian.com.networkmapping;

import android.app.Activity;
import android.content.ActivityNotFoundException;
import android.content.Intent;
import android.os.AsyncTask;
import android.os.Bundle;
import android.util.Log;
import android.widget.Toast;

import com.google.gson.Gson;

import org.apache.http.HttpResponse;
import org.apache.http.client.HttpClient;
import org.apache.http.client.methods.HttpPost;
import org.apache.http.entity.StringEntity;
import org.apache.http.impl.client.DefaultHttpClient;

import java.net.URI;
import java.util.Objects;

/**
 * Created by Krystian on 2015-04-17.
 */

public class ServiceLogin extends AsyncTask {
    private static final String TAG = "Login";
    private boolean logSuccess = false;

    @Override
    protected Objects doInBackground(Object[] params) {
        invokeLogin(params);
        return null;
    }

    public void invokeLogin(Object[] params)
    {
        final Gson gson = new Gson();
        // original object instantiation
       // RegisterBindingModel modelObject = new RegisterBindingModel((String)params[0] ,(String) params[1], (String) params[2]);
        String connection = ("username="+(String)params[0]+"&password="+(String)params[1]+"&grant_type=password");
        // converting an object to json object
      //  String json = gson.toJson(connection);

        HttpClient httpClient = new DefaultHttpClient();
        HttpPost post = new HttpPost();
        try{
            post.setURI(new URI("http://restfulnetwork.azurewebsites.net/Token"));
            post.setHeader("Content-type", "application/json");
            post.setHeader("Content-type", "application/x-www-form-urlencoded");
            StringEntity entity = new StringEntity(connection);
            post.setEntity(entity);

            HttpResponse httpResponse = httpClient.execute(post);

            if (httpResponse.getStatusLine().getStatusCode() == 200) {
                // If successful.
                Log.d(TAG, "Success " + httpResponse.getStatusLine().getStatusCode() + " " + httpResponse.getStatusLine().getReasonPhrase());
                logSuccess = true;
               /* Intent menu= new Intent("networkmap.krystian.com.networkmapping.MENU");
                startActivity(menu);*/
            }
            else
            {
                Log.d(TAG, "Failed " + httpResponse.getStatusLine().getStatusCode() + " " + httpResponse.getStatusLine().getReasonPhrase());
            }
        }catch (Exception ex)
        {
        }
    }
        protected void onPostExecute()
        {

        }
}
