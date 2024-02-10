using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// A static helper class for making HTTP requests.
/// </summary>
public static class HttpClientHelper
{
    private static readonly HttpClient client;
    private static readonly int timeout = 30; // Timeout in seconds
    private static readonly string certPath = "client_certificate.pfx"; // Path to the client certificate
    private static readonly string certPassword = "password"; // Password for the client certificate

    static HttpClientHelper()
    {
        client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(timeout);
    }

    /// <summary>
    /// Sends an HTTP request asynchronously and returns the response body deserialized into the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response body into.</typeparam>
    /// <param name="method">The HTTP method.</param>
    /// <param name="url">The URL to send the request to.</param>
    /// <param name="payload">The payload of the request.</param>
    /// <param name="queryParams">The query parameters to include in the request URL.</param>
    /// <param name="headers">The additional headers to include in the request.</param>
    /// <param name="authenticationType">The type of authentication to use.</param>
    /// <param name="username">The username for HTTP basic authentication.</param>
    /// <param name="password">The password for HTTP basic authentication.</param>
    /// <param name="token">The OAuth2 token for authentication.</param>
    /// <returns>The deserialized response body and cookies received in the response.</returns>
    public static async Task<T> SendHttpRequestAsync<T>(HttpMethod method, string url, string payload = null, Dictionary<string, string> queryParams = null, Dictionary<string, string> headers = null, AuthenticationType authenticationType = AuthenticationType.None, string username = null, string password = null, string token = null)
    {
        try
        {
            var (result, _) = await SendHttpRequestAsyncInternal<T>(method, url, payload, queryParams, headers, authenticationType, username, password, token);
            return result;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"HTTP request failed: {e.Message}");
            throw;
        }
    }
    
     /// <summary>
    /// Sends an HTTP request asynchronously and returns the response body deserialized into the specified type with Cookies
    /// </summary>
    public static async Task<(T, IEnumerable<Cookie>)> SendHttpRequestAsync<T>(HttpMethod method, string url, string payload = null, Dictionary<string, string> queryParams = null, Dictionary<string, string> headers = null, AuthenticationType authenticationType = AuthenticationType.None, string username = null, string password = null, string token = null)
    {
        try
        {
            return await SendHttpRequestAsyncInternal<T>(method, url, payload, queryParams, headers, authenticationType, username, password, token);
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"HTTP request failed: {e.Message}");
            throw;
        }
    }

    private static async Task<(T, IEnumerable<Cookie>)> SendHttpRequestAsyncInternal<T>(HttpMethod method, string url, string payload, Dictionary<string, string> queryParams, Dictionary<string, string> headers, AuthenticationType authenticationType, string username, string password, string token)
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
        if (authenticationType == AuthenticationType.HttpBasic && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }
        else if (authenticationType == AuthenticationType.OAuth2 && !string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // Send HTTP request and get response
        var response = await client.SendAsync(request);

        // Ensure successful response
        response.EnsureSuccessStatusCode();

        // Read response content and deserialize to type T
        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<T>(responseBody);

        // Get cookies from the response
        IEnumerable<Cookie> cookies = GetCookiesFromResponse(response);

        return (result, cookies);
    }

    private static IEnumerable<Cookie> GetCookiesFromResponse(HttpResponseMessage response)
    {
        IEnumerable<string> cookieValues;
        if (response.Headers.TryGetValues("Set-Cookie", out cookieValues))
        {
            foreach (var cookieValue in cookieValues)
            {
                var cookie = SetCookieHeaderValue.Parse(cookieValue);
                yield return new Cookie(cookie.Name.Value, cookie.Value.Value, cookie.Path.Value, cookie.Domain.Value);
            }
        }
    }
}

/// <summary>
/// Enumeration representing different types of authentication.
/// </summary>
public enum AuthenticationType
{
    /// <summary>
    /// No authentication.
    /// </summary>
    None,
    /// <summary>
    /// OAuth2 token-based authentication.
    /// </summary>
    OAuth2,
    /// <summary>
    /// HTTP Basic authentication.
    /// </summary>
    HttpBasic,
    /// <summary>
    /// SSL client certificate authentication.
    /// </summary>
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
            var (response, cookies) = HttpClientHelper.SendHttpRequestAsync<MyResponseClass>(HttpMethod.Get, apiUrl, headers: headers, queryParams: queryParams, authenticationType: AuthenticationType.HttpBasic, username: "username", password: "password").Result;

            if (response != null)
            {
                Console.WriteLine($"Response: {response}");

                // Example usage of cookies
                foreach (var cookie in cookies)
                {
                    Console.WriteLine($"Cookie: {cookie.Name}={cookie.Value}");
                }

                // Check if response indicates success
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    // HTTP 200 OK - Request successful
                    Console.WriteLine($"Request was successful. Response: {response}");
                }
                else
                {
                    // Handle other status codes
                    Console.WriteLine($"Request failed with status code: {response.StatusCode}");
                }
            }
            else
            {
                Console.WriteLine("No response received.");
            }
        }
        catch (HttpRequestException ex)
        {
            // Handle HTTP request exception
            Console.WriteLine($"HTTP request failed: {ex.Message}");
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
