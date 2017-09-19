using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Tooling.Connector;
using Xrm.Tools.WebAPI;

namespace EpicXrm.TestFramework.CodeGenerator
{
    class MainClass
    {
        private static IOrganizationService _service;
        public static void Main(string[] args)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("AuthType=OAuth;");
            builder.Append($"AppId=application-id;");
            builder.Append("RedirectUri=https://arbitrary;");
            builder.Append("Username=user@adname.onmicrosoft.com;");
            builder.Append("Password=passcode;");
            builder.Append("Url=https://orgname.crm.dynamics.com;");

            CrmServiceClient client = new CrmServiceClient(builder.ToString());

            while(!client.IsReady)
            {
                Console.WriteLine(client.LastCrmError);
                Thread.Sleep(500);
            }
        }

		public static void GetAPI()
		{
			string clientId = "guid-from-azure";
			string crmBaseUrl = "https://orgname.crm.dynamics.com";
            string relationships = $"{crmBaseUrl}/api/data/v8.2/";
            // AdalAuthentication auth = new AdalAuthentication("common", new Uri(crmBaseUrl), clientId, new System.Net.NetworkCredential("user@adname.onmicrosoft.com", "passcode"));
            var token = ""; // auth.GetAuthTokenAsync().Result.AccessToken;

			var client = new HttpClient();
			client.BaseAddress = new Uri(relationships);
			client.DefaultRequestHeaders.Accept.Clear();
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue($"application/json"));
            client.DefaultRequestHeaders.Add("Authorization", ""); // auth.GetAuthHeader());

            var result = client.GetStringAsync("RelationshipDefinitions").Result;
			// CRMWebAPI api = new CRMWebAPI(crmBaseUrl + "/api/data/v8.0/", token);

			// return api;
		}

        public static void ConnectToMSCRM(string UserName, string Password, string SoapOrgServiceUri)
        {
        	try
        	{
        		ClientCredentials credentials = new ClientCredentials();
        		credentials.UserName.UserName = UserName;
        		credentials.UserName.Password = Password;
        		Uri serviceUri = new Uri(SoapOrgServiceUri);
        		OrganizationServiceProxy proxy = new OrganizationServiceProxy(serviceUri, null, credentials, null);
        		proxy.EnableProxyTypes();
        		_service = (IOrganizationService)proxy;
        	}
        	catch (Exception ex)
        	{
        		Console.WriteLine("Error while connecting to CRM " + ex.Message);
        		Console.ReadKey();
        	}
        }
    }
}
