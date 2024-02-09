using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

public class HttpClientExample
{
    private static readonly HttpClient client;

    static HttpClientExample()
    {
        // Load your SSL certificate from file or any other source
        X509Certificate2 certificate = new X509Certificate2("path_to_your_certificate.pfx", "certificate_password");

        // Create HttpClientHandler with the SSL certificate
        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(certificate);

        // Set ServerCertificateCustomValidationCallback to accept all certificates
        handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

        // Create HttpClient with the configured handler
        client = new HttpClient(handler);
    }

    public static async Task<string> SendDataWithCertificateAsync(string apiUrl, string jsonData)
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

            return responseBody;
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
            string response = await SendDataWithCertificateAsync(apiUrl, jsonData);
            Console.WriteLine($"Response from server: {response}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}
