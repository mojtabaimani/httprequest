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

        try
        {
            using (HttpClient client = new HttpClient())
            {

                var token = await GetToken();
                Console.WriteLine(token);

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
    public static async Task<string> GetToken()
    {

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        IConfigurationRoot configuration = builder.Build();
        string url = configuration["AuthUrl"];

        var credentials = new
        {
            phoneNumber = configuration["phoneNumber"],
            password = configuration["password"]
        };
        string jsonCredentials = System.Text.Json.JsonSerializer.Serialize(credentials);

        using (var client = new HttpClient())
        using (var request = new HttpRequestMessage(HttpMethod.Post, url))
        {
            // Set request headers
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:122.0) Gecko/20100101 Firefox/122.0");
            request.Headers.Add("Accept", "application/json, text/plain, */*");
            request.Headers.Add("Accept-Language", "en-US,en;q=0.5");
            request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            request.Headers.Add("Referer", "https://app.livetse.ir/");
            request.Headers.Add("Origin", "https://app.livetse.ir");
            request.Headers.Add("DNT", "1");
            request.Headers.ConnectionClose = true; // Equivalent to "Connection: keep-alive" but managed by HttpClient
            // Sec-Fetch-* headers are typically controlled by the browser and not set programmatically

            // Set Content-Type and Content-Length via HttpContent
            var content = new StringContent(jsonCredentials, Encoding.UTF8, "application/json");
            request.Content = content;

            // Send the request
            var response = await client.SendAsync(request);

            // Read the response content
            // var responseContent = await response.Content.ReadAsStringAsync();

            // Check if the x-auth-token header is present
            if (response.Headers.TryGetValues("x-auth-token", out var tokenValues))
            {
                // Assuming there's only one value for this header
                var authToken = tokenValues.FirstOrDefault();
                return authToken; // Return the token
            }
            else
            {
                Console.WriteLine("x-auth-token header not found.");
                return null; // or appropriate handling
            }

        }
    }
}
