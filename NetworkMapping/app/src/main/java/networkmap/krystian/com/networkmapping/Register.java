package networkmap.krystian.com.networkmapping;


import android.app.Activity;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.EditText;
import android.widget.Toast;

import com.google.gson.Gson;
import com.loopj.android.http.RequestParams;

import org.apache.http.HttpResponse;
import org.apache.http.client.ClientProtocolException;
import org.apache.http.client.HttpClient;
import org.apache.http.client.methods.HttpGet;
import org.apache.http.client.methods.HttpPost;
import org.apache.http.entity.StringEntity;
import org.apache.http.impl.client.DefaultHttpClient;
import org.apache.http.message.BasicHeader;
import org.apache.http.protocol.HTTP;

import java.io.IOException;
import java.net.URI;

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
                Service service = new Service();
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
}
