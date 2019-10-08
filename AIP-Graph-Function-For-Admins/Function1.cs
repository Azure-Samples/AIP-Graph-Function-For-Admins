using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.WebPages.Html;
using System.Net;
using System.Text;
using Microsoft.Identity.Client;
using Microsoft.Graph;
using System.Security.Claims;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.Azure.OperationalInsights;
using Microsoft.Azure.OperationalInsights.Models;
using System.Data;
using System.Runtime.InteropServices.ComTypes;
using System.Web.Helpers;

namespace GraphFunction
{
    public static class Function1
    {
        private static HttpClient client = new HttpClient();

        // Get WorkspaceID, ClientID and Client Secret from Azure KeyVault
        private static string workspaceId = System.Environment.GetEnvironmentVariable("LAWorkSpaceIDFromAKV");
        private static string clientId = System.Environment.GetEnvironmentVariable("ClientIDFromAKV");
        private static string clientSecret = System.Environment.GetEnvironmentVariable("ClientSecretFromAKV");
        private static string tenantId = System.Environment.GetEnvironmentVariable("AzTenantIdFromAKV");
        private static string domain = System.Environment.GetEnvironmentVariable("AzDomainNameFromAKV");


        // Configure app builder for the Microsoft Graph AccessToken
        private static string authority = $"https://login.microsoftonline.com/{tenantId}";
        private static IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority(new Uri(authority))
            .Build();

        public class AIPUser
        {
            public string displayName { get; set; }
            public string Department { get; set; }
            public string Country { get; set; }
            public string userObjecId { get; set; }
        }

        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, HttpRequest adminUPN,
            ILogger log)
        {

            // Acquire tokens for Graph API
            var scopes = new[] { "https://graph.microsoft.com/.default" };
            var authenticationResult = await app.AcquireTokenForClient(scopes).ExecuteAsync();

            string name = req.Query["name"];
            string adminEmail = req.Query["adminUPN"];

            log.LogInformation($"Admin's Email: {adminEmail}");

            // Extrackt user email from the HttpRequest
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            // GET USER COUNTRY
            string userCountry = await GetCountry(name, authenticationResult.AccessToken);
            log.LogInformation($"User's Country: {userCountry}");

            // MAKE SURE THE ADMIN IS AUTHORIZED TO ACCES THE USER'S LOGS
            bool IsAdminCheck = await CofirmIsAdmin(adminEmail, authenticationResult.AccessToken);
            log.LogInformation($"Is Member of Admin's Group?: {IsAdminCheck}");

            // GET ADMIN COUNTRY
            string adminCountry = await GetCountry(adminEmail, authenticationResult.AccessToken);
            log.LogInformation($"Admin's Country: {adminCountry}");

            QueryResults outputTable = null;
            // ONLY allow the query to run if:
            // 1. The Admin is member of the specified Azure AD Group
            // 2. The admin and the user are in the same country
            if (IsAdminCheck && userCountry == adminCountry)
            {
                outputTable = await RunLAQuery(name);
            }
            else
            {
                // If the user and admin's countries do not match, return this message.
                string noAccessReturn = "You do not have access to the data requested. " +
                    "\nIf you believe you have recieved this message in error, please contact your local compliance office.";

                return new BadRequestObjectResult(noAccessReturn);
            }

            // Return table to back to calling user
            return name != null
                ? (ActionResult)new OkObjectResult(outputTable)
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body.");
        }

        // Call Microsoft Graph to get user and admin country values
        public static async Task<String> GetCountry(string userUPN, string AccessToken)
        {

            var graphUri = $"https://graph.microsoft.com/v1.0/users/{userUPN}?$select=displayName,Department,Country,id";

            var req = new HttpRequestMessage();
            req.Method = HttpMethod.Get;
            req.RequestUri = new Uri(graphUri);
            req.Headers.Add("Authorization", "bearer " + AccessToken);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // User OData Call
            HttpResponseMessage userResponse = await client.SendAsync(req);

            // User Response
            var userObject = await userResponse.Content.ReadAsAsync<AIPUser>();
            return userObject.Country;

        }


      
        // Call Microsoft Graph to get admin Group Membership
        public static async Task<Boolean> CofirmIsAdmin(string adminUPN, string AccessToken)
        {
            //The filter's ID is the group object ID.
            var graphUri = $"https://graph.microsoft.com/v1.0/users/{adminUPN}/memberOf?$filter=id%20eq%20'AZURE_AD_GROUP_OBJECT_ID'";
            
            bool AdminIsAMember = false;

            var req = new HttpRequestMessage();
            req.Method = HttpMethod.Get;
            req.RequestUri = new Uri(graphUri);
            req.Headers.Add("Authorization", "bearer " + AccessToken);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // User OData Call
            HttpResponseMessage userResponse = await client.SendAsync(req);

            // Check response status: 200 = Group membership confirmed. 404 = Membership not found.
            if (userResponse.IsSuccessStatusCode)
            {
                AdminIsAMember = true;
            }

            return AdminIsAMember;

        }
        public static async Task<QueryResults> RunLAQuery(string username)
        {

            var authEndpoint = "https://login.microsoftonline.com";
            var tokenAudience = "https://api.loganalytics.io/";

            var adSettings = new ActiveDirectoryServiceSettings
            {
                AuthenticationEndpoint = new Uri(authEndpoint),
                TokenAudience = new Uri(tokenAudience),
                ValidateAuthority = true
            };

            var creds = ApplicationTokenProvider.LoginSilentAsync(domain, clientId, clientSecret, adSettings).GetAwaiter().GetResult();
            var LAclient = new OperationalInsightsDataClient(creds)
            {
                WorkspaceId = workspaceId
            };

            // Log Analytics Kusto query - look for user data in the past 20 days
            string query = @"
                InformationProtectionLogs_CL
                | where TimeGenerated >= ago(20d)
                | where UserId_s == 'USERNAME@DOMAIN.COM'
                | where ProtectionOwner_s == 'USERNAME@DOMAIN.COM'
                | where ObjectId_s != 'document1'
                | where MachineName_s != '' 
                | extend FileName = extract('((([a-zA-Z0-9\\s_:]*\\.[a-z]{1,4}$))|([a-zA-Z0-9\\s_:]*$))', 1, ObjectId_s)
                | distinct FileName, Activity_s, LabelName_s, TimeGenerated, Protected_b, MachineName_s
                | sort by TimeGenerated desc nulls last";

            // update the query with caller user's email
            string query1 = query.Replace("USERNAME@DOMAIN.COM", username);

            var outputTable = await LAclient.QueryAsync(query1.Trim());

            return outputTable;
        }

    }
}
