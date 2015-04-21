package networkmap.krystian.com.networkmapping;

import android.app.Activity;
import android.content.ActivityNotFoundException;
import android.content.Intent;
import android.os.AsyncTask;
import android.support.v7.app.ActionBarActivity;
import android.os.Bundle;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.widget.EditText;
import android.widget.Toast;


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
            ServiceLogin service = new ServiceLogin();
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
}
