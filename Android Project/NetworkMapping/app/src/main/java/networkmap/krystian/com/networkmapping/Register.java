package networkmap.krystian.com.networkmapping;

import java.io.*;
import android.app.Activity;
import android.content.Intent;
import android.nfc.Tag;
import android.os.AsyncTask;
import android.os.Bundle;
import android.util.Log;
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
import com.microsoft.azure.storage.*;
import com.microsoft.azure.storage.blob.*;

/**
 * Created by Krystian on 2015-04-16.
 */
public class Register extends Activity {

    EditText emailET, passwordET, confPassET;
    // Progress Dialog Object
    //ProgressDialog prgDialog;
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.register);
        emailET = (EditText)findViewById(R.id.etEmail);
        passwordET = (EditText)findViewById(R.id.etPassword);
        confPassET = (EditText)findViewById(R.id.etConfPass);
      /*  // Instantiate Progress Dialog object
        prgDialog = new ProgressDialog(this);
        // Set Progress Dialog Text
        prgDialog.setMessage("Please wait...");
        // Set Cancelable as False
        prgDialog.setCancelable(false);*/
    }
    /*
    * Methods gets triggered when Register button is clicked
    *
    * @params view
    * */
    public void registerUser(View v)
    {
        String email = emailET.getText().toString();
        String password = passwordET.getText().toString();
        String confPass = confPassET.getText().toString();

        Object[] params2 = new Object[3];
        if(Utility.isNotNull(email) && Utility.isNotNull(password) && Utility.isNotNull(confPass))
        {
            if(Utility.validate(email) && Utility.validatePassword(password,confPass))
            {
               // RegisterBindingModel modelObject = new RegisterBindingModel(email,password,confPass);
                params2[0]=email;
                params2[1]=password;
                params2[2]=confPass;
                Service2 service = new Service2();
                service.execute(params2);
            }
            else
            {
                message("Passwords are not the same");
            }
        }
        else
        {
            message("Email or passowrd is not valid");
        }
    }
    public void message(String msg)
    {
        Toast.makeText(getApplicationContext(), msg, Toast.LENGTH_SHORT).show();
    }
    private class Service2 extends AsyncTask<Object ,Void, Boolean>
    {
        private String blobUsername = "";
        private static final String TAG = "Register";
        protected Boolean doInBackground(Object[] params) {
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
                    // Log.d(TAG, "Success");

                    //  -----<BLOB_STORAGE>-----
                    // Setup the cloud storage account.
                    CloudStorageAccount account = CloudStorageAccount.parse(Utility.storageConnectionString);
                    // Create a blob service client
                    CloudBlobClient blobClient = account.createCloudBlobClient();
                    try {
                        blobUsername = parseUsername((String)params[0]);
                        Log.i(TAG, " Blob "+ blobUsername );
                        // MessageBox.Show(parseUsername(tbLogin.Text));
                        // Retrieve a reference to a container.
                        CloudBlobContainer container = blobClient.getContainerReference(blobUsername);      // e.g. krystianhoro@gmail.com

                        // Create the container if it does not exist
                        container.createIfNotExists();

                        // Make the container public
                        // Create a permissions object
                        BlobContainerPermissions containerPermissions = new BlobContainerPermissions();

                        // Include public access in the permissions object
                        containerPermissions.setPublicAccess(BlobContainerPublicAccessType.CONTAINER);

                        // Set the permissions on the container
                        container.uploadPermissions(containerPermissions);
                    }catch (Throwable t)
                    {
                        t.printStackTrace();
                    }
                    Intent menu= new Intent("networkmap.krystian.com.networkmapping.MENU");
                    startActivity(menu);
                    return true;
                }
                else
                {
                    //  Log.d(TAG, "Failed " + httpResponse.getStatusLine().getStatusCode() + " " + httpResponse.getStatusLine().getReasonPhrase());
                }
            }catch (Exception ex)
            {
                ex.printStackTrace();
                return false;
            }
            return false;
        }
        public String parseUsername(String username)                  // Trim the username from krystianhoro@gmail.com to just krystianhoro
        {
            int lastIndex =  username.indexOf("@");
            //Start removing from krystianhoro++
            return username.substring(lastIndex);         //End result: krystianhoro
        }
        protected void onPostExecute() {
        }
    }

}
