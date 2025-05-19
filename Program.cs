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
var apiKey = configuration["ApiKey"] ?? "";
bool dryRun = Boolean.Parse(configuration["DryRun"] ?? "True");
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

if (dryRun)
{
    Console.WriteLine("Starting a DRY RUN. Nothing will be written to the API.");
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
adUsers = adUsers.Where(u => !Utils.IsBlank(u.ContactPhone)).ToList();
adUsers.ForEach(u => u.ListNames = Utils.IsBlank(u.Office) ? allUserListIds : allUserListIds.Union(new List<string>() { u.Office! }).ToList() );
Console.WriteLine($"Found {adUsers.Count} existing enabled users with mobile numbers in Active Directory.");


// Fetch SimpleTexting users
List<User> apiUsers = await ApiHelper.FetchApiUsers(apiKey);
apiUsers.ForEach(u => u.ListNames = u.Lists.Select(l => l.Name).ToList());
Console.WriteLine($"Found {apiUsers.Count} existing contacts in SimpleTexting.");

// Discover available lists from users
List<ContactList> lists = await ApiHelper.FetchApiContactLists(apiKey);
Console.WriteLine($"Found {lists.Count} existing lists in SimpleTexting.");

// Add any non-existant lists
var newLists = lists.Where(l => !adUsers.Select(u => u.Office).ToList().Distinct().Contains(l.Name)).ToList();
if (newLists.Count > 0)
{
    Console.WriteLine($"Found {newLists.Count} distinct offices from AD Users that do not match up with a list from SimpleTexting to be added:");
    newLists.ForEach(l => Console.WriteLine(l.Name));
}

// New users
var usersToAdd = adUsers.Where(ad => !apiUsers.Any(api => api.ContactPhone == ad.ContactPhone)).ToList();
if (usersToAdd.Count > 0)
{
    Console.WriteLine($"Found {usersToAdd.Count} new users not in SimpleTexting to be added:");
    usersToAdd.OrderBy(u => u.LastName).ToList().ForEach(u => Console.WriteLine($"{u.FirstName} {u.LastName} ({u.Office}): {u.ContactPhone}"));
}

// Removed users
List<User> usersRemoved = apiUsers.Where(api => !adUsers.Any(ad => ad.ContactPhone == api.ContactPhone)).ToList();
if (usersRemoved.Count > 0)
{
    Console.WriteLine($"Found {usersRemoved.Count} users in SimpleTexting no longer active in AD or no longer matching a mobile number to be removed:");
    usersRemoved.OrderBy(u => u.LastName).ToList().ForEach(u => Console.WriteLine($"{u.FirstName} {u.LastName} ({u.ListString}): {u.ContactPhone}"));
}

// Users with updated name or new office
var usersToUpdate = adUsers.Where(ad =>  
    apiUsers.Any(api => api.ContactPhone == ad.ContactPhone &&
    (api.FirstName != ad.FirstName || api.LastName != ad.LastName || !Utils.IsBlank(ad.Office) && !api.ListNames.Contains(ad.Office!)))).ToList();
if (usersToUpdate.Count > 0)
{
    Console.WriteLine($"Found {usersToUpdate.Count} existing users with different Names or Offices in AD than SimpleTexting to be updated:");
    foreach (var uu in usersToUpdate.OrderBy(u => u.LastName))
    {
        var old = apiUsers.Where(u => u.ContactPhone == uu.ContactPhone).First();
        Console.WriteLine($"{old.FirstName} {old.LastName} ({old.ListString}) -> {uu.FirstName} {uu.LastName} ({uu.ListString})");
    }
}

var usersToUpsert = usersToAdd.Union(usersToUpdate).ToList();

// Add users to office
usersToUpsert.ForEach(user => { user.ListNames = allUserListIds.Union(lists.Where(l => l.Name == user.Office).Select(l => l.Name)).ToList(); });



// Print AD users
Console.WriteLine("\nActive Directory Users:");
foreach (var userObj in adUsers.OrderBy(u => u.LastName))
{
    Console.WriteLine($"Name: {userObj.FirstName} {userObj.LastName}, Phone: {userObj.ContactPhone}");
}

// Print SimpleTexting users
Console.WriteLine("\nSimpleTexting Users:");
foreach (var apiUser in apiUsers.OrderBy(u => u.LastName))
{
    Console.WriteLine($"Name: {apiUser.FirstName} {apiUser.LastName}, Phone: {apiUser.ContactPhone}");
}
