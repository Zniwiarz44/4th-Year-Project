package networkmap.krystian.com.networkmapping;

import android.os.AsyncTask;
import android.util.Log;
import android.widget.Toast;

import com.google.gson.Gson;

import org.apache.http.HttpResponse;
import org.apache.http.client.HttpClient;
import org.apache.http.client.methods.HttpPost;
import org.apache.http.entity.StringEntity;
import org.apache.http.impl.client.DefaultHttpClient;

import java.net.URI;

/**
 * Created by Krystian on 2015-04-17.
 */
public class Service extends AsyncTask {

    private static final String TAG = "Register";
    @Override
    protected Object doInBackground(Object[] params) {
        invokeRegistration(params);
        return null;
    }
    public void invokeRegistration(Object[] params)
    {
        final Gson gson = new Gson();
        // original object instantiation
        RegisterBindingModel modelObject = new RegisterBindingModel((String)params[0] ,(String) params[1], (String) params[2]);
        // converting an object to json object
        String json = gson.toJson(modelObject);

        HttpClient httpClient = new DefaultHttpClient();
        HttpPost post = new HttpPost();
        try{
            post.setURI(new URI("http://restfulnetwork.azurewebsites.net/api/Account/Register"));
            post.setHeader("Content-type", "application/json");
            post.setHeader("Accept", "application/json");
            StringEntity entity = new StringEntity(json);
            post.setEntity(entity);

            HttpResponse httpResponse = httpClient.execute(post);

            if (httpResponse.getStatusLine().getStatusCode() == 200) {
                // If successful.
                 Log.d(TAG, "Success");
            }
            else
            {
                Log.d(TAG, "Failed " + httpResponse.getStatusLine().getStatusCode() + " " + httpResponse.getStatusLine().getReasonPhrase());
            }
        }catch (Exception ex)
        {
            ex.printStackTrace();
        }
    }
}
