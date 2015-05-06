package networkmap.krystian.com.networkmapping;

import android.app.Activity;
import android.content.ActivityNotFoundException;
import android.content.Intent;
import android.os.AsyncTask;
import android.support.v7.app.ActionBarActivity;
import android.os.Bundle;
import android.util.Log;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.widget.EditText;
import android.widget.Toast;

import com.google.gson.Gson;

import org.apache.http.HttpResponse;
import org.apache.http.client.HttpClient;
import org.apache.http.client.methods.HttpPost;
import org.apache.http.entity.StringEntity;
import org.apache.http.impl.client.DefaultHttpClient;

import java.net.URI;
import java.util.Objects;

public class LoginActivity extends ActionBarActivity {
    EditText emailET;
    EditText passwordET;
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_login);
        emailET = (EditText)findViewById(R.id.etEmail);
        passwordET = (EditText)findViewById(R.id.etPassword);
    }

    public void login(View v)
    {
        String email = emailET.getText().toString();
        String password = passwordET.getText().toString();

        Object[] params = new Object[3];
        if(Utility.isNotNull(email) && Utility.isNotNull(password))
        {
            // RegisterBindingModel modelObject = new RegisterBindingModel(email,password,confPass);
            params[0]=email;
            params[1]=password;
            ServiceLogin2 service = new ServiceLogin2();
            service.execute(params);
        }
        else
        {
            message("Email or passowrd is not valid");
        }
    }
    public void regPage(View v)
    {
        try
        {
            Intent reg= new Intent("networkmap.krystian.com.networkmapping.REGISTER");
            startActivity(reg);

        }catch (ActivityNotFoundException ex)
        {
            ex.printStackTrace();
        }
        catch (NullPointerException ex)
        {
            ex.printStackTrace();
        }
    }
    public void openNewIntent()
    {
        try
        {
            Intent menu= new Intent("networkmap.krystian.com.networkmapping.MENU");
            startActivity(menu);

        }catch (ActivityNotFoundException ex)
        {
            ex.printStackTrace();
        }
        catch (NullPointerException ex)
        {
            ex.printStackTrace();
        }
    }
    public void skip(View v)
    {
        try
        {
            Intent map= new Intent("networkmap.krystian.com.networkmapping.MENU");
            startActivity(map);

        }catch (ActivityNotFoundException ex)
        {
            ex.printStackTrace();
        }catch (NullPointerException ex)
        {
            ex.printStackTrace();
        }
    }
    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        // Handle action bar item clicks here. The action bar will
        // automatically handle clicks on the Home/Up button, so long
        // as you specify a parent activity in AndroidManifest.xml.
        int id = item.getItemId();


        return super.onOptionsItemSelected(item);
    }
    public void message(String msg)
    {
        Toast.makeText(getApplicationContext(), msg, Toast.LENGTH_SHORT).show();
    }
    private class ServiceLogin2 extends AsyncTask
    {
        private static final String TAG = "Login";

        @Override
        protected Boolean doInBackground(Object[] params) {
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
                    Intent menu= new Intent("networkmap.krystian.com.networkmapping.MENU");
                    startActivity(menu);
                }
                else
                {
                    Log.d(TAG, "Failed " + httpResponse.getStatusLine().getStatusCode() + " " + httpResponse.getStatusLine().getReasonPhrase());
                }
            }catch (Exception ex)
            {
            }
            return false;
        }

        protected void onPostExecute() {
        }
    }
}
