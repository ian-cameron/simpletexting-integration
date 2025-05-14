using Microsoft.Extensions.Configuration;
using SimpletextingAPI.Services;
using SimpletextingAPI.Models;
using System.Linq;
using SimpletextingAPI;
// Load configuration
var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

var domainController = configuration["DC"] ?? "";
var domain = configuration["Domain"];
var username = configuration["Username"];
var password = configuration["Password"];
var ou = configuration["OU"] ?? $"OU=Users,DC=${domain},DC=com";
var apiKey = configuration["ApiKey"];
var allUserListIds = configuration.GetSection("ListIds").Get<List<string>>();

if (Utils.IsBlank(apiKey))
{
    Console.WriteLine("Must supply apiKey");
    return;
}

if (allUserListIds == null || !allUserListIds.Any())
{
    Console.WriteLine("Must specify at least one list Id");
    return;
}

if (Utils.IsBlank(domain))
{
    Console.WriteLine("Must specify and AD domain");
    return;
}

// If specifiying a username you must supply password.  If supplying a password you must specify a username.
if (Utils.IsBlank(username) && !Utils.IsBlank(password) || Utils.IsBlank(password) && !Utils.IsBlank(username))
{
    Console.WriteLine($"Must supply a {(Utils.IsBlank(password) ? "password" : "username")}.");
}

// Fetch Active Directory users
List<User> adUsers = [];
if (Utils.IsBlank(domainController))
{
    if (Utils.IsBlank(username) || Utils.IsBlank(password))
        adUsers = AdHelper.FetchAdUsers(ou);
    else
        adUsers = AdHelper.FetchAdUsers(username!, password!, ou);
}
else if (Utils.IsBlank(username) || Utils.IsBlank(password))
{
    adUsers = AdHelper.FetchAdUsers(domainController, ou);
}
else
{
    adUsers = AdHelper.FetchAdUsers(domainController, username!, password!, ou);
}


// Fetch SimpleTexting users
List<User> apiUsers = await ApiHelper.FetchApiUsers(apiKey!);

// Discover available lists from users
List<ContactList> lists = await ApiHelper.FetchApiContactLists(apiKey!);

// Add any non-existant lists
var newLists = lists.Where(l => !adUsers.Select(u => u.Office).ToList().Distinct().Contains(l.Name));

// New users, or users with changed names,  emails or office names
List<User> usersToUpsert = adUsers
    .Where(ad => !apiUsers.Any(api => api.ContactPhone == ad.ContactPhone) ||
              apiUsers.Any(api => api.ContactPhone == ad.ContactPhone &&
             (api.FirstName != ad.FirstName || api.LastName != ad.LastName || api.Email != ad.Email || !api.Lists.Select(l=> l.Name).Contains(ad.Office)))
          ).ToList();

// Add users to office
usersToUpsert.ForEach(user => { user.ListIds = allUserListIds.Union(lists.Where(l => l.Name == user.Office).Select(l => l.ListId)).ToList(); });

// API Users with phone numbers not found in AD
List<User> usersRemoved = apiUsers.Where(api => !adUsers.Any(ad => ad.ContactPhone == api.ContactPhone)).ToList();

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

