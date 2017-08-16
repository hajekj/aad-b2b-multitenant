A code sample accompanying an article [Creating a multi-tenant application which supports B2B users](https://hajekj.net/2017/07/24/creating-a-multi-tenant-application-which-supports-b2b-users/) on my [blog](https://hajekj.net).

# Setup instructions
1. [Create an Azure AD application](https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-integrating-applications#adding-an-application) in the [Portal](https://portal.azure.com).
2. Configure application's permissions to have access to *Windows Azure Service Management API* and also *Microsoft Graph* (add permissions to sign-in the user and read user's profile, read basic profiles of users and also access directory as currently signed in user)
3. Get the application's client id, client secret and configure the reply url to *http://localhost:5000/signin-oidc*
4. Replace the client id in the *appsettings.json* and place the client secret into [user secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets#secret-manager) or environmental variables if deploying to Azure.
