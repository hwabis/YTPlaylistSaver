using Google;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace YTPlaylistSaver
{
    public partial class MainWindow : Window
    {
        private const string examplePlaylistId = "PL06C2DC7DDCDEBAFE";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void exampleClick(object sender, RoutedEventArgs e)
        {
            PlaylistIdTextBox.Text = examplePlaylistId;
        }

        private async void saveClick(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Text = "Saving...";

            try
            {
                var response = await getPlaylistItemResults(PlaylistIdTextBox.Text, ApiKeyPasswordBox.Password);
            }
            catch (GoogleApiException)
            {
                StatusTextBlock.Text = "Save failed; check the API key and playlist ID.";
                return;
            }

            /*
            foreach (var playlistItem in response.Items)
            {
                StatusTextBlock.Text += "\n" + playlistItem.Snippet.Title + playlistItem.Snippet.VideoOwnerChannelTitle;
            }
            */

            // TODO: Save to database

            // Good enough...
            new ResultsWindow().Show();
            Close();
        }

        private async Task<List<PlaylistItemListResponse>> getPlaylistItemResults(string playlistId, string apiKey)
        {
            List<PlaylistItemListResponse> responses = new List<PlaylistItemListResponse>();
            int resultsPerResponse = 50;

            var service = new YouTubeService(new BaseClientService.Initializer
            {
                ApiKey = apiKey
            });
            var request = new PlaylistItemsResource.ListRequest(service, "snippet")
            {
                PlaylistId = playlistId,
                MaxResults = resultsPerResponse
            };

            // This works I guess...
            var response = await request.ExecuteAsync();
            responses.Add(response);
            for (int i = resultsPerResponse; i < response.PageInfo.TotalResults; i += resultsPerResponse)
            {
                request.PageToken = response.NextPageToken;
                response = await request.ExecuteAsync();
                responses.Add(response);
            }

            return responses;
        }
    }
}
