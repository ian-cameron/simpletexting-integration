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
        public static async Task<List<User>> FetchApiUsers(string apiKey)
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
                            // Deserialize into the List<User>
                            var contacts = JsonSerializer.Deserialize<List<User>>(jsonResponse, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (contacts != null && contacts.Count > 0)
                            {
                                apiUsers.AddRange(contacts); // Add all contacts to apiUsers
                                moreResults = contacts.Count == 100; // Check if we received the maximum size
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

        public static async Task UpsertUsers(string apiKey, List<User> users, List<string> lists)
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
                            Email = user.Email,
                            ContactPhone = user.ContactPhone,
                            ListIds = lists
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
