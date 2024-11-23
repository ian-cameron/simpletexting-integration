using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace SimpletextingAPI
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Load configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var domainController = configuration["DC"];
            var domain = configuration["Domain"];
            var user = configuration["User"];
            var password = configuration["Password"];
            var ou = configuration["OU"] ?? $"OU=Users,DC=${domain},DC=com";
            var apiKey = configuration["ApiKey"];

            // Fetch Active Directory users
            List<User> adUsers = FetchAdUsers(domainController, user, password, ou);
            // Fetch SimpleTexting users
            List<User> apiUsers = await FetchApiUsers(apiKey);

            // Print AD users
            Console.WriteLine("Active Directory Users:");
            foreach (var userObj in adUsers)
            {
                Console.WriteLine($"Name: {userObj.FirstName} {userObj.LastName}, Email: {userObj.Email}, Phone: {userObj.ContactPhone}");
            }

            // Print SimpleTexting users
            Console.WriteLine("\nSimpleTexting Users:");
            foreach (var apiUser in apiUsers)
            {
                Console.WriteLine($"Name: {apiUser.FirstName} {apiUser.LastName}, Email: {apiUser.Email}, Phone: {apiUser.ContactPhone}");
            }
        }

        static List<User> FetchAdUsers(string domainController, string user, string password, string ou)
        {
            List<User> users = new List<User>();

            try
            {
                using (DirectoryEntry entry = new DirectoryEntry($"LDAP://{domainController}/{ou}", user, password))
                {
                    using (DirectorySearcher searcher = new DirectorySearcher(entry))
                    {
                        searcher.Filter = "(objectClass=user)";
                        searcher.PropertiesToLoad.Add("mobile"); // MobilePhone
                        searcher.PropertiesToLoad.Add("givenName"); // FirstName
                        searcher.PropertiesToLoad.Add("sn"); // LastName
                        searcher.PropertiesToLoad.Add("mail"); // Email

                        SearchResultCollection results = searcher.FindAll();

                        foreach (SearchResult result in results)
                        {
                            var contactPhone = result.Properties["mobile"].Count > 0
                                ? StripNonNumeric(result.Properties["mobile"][0].ToString())
                                : null;
                            var firstName = result.Properties["givenName"].Count > 0
                                ? result.Properties["givenName"][0].ToString()
                                : null;
                            var lastName = result.Properties["sn"].Count > 0
                                ? result.Properties["sn"][0].ToString()
                                : null;
                            var email = result.Properties["mail"].Count > 0
                                ? result.Properties["mail"][0].ToString().ToLower()
                                : null;

                            users.Add(new User
                            {
                                ContactPhone = contactPhone,
                                FirstName = firstName,
                                LastName = lastName,
                                Email = email
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return users;
        }
    

        static async Task<List<User>> FetchApiUsers(string apiKey)
        {
            List<User> apiUsers = new List<User>();
            string url = "https://api-app2.simpletexting.com/v2/api/contacts?page=0&size=100&since=1989-12-13";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                int page = 0;
                bool moreResults = true;

                while (moreResults)
                {
                    try
                    {
                        var response = await client.GetAsync(url.Replace("page=0", $"page={page}"));
                        if (response.IsSuccessStatusCode)
                        {
                            var jsonResponse = await response.Content.ReadAsStringAsync();
                            // Deserialize into the ApiResponse class
                            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(jsonResponse, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true // Handle case insensitivity
                            });

                            if (apiResponse != null && apiResponse.Contacts != null)
                            {
                                foreach (var contact in apiResponse.Contacts)
                                {
                                    apiUsers.Add(new User
                                    {
                                        FirstName = contact.FirstName,
                                        LastName = contact.LastName,
                                        Email = contact.Email.ToLower(),
                                        ContactPhone = StripNonNumeric(contact.ContactPhone)
                                    });
                                }

                                moreResults = page < apiResponse.TotalPages - 1; // Check if more pages are available
                            }
                            else
                            {
                                moreResults = false;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"API request failed: {response.ReasonPhrase}");
                            moreResults = false; // Stop fetching if there is a failure
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        Console.WriteLine($"HTTP request error: {httpEx.Message}");
                        moreResults = false; // Stop fetching on HTTP error
                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine($"JSON deserialization error: {jsonEx.Message}");
                        moreResults = false; // Stop fetching on JSON error
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                        moreResults = false; // Stop fetching on unexpected error
                    }

                    page++;
                }
            }

            return apiUsers;
        }

        static string StripNonNumeric(string input)
        {
            return Regex.Replace(input, "[^0-9]", "");
        }
    }



    public class User
    {
       public string ContactPhone { get; set; }
       public string FirstName { get; set; }
       public string LastName { get; set; }
       public string Email { get; set; }
    }

    public class ApiResponse
    {
        public List<User> Contacts { get; set; }
        public int TotalPages { get; set; }
        public long TotalElements { get; set; }
    }
}

