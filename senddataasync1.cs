using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class HttpClientExample
{
    private static readonly HttpClient client = new HttpClient();

    public static async Task<T> SendDataAsync<T>(string apiUrl, string jsonData)
    {
        try
        {
            // Create HTTP request content
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            // Send POST request
            var response = await client.PostAsync(apiUrl, content);

            // Check if request was successful
            response.EnsureSuccessStatusCode();

            // Read response content
            string responseBody = await response.Content.ReadAsStringAsync();

            // Deserialize response JSON into anonymous type T
            T result = JsonSerializer.Deserialize<T>(responseBody);

            return result;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"HTTP request failed: {e.Message}");
            throw;
        }
    }

    public static async Task Main(string[] args)
    {
        string apiUrl = "https://example.com/api/data";
        string jsonData = "{\"key\": \"value\"}";

        try
        {
            // Deserialize response into anonymous type
            var response = await SendDataAsync<object>(apiUrl, jsonData);
            Console.WriteLine($"Response from server: {response}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}
