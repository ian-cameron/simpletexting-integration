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
        static int perPage = 500;
        private static async Task<List<TItem>> FetchPaginatedApiData<TItem>(string apiKey, string baseUrl, Func<ApiResponse<TItem>, List<TItem>> getContent)
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
                            var jsonOptions = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            };
                            var jsonResponse = await response.Content.ReadAsStringAsync();
                            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TItem>>(jsonResponse, jsonOptions);

                            var items = getContent(apiResponse ?? new ApiResponse<TItem>());
                            if (items != null && items.Count > 0)
                            {
                                results.AddRange(items);
                                moreResults = items.Count == perPage;
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
            string url = $"https://api-app2.simpletexting.com/v2/api/contacts?page=0&size={perPage}";
            return FetchPaginatedApiData<User>(
                apiKey,
                url,
                response => response.Content ?? []);
        }

        public static Task<List<ContactList>> FetchApiContactLists(string apiKey)
        {
            string url = $"https://api-app2.simpletexting.com/v2/api/contact-lists?page=0&size={perPage}";
            return FetchPaginatedApiData<ContactList>(
                apiKey,
                url,
                response => response.Content ?? []);
        }
        private static async Task<bool> ExecuteHttpRequest(HttpClient client, Func<Task<HttpResponseMessage>> httpCall, string operationDescription)
        {
            try
            {
                var response = await httpCall();
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Success {operationDescription}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Failed {operationDescription}. Status: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP request error while {operationDescription}: {httpEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error while {operationDescription}: {ex.Message}");
                return false;
            }
        }

        public static async Task RemoveUsers(string apiKey, List<User> users)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                foreach (var user in users)
                {
                    var phoneNumber = user.ContactPhone;
                    var operationDescription = $"removing user: {user.FirstName} {user.LastName}, Phone: {phoneNumber}";
                    
                    await ExecuteHttpRequest(client, 
                        () => client.DeleteAsync($"https://api-app2.simpletexting.com/v2/api/contacts/{phoneNumber}"),
                        operationDescription);
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
                    var requestBody = new { name = list };
                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var operationDescription = $"adding list: {list}";
                    
                    await ExecuteHttpRequest(client, 
                        () => client.PostAsync("https://api-app2.simpletexting.com/v2/api/contact-lists", content),
                        operationDescription);
                }
            }
        }

        public static async Task UpdateUsers(string apiKey, List<User> users)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                foreach (var user in users)
                {
                    var requestBody = new
                    {
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        contactPhone = user.ContactPhone,
                        listIds = user.ListNames
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var operationDescription = $"updating user: {user.FirstName} {user.LastName}, Phone: {user.ContactPhone}";
                    
                    await ExecuteHttpRequest(client, 
                        () => client.PutAsync($"https://api-app2.simpletexting.com/v2/api/contacts/{user.ContactPhone}?upsert=true&listsReplacement=true", content),
                        operationDescription);
                }
            }
        }

        public static async Task AddUsers(string apiKey, List<User> users)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                foreach (var user in users)
                {
                    var requestBody = new
                    {
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        contactPhone = user.ContactPhone,
                        listIds = user.ListNames
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var operationDescription = $"adding user: {user.FirstName} {user.LastName}, Phone: {user.ContactPhone}";
                    
                    await ExecuteHttpRequest(client, 
                        () => client.PostAsync("https://api-app2.simpletexting.com/v2/api/contacts", content),
                        operationDescription);
                }
            }
        }
    }
}
