using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace UserDetailsClient
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();

            await SignInAsync();
        }

        async void OnSignInSignOut(object sender, EventArgs e)
        {
            if (btnSignInSignOut.Text == "Sign in")
            {
                await SignInAsync();
            }
            else
            {
                await SignOutAsync();
            }
        }


        public async Task SignOutAsync()
        {
            IEnumerable<IAccount> accounts = await App.PCA.GetAccountsAsync();

            try
            {
                while (accounts.Any())
                {
                    await App.PCA.RemoveAsync(accounts.FirstOrDefault());
                    accounts = await App.PCA.GetAccountsAsync();
                }

                slUser.IsVisible = false;
                Device.BeginInvokeOnMainThread(() => { btnSignInSignOut.Text = "Sign in"; });

            }
            catch (Exception ex)
            {

            }
        }

        public async Task SignInAsync()
        {
            AuthenticationResult authResult = null;
            IEnumerable<IAccount> accounts = await App.PCA.GetAccountsAsync();


            // let's see if we have a user in our belly already
            try
            {
                IAccount firstAccount = accounts.FirstOrDefault();
                authResult = await App.PCA.AcquireTokenSilent(App.Scopes, firstAccount)
                                      .ExecuteAsync();
                await RefreshUserDataAsync(authResult.AccessToken).ConfigureAwait(false);
                Device.BeginInvokeOnMainThread(() => { btnSignInSignOut.Text = "Sign out"; });
            }
            catch (MsalUiRequiredException ex)
            {
                try
                {
                    authResult = await App.PCA.AcquireTokenInteractive(App.Scopes)
                                              .WithParentActivityOrWindow(App.ParentWindow)
                                              .ExecuteAsync();

                    await RefreshUserDataAsync(authResult.AccessToken);
                    Device.BeginInvokeOnMainThread(() => { btnSignInSignOut.Text = "Sign out"; });
                }
                catch (Exception ex2)
                {

                }
            }
        }

        public async Task RefreshUserDataAsync(string token)
        {
            //get data from API
            HttpClient client = new HttpClient();
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
            message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", token);
            HttpResponseMessage response = await client.SendAsync(message);
            string responseString = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                JObject user = JObject.Parse(responseString);

                slUser.IsVisible = true;

                Device.BeginInvokeOnMainThread(() =>
                {

                    lblDisplayName.Text = user["displayName"].ToString();
                    lblGivenName.Text = user["givenName"].ToString();
                    lblId.Text = user["id"].ToString();
                    lblSurname.Text = user["surname"].ToString();
                    lblUserPrincipalName.Text = user["userPrincipalName"].ToString();

                // just in case
                btnSignInSignOut.Text = "Sign out";
                });
            }
            else
            {
                await DisplayAlert("Something went wrong with the API call", responseString, "Dismiss");
            }
        }
    }
}
