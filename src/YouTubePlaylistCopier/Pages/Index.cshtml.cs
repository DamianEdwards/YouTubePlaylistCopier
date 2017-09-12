using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Google.Apis.YouTube.v3.Data;
using Google.Apis.YouTube.v3;
using Google.Apis.Services;
using Google.Apis.Auth.OAuth2;
using System.ComponentModel.DataAnnotations;
using Google;

namespace YouTubePlaylistCopier.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Display(Name = "Source playlist ID")]
            [Required]
            public string SourcePlaylistId { get; set; }

            [Display(Name = "Destination playlist ID")]
            [Required]
            public string DestinationPlaylistId { get; set; }
        }

        [TempData]
        public string Message { get; set; }

        public bool ShowMessage => !string.IsNullOrEmpty(Message);

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var credential = GoogleCredential.FromAccessToken(await HttpContext.GetTokenAsync("access_token"));

            try
            {
                using (var client = new YouTubeService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "live.asp.net"
                }))
                {
                    var count = await CopyItems(client);
                    Message = $"{count} items copied successfully!";
                }
            }
            catch (GoogleApiException ex)
            {
                if (ex.Error != null)
                {
                    ModelState.AddModelError("", $"Google returned the following error: {ex.Error.Message}");
                    return Page();
                }

                throw;
            }

            return RedirectToPage("/Index");
        }

        private async Task<int> CopyItems(YouTubeService client, string nextPageToken = null)
        {
            var count = 0;
            var listRequest = client.PlaylistItems.List("snippet");
            listRequest.PageToken = nextPageToken;
            listRequest.PlaylistId = Input.SourcePlaylistId;
            listRequest.MaxResults = 50;

            var playlistItems = await listRequest.ExecuteAsync();
            var sourceItems = playlistItems.Items;

            foreach (var item in sourceItems)
            {
                var newItem = new PlaylistItem
                {
                    Snippet = new PlaylistItemSnippet
                    {
                        PlaylistId = Input.DestinationPlaylistId,
                        ResourceId = item.Snippet.ResourceId
                    }
                };

                var addRequest = client.PlaylistItems.Insert(newItem, "snippet");
                await addRequest.ExecuteAsync();
                count++;
            }

            if (!string.IsNullOrEmpty(playlistItems.NextPageToken))
            {
                count += await CopyItems(client, playlistItems.NextPageToken);
            }

            return count;
        }

        public IActionResult OnPostLogin() => Challenge(new AuthenticationProperties { RedirectUri = Url.Page("/Index") }, GoogleDefaults.AuthenticationScheme);
    }
}
