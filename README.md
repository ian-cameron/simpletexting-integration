# SimpleTexting Integration with Active Directory
[SimpleTexting](simpletexting.com) is an affordable SMS gateway.  They have an API for managing users.  Many companies already have a Windows Active Directory of all users.  This utility is made to automatically synchronize contacts from AD to SimpleTexting using their [v2 REST API](https://simpletexting.com/api/docs/v2/). When configured as a recurring task, it can keep the systems in sync.

## Requirements
* SimpleTexting API Key
* Active Directory / LDAP User objects are assumed to have the following standard properties
    * `mobile` (Mobile Phone)
    * `givenName` (First Name)
    * `sn` (Surname / Last Name)
    * `physicalDeliveryOfficeName` (Office)

## Functionality
 * Only *enabled* User objects with a non-empty `mobile` will be synchronized- it is the primary identifier used to match users between the systems.
 * If a name or office change is detected between the systems, we will update SimpleTexting with the values from AD.
 * We assume there is a SimpleTexting group name that matches each distinct Office name.
   * Users will be added to the contact list that matches their Office.
   * Users will be added to all lists specifed in the configuration file value for `listIds`
   * Users will be removed from any other contact lists
   * Unmatched contacts (by `mobile`) from SimpleTexting who are not found in Active Directory will be removed.

## Configuration
[appsettings.json](https://github.com/ian-cameron/simpletexting-integration/blob/2d53ccd1e215877e52f94a74417580cfb4e5513c/appsettings.json) specifies the following key-value pairs:

| Property Name | Example  | Description | Required? |
| ------------- |--------| ------------- | --------- |
| Domain  | `example.com `|FQDN - Active Directory Domain | :heavy_check_mark:          |
| DC  | `dc001.example.internal` | Hostname of preferred Domain Controller, you'll need this if running from a machine thats not a member of the domain  | :x:  |
| Username  |`ad_reader`| An account name with permissions on the AD, otherwise if will run under the context of your logged in user which is usually enough  |:x:  |
| Password  |`@p@5s\/\/0rD`| Only applicable when a Username is specified  | :x: |
| OU  | `OU=Example Users,DC=example,DC=com`|Organizational Unit specified in a X.500/LDAP DIT format of where user objects are to be read from.  It will default to "OU=Users,DC={Domain.Replace(".",",DC=")}" if omitted. | :x: |
| ApiKey  |`507f191e810c19729de860ea`| SimpleTexting API Token. Your API token can be found under [settings](https://app2.simpletexting.com/integrations/webhooks), used as the token in the request header Authorization: Bearer <token>  | :heavy_check_mark:  |
| DryRun  |`true`| Do not write to API, just log actions to console. Default is true, so to run you have to specify 'false'  | :x:  |
| listIds  |`["All Employees", "AD Users"]`| Contact Lists (both Name or listId props are supported), as a JSON Array which to add all users to  | :heavy_check_mark: |

## Usage
This tool is a console app meant to be run on a CLI or in a script.  There are no CLI arguments.  It requires the JSON config file.  Console output can be redirected to create a logfile.  For example `task.ps1` could be scheduled as a task to run nightly to keep systems synced:

```
./simpletextingAPI.exe | ForEach-Object { "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') $_" } | Tee-Object -FilePath SimpleTextingSync.log
```

This appends output to a log file `SimpleTextingSync.log`.  

By default the tool runs with under the user's domain context.  As such, the configuration values for domain, username, and password are optional and can be provided by the environment.  The Windows Scheduled Task Manager can store "Run As" user credentials as well.
