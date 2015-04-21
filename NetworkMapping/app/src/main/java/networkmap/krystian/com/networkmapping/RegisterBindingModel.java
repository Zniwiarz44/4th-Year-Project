package networkmap.krystian.com.networkmapping;

/**
 * Created by Krystian on 2015-04-16.
 */
public class RegisterBindingModel {

    public String email;
    public String password;
    public String confirmPassword;

    public RegisterBindingModel(String email, String password, String confirmPassword)
    {
        this.email = email;
        this.password = password;
        this.confirmPassword = confirmPassword;
    }

    public String getEmail() {
        return email;
    }

    public void setEmail(String email) {
        this.email = email;
    }

    public String getPassword() {
        return password;
    }

    public void setPassword(String password) {
        this.password = password;
    }

    public String getConfirmPassword() {
        return confirmPassword;
    }

    public void setConfirmPassword(String confirmPassword) {
        this.confirmPassword = confirmPassword;
    }
    @Override
    public String toString() {
        return "ModelObject [email=" + email + ",password=" + password + ", confirmPassword=" + confirmPassword+"]";
    }
}
