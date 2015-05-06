using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace Project_v1._0._2
{
    class Service
    {
        private string success = null;
        private bool regSuccess = false;
        public string getSuccess()
        {
            return success;
        }
        public bool getRegSuccess()
        {
            return regSuccess;
        }
        public async Task RegisterUser(string email, string password)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("http://restfulnetwork.azurewebsites.net/");                             // base URL for API Controller i.e. RESTFul service
            // add an Accept header for JSON
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));            // or application/xml
            try
            {
                // HTTP POST
                HttpResponseMessage response;
                var gizmo = new RegisterBindingModel() { Email = email, Password = password, ConfirmPassword = password };
                response = await client.PostAsJsonAsync("api/Account/Register", gizmo);
                if (response.IsSuccessStatusCode)
                {
                    // Get the URI of the created resource.
                    Uri gizmoUrl = response.Headers.Location;
                    regSuccess = true;
                    MessageBox.Show("Account created", "Register", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    Console.WriteLine(response.StatusCode + " " + response.ReasonPhrase);
                    MessageBox.Show("Username or password is incorrect", "Register", MessageBoxButton.OK, MessageBoxImage.Error);
                    regSuccess = false;
                }
            }
            catch (Exception ex)
            {
                regSuccess = false;
                Debug.WriteLine("\n-----<Exception>-----\nClass: Service\nRegisterUser()\n" + ex.Message + "\n");
            }

        }
        public async Task LoginUser(string email, string password)
        {
            success = null;
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("http://restfulnetwork.azurewebsites.net/");                                   // base URL for API Controller i.e. RESTFul service
            // add an Accept header for JSON
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));            // or application/xml
            //AddExternalLoginBindingModel tokenResponse;
            try
            {
                ASCIIEncoding encoding = new ASCIIEncoding();
                string postData = "username=" + email + "&password="+password+"&grant_type=password";
                byte[] data = encoding.GetBytes(postData);

                WebRequest request = WebRequest.Create("http://restfulnetwork.azurewebsites.net//Token");
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;

                Stream stream = request.GetRequestStream();
                stream.Write(data, 0, data.Length);
                stream.Close();

                WebResponse response = request.GetResponse();
                stream = response.GetResponseStream();
                HttpWebResponse resp = request.GetResponse() as HttpWebResponse;
                if (resp.IsMutuallyAuthenticated)
                {
                   // Debug.WriteLine("Authenticated");
                }
                else
                {
                    Debug.WriteLine(resp.StatusCode + " ");
                    success = "true";
                    MessageBox.Show("Logged in successfully", "Login", MessageBoxButton.OK);
                }
                stream.Close();
            }
            catch (Exception ex)
            {
                success = "false";
                Debug.WriteLine("\n-----<Exception>-----\nClass: Service\nLoginUser()\n" + ex.Message + "\n");
                MessageBox.Show("Username or password is incorrect", "Login", MessageBoxButton.OK);
            }
        }
    }
}
