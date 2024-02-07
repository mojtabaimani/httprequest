using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Configuration;
using Microsoft.Extensions.Configuration;

class Program
{
    static async Task Main(string[] args)
    {

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        IConfigurationRoot configuration = builder.Build();

        var credentials = new
        {
            Username = configuration["Username"],
            Password = configuration["Password"]
        };
        string authToken = configuration["AuthToken"];
        string apiUrl = configuration["ApiUrl"];

        try
        {
            using (HttpClient client = new HttpClient())
            {
                // Add the X-Auth-Token header with your authentication token
                client.DefaultRequestHeaders.Add("X-Auth-Token", authToken);

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
