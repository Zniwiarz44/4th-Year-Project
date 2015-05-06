package networkmap.krystian.com.networkmapping;

/**
 * Created by Krystian on 2015-04-16.
 */
import java.util.regex.Matcher;
import java.util.regex.Pattern;
/**
 * Class which has Utility methods
 *
 */
public class Utility {
    private static Pattern pattern;
    private static Matcher matcher;
    //Email Pattern
    private static final String EMAIL_PATTERN =
            "^[_A-Za-z0-9-\\+]+(\\.[_A-Za-z0-9-]+)*@"
                    + "[A-Za-z0-9-]+(\\.[A-Za-z0-9]+)*(\\.[A-Za-z]{2,})$";

    /**
     * Validate Email with regular expression
     *
     * @param email
     * @return true for Valid Email and false for Invalid Email
     */
    public static boolean validate(String email) {
        pattern = Pattern.compile(EMAIL_PATTERN);
        matcher = pattern.matcher(email);
        return matcher.matches();

    }
    /**
     * Checks for Null String object
     *
     * @param txt
     * @return true for not null and false for null String object
     */
    public static boolean isNotNull(String txt){
        return txt!=null && txt.trim().length()>0 ? true: false;
    }

    /*
    * Checks if passwords are the same
    * */
    public static boolean validatePassword(String pass, String confPass)
    {
        if(pass.contentEquals(confPass))
        {
            return true;
        }else{return false;}
    }
    /*
        Connection String for Blob storage
     */
    public static final String storageConnectionString =
            "DefaultEndpointsProtocol=https;"
                    + "AccountName=networkmaps;"
                    + "AccountKey=mToGfJAKGBnpXGB/U5bVLB69o8XINjB0WSXJE0pbYcdTdfi/RYkNqWr/iOmQbh8UyvGM6YFYs0NAuI9C5UfDXQ==";

}