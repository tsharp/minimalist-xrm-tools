namespace EpicXrm.TestFramework.CodeGenerator
{
	using System;
	using System.Configuration;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;

    public class AdalAuthentication
	{
        private AuthenticationResult authResult;
        private AuthenticationContext authContext;
        private string resourceUri;
        private string clientId;
        private NetworkCredential credentials;

        public AdalAuthentication(string domain, Uri resourceUri, string clientId, NetworkCredential credentials)
        {
            authContext = new AuthenticationContext($"https://login.microsoftonline.com/{domain}");
            this.resourceUri = resourceUri.ToString();
            this.clientId = clientId;
            this.credentials = credentials;
		}
		
		public Task<AuthenticationResult> GetAuthTokenAsync()
		{
			UserPasswordCredential credential = new UserPasswordCredential(credentials.UserName, credentials.Password);
            return authContext.AcquireTokenAsync(resourceUri.ToString(), clientId, credential);
		}

        public Task<AuthenticationResult> GetAuthTokenInteractive()
        {
			UserIdentifier userIdentifier = new UserIdentifier(credentials.UserName, UserIdentifierType.OptionalDisplayableId);
			return authContext.AcquireTokenAsync(resourceUri, clientId, new Uri("https://tsharp-redirect"), new PlatformParameters(PromptBehavior.Auto), userIdentifier);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="resourceURI"></param>
		/// <returns>Auth Header Data</returns>
		public string GetAuthHeader()
		{
			if (authResult == null || authResult.ExpiresOn < DateTimeOffset.UtcNow.AddSeconds(-10))
			{
				authResult = GetAuthTokenAsync().Result;
			}

			return authResult.CreateAuthorizationHeader();
		}
	}
}
