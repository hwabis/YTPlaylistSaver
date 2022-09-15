using Google;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace YTPlaylistSaver
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void exampleClick(object sender, RoutedEventArgs e)
        {
            const string examplePlaylistId = "PL06C2DC7DDCDEBAFE";

            PlaylistIdTextBox.Text = examplePlaylistId;
        }

        private async void saveClick(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Text = "Saving...";

            string playlistId = PlaylistIdTextBox.Text;

            List<PlaylistItemListResponse> playlistItemListResponses;
            string playlistTitle;
            try
            {
                playlistItemListResponses = await getPlaylistItemResults(playlistId, ApiKeyPasswordBox.Password);
                playlistTitle = await getPlaylistTitle(playlistId, ApiKeyPasswordBox.Password);
            }
            catch (GoogleApiException)
            {
                StatusTextBlock.Text = "Save failed; check the API key and playlist ID.";
                return;
            }

            using (var connection = new SqliteConnection("Data Source=database.db"))
            {
                connection.Open();

                DateTime currentDateTime = DateTime.Now;

                try
                {
                    // Save the playlist
                    var playlistCommand = connection.CreateCommand();
                    playlistCommand.CommandText = @"INSERT INTO playlist VALUES(@id, @time_saved, @title)";
                    playlistCommand.Parameters.AddWithValue("@id", playlistId);
                    playlistCommand.Parameters.AddWithValue("@time_saved", currentDateTime);
                    playlistCommand.Parameters.AddWithValue("@title", playlistTitle);
                    playlistCommand.ExecuteNonQuery();


                    int currentVideoIndex = 0;
                    foreach (var playlistItemListResponse in playlistItemListResponses)
                    {
                        foreach (var playlistItem in playlistItemListResponse.Items)
                        {
                            // Save the video
                            var videoCommand = connection.CreateCommand();
                            videoCommand.CommandText = @"INSERT OR REPLACE INTO video VALUES(@id, @title, @channel_id, @channel_title)";
                            videoCommand.Parameters.AddWithValue("@id", playlistItem.ContentDetails.VideoId);
                            videoCommand.Parameters.AddWithValue("@title", playlistItem.Snippet.Title);
                            videoCommand.Parameters.AddWithValue("@channel_id", playlistItem.Snippet.VideoOwnerChannelId);
                            videoCommand.Parameters.AddWithValue("@channel_title", playlistItem.Snippet.VideoOwnerChannelTitle);
                            videoCommand.ExecuteNonQuery();

                            // Save the relation
                            var relationCommand = connection.CreateCommand();
                            relationCommand.CommandText = @"INSERT INTO video_in_playlist VALUES(@playlist_id, @playlist_time_saved, @video_id, @video_index)";
                            relationCommand.Parameters.AddWithValue("@playlist_id", playlistId);
                            relationCommand.Parameters.AddWithValue("@playlist_time_saved", currentDateTime);
                            relationCommand.Parameters.AddWithValue("@video_id", playlistItem.ContentDetails.VideoId);
                            relationCommand.Parameters.AddWithValue("@video_index", currentVideoIndex);
                            relationCommand.ExecuteNonQuery();

                            currentVideoIndex++;
                        }
                    }

                    StatusTextBlock.Text = "Done."; // We move to next window instantly anyway
                }
                catch (SqliteException ex)
                {
                    StatusTextBlock.Text = "Error.";
                    MessageBox.Show(ex.ToString(), "Error");
                    return;
                }
            }

            // Good enough...
            new ResultsWindow().Show();
            Close();
        }

        private async Task<List<PlaylistItemListResponse>> getPlaylistItemResults(string playlistId, string apiKey)
        {
            List<PlaylistItemListResponse> responses = new List<PlaylistItemListResponse>();
            int resultsPerResponse = 5;

            var service = new YouTubeService(new BaseClientService.Initializer
            {
                ApiKey = apiKey
            });
            var request = new PlaylistItemsResource.ListRequest(service, "snippet, contentDetails")
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

        private async Task<string> getPlaylistTitle(string playlistId, string apiKey)
        {
            var service = new YouTubeService(new BaseClientService.Initializer
            {
                ApiKey = apiKey
            });
            var request = new PlaylistsResource.ListRequest(service, "snippet")
            {
                Id = playlistId,
                MaxResults = 1
            };

            var response = await request.ExecuteAsync();

            return response.Items[0].Snippet.Title;
        }
    }
}
