using Google;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using YTPlaylistSaverWebApi.Models;

namespace YTPlaylistSaverWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PlaylistsController : ControllerBase
    {
        [HttpGet("{playlistId}")]
        public IEnumerable<Models.Playlist> GetPlaylistHistory(string playlistId)
        {
            var playlists = new List<Models.Playlist>();

            using (var connection = new SqliteConnection("Data Source=database.db"))
            {
                connection.Open();

                try
                {
                    var playlistCommand = connection.CreateCommand();
                    playlistCommand.CommandText =
                    @"
                        SELECT time_saved, title
                        FROM playlist
                        WHERE playlist.id=@playlist_id
                        ORDER BY playlist.time_saved ASC
                    ";
                    playlistCommand.Parameters.AddWithValue("@playlist_id", playlistId);

                    using (var reader = playlistCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            playlists.Add(new Models.Playlist
                            {
                                Id = playlistId,
                                TimeSaved = reader.GetDateTime(0),
                                Title = reader.GetString(1)
                            });
                        }
                    }
                }
                catch (SqliteException)
                {
                    // TODO: return something like NotFound() 
                    return new List<Models.Playlist>();
                }
            }

            return playlists;
        }

        // user can get timeSaved from calling GetPlaylistHistory(string playlistId)
        [HttpGet("{playlistId}/{timeSavedString}")]
        public IEnumerable<Models.Video> GetVideos(string playlistId, string timeSavedString)
        {
            DateTime timeSaved = DateTime.Parse(timeSavedString);
            var videos = new List<Models.Video>();

            using (var connection = new SqliteConnection("Data Source=database.db"))
            {
                connection.Open();

                try
                {
                    // TODO: also get title of videos...
                    var playlistCommand = connection.CreateCommand();
                    playlistCommand.CommandText =
                    @"
                        SELECT video_id, video_index
                        FROM video_in_playlist
                        WHERE playlist_id=@playlist_id AND playlist_time_saved=@timeSaved
                        ORDER BY video_index ASC
                    ";
                    playlistCommand.Parameters.AddWithValue("@playlist_id", playlistId);
                    playlistCommand.Parameters.AddWithValue("@timeSaved", timeSaved);

                    using (var reader = playlistCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            videos.Add(new Models.Video
                            {
                                Id = reader.GetString(0)
                            });
                        }
                    }
                }
                catch (SqliteException)
                {
                    // TODO: return something like NotFound() 
                    return new List<Models.Video>();
                }
            }

            return videos;
        }

        // still make the user use their own key...
        // TODO: probably shouldnt be copy pasting all this from the other project (including the private methods)
        [HttpPost("{playlistId}")]
        public async Task<IActionResult> UploadNew(string playlistId, [FromQuery] string youtubeApiKey)
        {
            Models.Playlist playlist = new Models.Playlist()
            {
                Id = playlistId
            };

            List<PlaylistItemListResponse> playlistItemListResponses;
            try
            {
                playlistItemListResponses = await getPlaylistItemResults(playlist.Id, youtubeApiKey);
                playlist.Title = await getPlaylistTitle(playlist.Id, youtubeApiKey);
            }
            catch (GoogleApiException)
            {
                return NotFound();
            }

            using (var connection = new SqliteConnection("Data Source=database.db"))
            {
                connection.Open();

                playlist.TimeSaved = DateTime.Now;

                try
                {
                    // Save the playlist
                    var playlistCommand = connection.CreateCommand();
                    playlistCommand.CommandText = @"INSERT INTO playlist VALUES(@id, @time_saved, @title)";
                    playlistCommand.Parameters.AddWithValue("@id", playlist.Id);
                    playlistCommand.Parameters.AddWithValue("@time_saved", playlist.TimeSaved);
                    playlistCommand.Parameters.AddWithValue("@title", playlist.Title);
                    playlistCommand.ExecuteNonQuery();

                    int currentVideoIndex = 0;
                    foreach (var playlistItemListResponse in playlistItemListResponses)
                    {
                        foreach (var playlistItem in playlistItemListResponse.Items)
                        {
                            // Save the video
                            var videoCommand = connection.CreateCommand();
                            videoCommand.CommandText = @"INSERT OR REPLACE INTO video VALUES(@id, @title, @channel_title, @channel_id)";
                            videoCommand.Parameters.AddWithValue("@id", playlistItem.ContentDetails.VideoId);
                            videoCommand.Parameters.AddWithValue("@title", playlistItem.Snippet.Title);
                            videoCommand.Parameters.AddWithValue("@channel_title", playlistItem.Snippet.VideoOwnerChannelTitle);
                            videoCommand.Parameters.AddWithValue("@channel_id", playlistItem.Snippet.VideoOwnerChannelId);
                            videoCommand.ExecuteNonQuery();

                            // Save the relation
                            var relationCommand = connection.CreateCommand();
                            relationCommand.CommandText = @"INSERT INTO video_in_playlist VALUES(@playlist_id, @playlist_time_saved, @video_id, @video_index)";
                            relationCommand.Parameters.AddWithValue("@playlist_id", playlist.Id);
                            relationCommand.Parameters.AddWithValue("@playlist_time_saved", playlist.TimeSaved);
                            relationCommand.Parameters.AddWithValue("@video_id", playlistItem.ContentDetails.VideoId);
                            relationCommand.Parameters.AddWithValue("@video_index", currentVideoIndex);
                            relationCommand.ExecuteNonQuery();

                            currentVideoIndex++;
                        }
                    }
                }
                catch (SqliteException)
                {
                    return NotFound();
                }
            }

            return CreatedAtAction(nameof(UploadNew), playlist);
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