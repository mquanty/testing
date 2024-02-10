using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public static class HttpClientHelper
{
    private static readonly HttpClient client = new HttpClient();
    private static readonly X509Certificate2 clientCertificate;
	private static int timeout = 100;
	// Set the path to the client certificate and its password
	string certPath = "client_certificate.pfx";
	string certPassword = "password";
	
    //static HttpClientHelper() { client = new HttpClient(); }

	/// <summary>
    /// Sends an HTTP request asynchronously and returns the response.
    /// </summary>
    /// <typeparam name="T">The type of the response object.</typeparam>
    /// <param name="method">The HTTP method (GET, POST, etc.).</param>
    /// <param name="url">The URL of the request.</param>
    /// <param name="payload">The request payload (if any).</param>
    /// <param name="queryParams">The query parameters (if any).</param>
    /// <param name="headers">The additional headers (if any).</param>
    /// <param name="authenticationType">The type of authentication to use.</param>
	  /// <param name="username">The username (if authentication type is HTTP Basic authentication).</param>
	  /// <param name="password">The password (if authentication type is HTTP Basic authentication).</param>
    /// <param name="accessToken">The OAuth2 access token (if authentication type is OAuth2).</param>
    /// <returns>The deserialized response object.</returns>
    /// <example>
    /// <code>
    /// // Example usage:
    /// var headers = new Dictionary&lt;string, string&gt;() {{ "Authorization", "Bearer token" }};
    /// var response = await HttpClientHelper.SendHttpRequestAsync&lt;MyResponseClass&gt;(HttpMethod.Get, "https://example.com/api/resource", headers: headers, authenticationType: AuthenticationType.None);
    /// Console.WriteLine($"Response: {response}");
    /// </code>
    /// </example>
    public static async Task<T> SendHttpRequestAsync<T>(HttpMethod method, string url, string payload = null, Dictionary<string, string> queryParams = null, Dictionary<string, string> headers = null, AuthenticationType authenticationType = AuthenticationType.None, string username = null, string password = null, string accessToken = null))
    {
        try
        {
            // Construct URL with query parameters if provided
            if (queryParams != null && queryParams.Count > 0)
            {
                var queryString = new StringBuilder();
                foreach (var param in queryParams)
                {
                    queryString.Append($"{param.Key}={param.Value}&");
                }
                url += "?" + queryString.ToString().TrimEnd('&');
            }

            // Create HTTP request message
            var request = new HttpRequestMessage(method, url);

            // Add payload if provided
            if (!string.IsNullOrEmpty(payload))
            {
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            }

            // Add additional headers if provided
            if (headers != null && headers.Count > 0)
            {
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            // Add authentication if specified
            if (authenticationType == AuthenticationType.OAuth2 && !string.IsNullOrEmpty(accessToken))
            {
                // Add OAuth2 access token to the Authorization header
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
            else if (authenticationType == AuthenticationType.HttpBasic && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }

            // Add SSL/TLS certificate if enabled
            if (authenticationType == AuthenticationType.SslClientCertificate)
            {
				// Load the client certificate
				clientCertificate = new X509Certificate2(certPath, certPassword);
                var handler = new HttpClientHandler();
                handler.ClientCertificates.Add(clientCertificate);
                client = new HttpClient(handler);
            }
			
			// Add TimeOut parameters
			client.Timeout = TimeSpan.FromSeconds(timeout);
			
            // Send HTTP request and get response
            var response = await client.SendAsync(request);

            // Ensure successful response
            response.EnsureSuccessStatusCode();

            // Read response content and deserialize to type T
            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<T>(responseBody);

            return result;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"HTTP request failed: {e.Message}");
            throw;
        }
    }
}

public enum AuthenticationType
{
    None,
    OAuth2,
    HttpBasic,
    SslClientCertificate
}

public class MyClass
{
    public void MyMethod()
    {
        string apiUrl = "https://example.com/api/resource";
        string payload = "{\"key\": \"value\"}";
        Dictionary<string, string> queryParams = new Dictionary<string, string>
        {
            { "param1", "value1" },
            { "param2", "value2" }
        };
        Dictionary<string, string> headers = new Dictionary<string, string>
        {
            { "Authorization", "Bearer token" }
        };

        try
        {
            // Example usage of SendHttpRequestAsync function
            var response = HttpClientHelper.SendHttpRequestAsync<MyResponseClass>(HttpMethod.Get, apiUrl, headers: headers, queryParams: queryParams, authenticationType: AuthenticationType.HttpBasic, username: "username", password: "password").Result;
            Console.WriteLine($"Response: {response}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}

public class MyResponseClass
{
    public string Property1 { get; set; }
    public int Property2 { get; set; }
    // Add more properties as needed
}
