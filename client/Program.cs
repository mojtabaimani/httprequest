using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Configuration;
using Microsoft.Extensions.Configuration;


class Program
{
    static readonly HttpClient httpClient = new HttpClient();
    static readonly ClientWebSocket webSocket = new ClientWebSocket();

    static async Task Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        IConfigurationRoot configuration = builder.Build();

        var token = await GetToken();
        Console.WriteLine($"Token: {token}");

        httpClient.DefaultRequestHeaders.Add("X-Auth-Token", token);


        // Step 1: Initial HTTP GET request to obtain SID and other parameters
        var initialResponse = await httpClient.GetStringAsync("https://api.livetse.ir/socket.io/?EIO=4&transport=polling");
        var sid = ParseSid(initialResponse); // Implement this method to parse SID from the response
        Console.WriteLine($"sid={sid}");

        // Step 2: POST request for authentication with SID and token
        var prefix = "40/ul_iii";
        var str = $"{prefix},{{\"token\":\"{token}\"}}";
        var content = new StringContent("40/ul_iii,{\"token\":\"your_auth_token\"}", Encoding.UTF8, "text/plain");
        var authResponse = await httpClient.PostAsync($"https://api.livetse.ir/socket.io/?EIO=4&transport=polling&sid={sid}", content);
        var authResult = await authResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"Authentication Result: {authResult}");

        // Step 3: Another GET request if necessary (based on your protocol)
        var updatedSidResponse = await httpClient.GetStringAsync($"https://api.livetse.ir/socket.io/?EIO=4&transport=polling&sid={sid}");
        // sid = ParseSid(updatedSidResponse); // Assume sid is updated


        // Step 4: Establish WebSocket connection
        webSocket.Options.SetRequestHeader("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:122.0) Gecko/20100101 Firefox/122.0");
        webSocket.Options.SetRequestHeader("Accept-Encoding", "gzip, deflate, br");
        webSocket.Options.SetRequestHeader("Origin", "https://app.livetse.ir");
        webSocket.Options.SetRequestHeader("Connection", "keep-alive, Upgrade");
        await webSocket.ConnectAsync(new Uri($"wss://api.livetse.ir/socket.io/?EIO=4&transport=websocket&sid={sid}"), CancellationToken.None);

        // Receiving data in a loop
        var receiving = ReceiveWebSocketData(webSocket);
        var sending = SendToWebSocket(webSocket);

        // Step 5:
        var response5 = await httpClient.GetStringAsync($"https://api.livetse.ir/socket.io/?EIO=4&transport=polling&sid={sid}");
        Console.WriteLine($"Step 5: {response5}");

        // Step 6:
        content = new StringContent("42/ul_iii,[\"join:rasad\"]", Encoding.UTF8, "text/plain");
        var response = await httpClient.PostAsync($"https://api.livetse.ir/socket.io/?EIO=4&transport=polling&sid={sid}", content);
        var result = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Step 6 Result: {result}");


        await Task.WhenAll(receiving, sending);
    }

    private static async Task SendToWebSocket(ClientWebSocket webSocket)
    {
        await send(webSocket, "2probe");
        await Task.Delay(1000);
        await send(webSocket, "5");

        while (webSocket.State == WebSocketState.Open)
        {
            try
            {
                await Task.Delay(25000);
                var message = "3";
                await send(webSocket, message);
            }
            catch (WebSocketException ex)
            {
                Console.WriteLine($"WebSocketException occurred: {ex.Message}");
                // await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                break;
            }
        }
    }

    private static async Task send(ClientWebSocket webSocket, string? message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        Console.WriteLine($"Sent: {message}");
    }

    static async Task ReceiveWebSocketData(ClientWebSocket webSocket)
    {
        var buffer = new byte[1024 * 4]; // Adjust buffer size as needed


        while (webSocket.State == WebSocketState.Open)
        {
            using (var memoryStream = new MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    }
                    else
                    {
                        memoryStream.Write(buffer, 0, result.Count);
                    }
                }
                while (!result.EndOfMessage);

                memoryStream.Seek(0, SeekOrigin.Begin);

                // Assuming the data is compressed with GZIP
                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    using (GZipStream decompressionStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                    using (var decompressedStream = new MemoryStream())
                    {
                        decompressionStream.CopyTo(decompressedStream);
                        decompressedStream.Seek(0, SeekOrigin.Begin);
                        var reader = new StreamReader(decompressedStream);
                        string text = reader.ReadToEnd();
                        Console.WriteLine($"Received: {text}");
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    var reader = new StreamReader(memoryStream, Encoding.UTF8);
                    string text = reader.ReadToEnd();
                    Console.WriteLine($"Received: {text}");
                }
            }
        }
    }

    // Implement this method based on how SID is returned in the initial response
    static string ParseSid(string response)
    {
        // Assuming the first character '0' is not part of the JSON and should be removed
        var jsonResponse = response.Substring(1);

        try
        {
            using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
            {
                var root = doc.RootElement;
                var sid = root.GetProperty("sid").GetString();
                return sid ?? string.Empty;
            }
        }
        catch (JsonException)
        {
            // Handle JSON parsing error
            Console.WriteLine("Error parsing JSON response.");
            return string.Empty;
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
            phoneNumber = configuration["PhoneNumber"],
            password = configuration["Password"]
        };
        string jsonCredentials = System.Text.Json.JsonSerializer.Serialize(credentials);

        using (var request = new HttpRequestMessage(HttpMethod.Post, url))
        {
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:122.0) Gecko/20100101 Firefox/122.0");
            request.Headers.Add("Accept", "application/json, text/plain, */*");
            request.Headers.Add("Accept-Language", "en-US,en;q=0.5");
            request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            request.Headers.Add("Referer", "https://app.livetse.ir/");
            request.Headers.Add("Origin", "https://app.livetse.ir");
            request.Headers.Add("DNT", "1");
            request.Headers.ConnectionClose = true; //Equivalent to "Connection: keep-alive" but managed by HttpClient

            var content = new StringContent(jsonCredentials, Encoding.UTF8, "application/json");
            request.Content = content;

            var response = await httpClient.SendAsync(request);


            if (response.Headers.TryGetValues("x-auth-token", out var tokenValues))
            {
                var authToken = tokenValues.FirstOrDefault();
                return authToken;
            }
            else
            {
                Console.WriteLine("x-auth-token header not found.");
                return null;
            }

        }
    }
}
