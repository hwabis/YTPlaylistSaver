using Google;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
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
            var response = await getAndSavePlaylistResults(PlaylistIdTextBox.Text, ApiKeyPasswordBox.Password);

            if (response != null)
            {
                /*
                foreach (var playlistItem in response.Items)
                {
                    StatusTextBlock.Text += "\n" + playlistItem.Snippet.Title + playlistItem.Snippet.VideoOwnerChannelTitle;
                }
                */

                // Good enough...
                new ResultsWindow().Show();
                Close();
            }
            else
            {
                StatusTextBlock.Text = "Save failed; check the API key and playlist ID.";
            }
        }

        private async Task<PlaylistItemListResponse?> getAndSavePlaylistResults(string playlistId, string apiKey)
        {
            var service = new YouTubeService(new BaseClientService.Initializer
            {
                ApiKey = apiKey
            });
            var request = new PlaylistItemsResource.ListRequest(service, "snippet")
            {
                PlaylistId = playlistId,
                MaxResults = 5000
            };

            try
            {
                var response = await request.ExecuteAsync();

                // TODO: save to database

                return response;
            }
            catch (GoogleApiException)
            {
                return null;
            }
        }
    }
}
