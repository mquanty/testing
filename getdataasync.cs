using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class HttpClientExample
{
    private static readonly HttpClient client = new HttpClient();

    public static async Task<T> GetDataAsync<T>(string apiUrl)
    {
        try
        {
            // Send GET request
            var response = await client.GetAsync(apiUrl);

            // Check if request was successful
            response.EnsureSuccessStatusCode();

            // Read response content
            string responseBody = await response.Content.ReadAsStringAsync();

            // Deserialize response JSON into specified type T
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

        try
        {
            // Retrieve data and deserialize response into specified type
            var data = await GetDataAsync<object>(apiUrl);
            Console.WriteLine($"Data retrieved: {data}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}
