using System;
using System.Text;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Text.Json;
using System.Threading.Tasks;
using SimpletextingAPI.Models;

namespace SimpletextingAPI.Services
{
    public static class ApiHelper
    {
        private static async Task<List<TItem>> FetchPaginatedApiData<TResponse, TItem>(string apiKey, string baseUrl, Func<TResponse, List<TItem>> getContent) where TResponse : class
        {
            var results = new List<TItem>();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                int page = 0;
                bool moreResults = true;

                while (moreResults)
                {
                    try
                    {
                        string pageUrl = baseUrl.Replace("page=0", $"page={page}");
                        var response = await client.GetAsync(pageUrl);

                        if (response.IsSuccessStatusCode)
                        {
                            var jsonResponse = await response.Content.ReadAsStringAsync();
                            var apiResponse = JsonSerializer.Deserialize<TResponse>(jsonResponse, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            var items = getContent(apiResponse);
                            if (items != null && items.Count > 0)
                            {
                                results.AddRange(items);
                                moreResults = items.Count == 100; // Assuming 100 is the page size
                            }
                            else
                            {
                                moreResults = false;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"API request failed: {response.ReasonPhrase}");
                            moreResults = false;
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        Console.WriteLine($"HTTP request error: {httpEx.Message}");
                        moreResults = false;
                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine($"JSON deserialization error: {jsonEx.Message}");
                        moreResults = false;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                        moreResults = false;
                    }

                    page++;
                }
            }

            return results;
        }

        public static Task<List<User>> FetchApiUsers(string apiKey)
        {
            string url = "https://api-app2.simpletexting.com/v2/api/contacts?page=0&size=100";
            return FetchPaginatedApiData<UserApiResponse, User>(
                apiKey,
                url,
                response => response?.Content);
        }

        public static Task<List<ContactList>> FetchApiContactLists(string apiKey)
        {
            string url = "https://api-app2.simpletexting.com/v2/api/contact-lists?page=0&size=100";
            return FetchPaginatedApiData<ListApiResponse, ContactList>(
                apiKey,
                url,
                response => response?.Content);
        }
        public static async Task RemoveUsers(string apiKey, List<User> users)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                foreach (var user in users)
                {
                    try
                    {
                        var phoneNumber = user.ContactPhone;
                        var response = await client.DeleteAsync($"https://api-app2.simpletexting.com/v2/api/webhooks/{phoneNumber}");

                        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                        {
                            Console.WriteLine($"Successfully removed user: {user.FirstName} {user.LastName}, Phone: {phoneNumber}");
                        }
                        else
                        {
                            Console.WriteLine($"Failed to remove user: {user.FirstName} {user.LastName}, Phone: {phoneNumber}. Reason: {response.ReasonPhrase}");
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        Console.WriteLine($"HTTP request error while removing user: {user.FirstName} {user.LastName}, Phone: {user.ContactPhone}. Error: {httpEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An unexpected error occurred while removing user: {user.FirstName} {user.LastName}, Phone: {user.ContactPhone}. Error: {ex.Message}");
                    }
                }
            }
        }

        public static async Task CreateLists(string apiKey, List<string> lists)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                foreach (var list in lists)
                {
                    try
                    {
                        var requestBody = new
                        {
                           Name = list
                        };

                        var json = JsonSerializer.Serialize(requestBody);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        // Make the POST request
                        var response = await client.PostAsync("https://api-app2.simpletexting.com/v2/api/contacts?listsReplacement=false", content);

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Successfully added list: {list}");
                        }
                        else
                        {
                            Console.WriteLine($"Failed to add list: {list}. Reason: {response.ReasonPhrase}");
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        Console.WriteLine($"HTTP request error while adding list: {list}. Error: {httpEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An unexpected error occurred while adding list: {list}. Error: {ex.Message}");
                    }
                }
            }
        }

        public static async Task UpsertUsers(string apiKey, List<User> users)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                foreach (var user in users)
                {
                    try
                    {
                        var requestBody = new
                        {
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            ContactPhone = user.ContactPhone,
                            ListIds = user.ListNames
                        };

                        var json = JsonSerializer.Serialize(requestBody);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        // Make the POST request
                        var response = await client.PostAsync("https://api-app2.simpletexting.com/v2/api/contacts?listsReplacement=false", content);

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Successfully upserted user: {user.FirstName} {user.LastName}, Phone: {user.ContactPhone}");
                        }
                        else
                        {
                            Console.WriteLine($"Failed to upsert user: {user.FirstName} {user.LastName}, Phone: {user.ContactPhone}. Reason: {response.ReasonPhrase}");
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        Console.WriteLine($"HTTP request error while upserting user: {user.FirstName} {user.LastName}, Phone: {user.ContactPhone}. Error: {httpEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An unexpected error occurred while upserting user: {user.FirstName} {user.LastName}, Phone: {user.ContactPhone}. Error: {ex.Message}");
                    }
                }
            }
        }
    }
}
