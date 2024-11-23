using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Text.RegularExpressions;
using SimpletextingAPI.Models;

namespace SimpletextingAPI.Services
{
    public static class AdHelper
    {
        // Fetching from specifc DC with specifc user
        public static List<User> FetchAdUsers(string domainController, string user, string password, string ou)
        {
            List<User> users = new List<User>();
            try
            {
                using (DirectoryEntry entry = new DirectoryEntry($"LDAP://{domainController}/{ou}", user, password))
                {
                    users = SearchDirectory(entry);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return users;
        }

        // Overloaded method for fetching AD users with username and password from any DC
        public static List<User> FetchAdUsers(string user, string password, string ou)
        {
            List<User> users = new List<User>();

            try
            {
                using (DirectoryEntry entry = new DirectoryEntry($"LDAP://{ou}", user, password))
                {
                    users = SearchDirectory(entry);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return users;
        }

        // Overloaded method for fetching AD users from specific DC with integrated security
        public static List<User> FetchAdUsers(string domainController, string ou)
        {
            List<User> users = new List<User>();

            try
            {
                using (DirectoryEntry entry = new DirectoryEntry($"LDAP://{domainController}/{ou}"))
                {
                    users = SearchDirectory(entry);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return users;
        }

        // Overloaded method for fetching AD users with integrated security
        public static List<User> FetchAdUsers(string ou)
        {
            List<User> users = new List<User>();

            try
            {
                using (DirectoryEntry entry = new DirectoryEntry($"LDAP://{ou}"))
                {
                    users = SearchDirectory(entry);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return users;
        }

        private static List<User> SearchDirectory(DirectoryEntry entry) {

            List<User> users = new List<User>();
            using (DirectorySearcher searcher = new DirectorySearcher(entry))
            {
                searcher.Filter = "(objectClass=user)";
                searcher.PropertiesToLoad.Add("mobile"); // MobilePhone
                searcher.PropertiesToLoad.Add("givenName"); // FirstName
                searcher.PropertiesToLoad.Add("sn"); // LastName
                searcher.PropertiesToLoad.Add("mail"); // Emai
                SearchResultCollection results = searcher.FindAll();
                foreach (SearchResult result in results)
                {
                    var contactPhone = result.Properties["mobile"].Count > 0 
                        ? Utils.StripNonNumeric(result.Properties["mobile"][0].ToString()) 
                        : null;
                    var firstName = result.Properties["givenName"].Count > 0 
                        ? result.Properties["givenName"][0].ToString() 
                        : null;
                    var lastName = result.Properties["sn"].Count > 0 
                        ? result.Properties["sn"][0].ToString() 
                        : null;
                    var email = result.Properties["mail"].Count > 0 
                        ? result.Properties["mail"][0].ToString()?.ToLower() 
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
            return users;
        }
    }
}
