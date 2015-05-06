package networkmap.krystian.com.networkmapping;

/**
 * Created by Krystian on 2015-04-17.
 */
public class LogMeIn
{
    public String getUsername() {
        return username;
    }

    public void setUsername(String username) {
        this.username = username;
    }

    public String getPassword() {
        return password;
    }

    public void setPassword(String password) {
        this.password = password;
    }

    private String username;
    private String password;
    LogMeIn(String username, String password)
    {
        this.username = username;
        this.password=password;
    }

}