using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace SimpletextingAPI {
    class Program
    {
        static void Main(string[] args)
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
                        List<User> users = new List<User>();

                        foreach (SearchResult result in results)
                        {
                            var contactPhone = result.Properties["mobile"].Count > 0 ? result.Properties["mobile"][0].ToString() : null;
                            var firstName = result.Properties["givenName"].Count > 0 ? result.Properties["givenName"][0].ToString() : null;
                            var lastName = result.Properties["sn"].Count > 0 ? result.Properties["sn"][0].ToString() : null;
                            var email = result.Properties["mail"].Count > 0 ? result.Properties["mail"][0].ToString() : null;

                            users.Add(new User
                            {
                                ContactPhone = contactPhone,
                                FirstName = firstName,
                                LastName = lastName,
                                Email = email
                            });
                        }

                        // Print the list of users
                        foreach (var userObj in users)
                        {
                            Console.WriteLine($"Name: {userObj.FirstName} {userObj.LastName}, Email: {userObj.Email}, Phone: {userObj.ContactPhone}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }

    public class User
    {
        public string ContactPhone { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }
}
