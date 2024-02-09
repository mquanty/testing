using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class HttpClientExample
{
    private static readonly HttpClient client = new HttpClient();

    public static async Task<T> GetDataAsync<T>(string apiUrl, IDictionary<string, string> queryParams = null)
    {
        try
        {
            // Build query string with parameters
            string queryString = string.Empty;
            if (queryParams != null)
            {
                List<string> queryParts = new List<string>();
                foreach (var kvp in queryParams)
                {
                    queryParts.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}");
                }
                queryString = string.Join("&", queryParts);
            }

            // Append query string to API URL
            string urlWithQueryParams = apiUrl;
            if (!string.IsNullOrEmpty(queryString))
            {
                urlWithQueryParams += "?" + queryString;
            }

            // Send GET request
            var response = await client.GetAsync(urlWithQueryParams);

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
        
        // Query parameters (optional)
        var queryParams = new Dictionary<string, string>
        {
            { "param1", "value1" },
            { "param2", "value2" }
        };

        try
        {
            // Retrieve data with query parameters and deserialize response into specified type
            var data = await GetDataAsync<object>(apiUrl, queryParams);
            Console.WriteLine($"Data retrieved: {data}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}
