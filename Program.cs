using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using System.Text;

class Program
{
    static async Task Main(string[] args)
    {

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        IConfigurationRoot configuration = builder.Build();
        string apiUrl = configuration["ApiUrl"];
        string authUrl = configuration["AuthUrl"];

        var credentials = new
        {
            Username = configuration["Username"],
            Password = configuration["Password"]
        };
        string jsonCredentials = System.Text.Json.JsonSerializer.Serialize(credentials);

        try
        {
            using (HttpClient client = new HttpClient())
            {

                // Authentication with the API

                var content = new StringContent(jsonCredentials, Encoding.UTF8, "application/json");

                var response1 = client.PostAsync(authUrl, content).Result; // Use .Result for synchronous operation
                string token = string.Empty;
                if (response1.IsSuccessStatusCode)
                {
                    // Extract the token from the response
                    token = response1.Content.ReadAsStringAsync().Result; // Assuming the token is returned as plain text
                    Console.WriteLine($"Token: {token}");
                }
                else
                {
                    Console.WriteLine("Authentication failed.");
                }
                //


                // Add the X-Auth-Token header with your authentication token
                client.DefaultRequestHeaders.Add("X-Auth-Token", token);

                // Loop to run the request 10 times
                for (int i = 0; i < 10; i++)
                {
                    // Send a GET request to the specified Uri
                    HttpResponseMessage response = await client.GetAsync(apiUrl);

                    // Ensure we received a successful response
                    response.EnsureSuccessStatusCode();

                    // Read the response content as a string
                    string responseBody = await response.Content.ReadAsStringAsync();

                    // Print the response body and the iteration to the console
                    Console.WriteLine($"Request #{i + 1} Response: {responseBody}");

                    // Wait for 10 seconds before the next iteration
                    Thread.Sleep(10000); // 10000 milliseconds = 10 seconds
                }
            }
        }
        catch (HttpRequestException e)
        {
            // If something went wrong, print the error message to the console
            Console.WriteLine("\nException Caught!");
            Console.WriteLine("Message :{0} ", e.Message);
        }
    }
}
