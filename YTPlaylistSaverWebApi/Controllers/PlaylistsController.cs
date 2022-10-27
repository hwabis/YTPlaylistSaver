using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using YTPlaylistSaverWebApi.Models;

namespace YTPlaylistSaverWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PlaylistsController : ControllerBase
    {
        // TODO: post playlist

        [HttpGet("{playlistId}")]
        public IEnumerable<Playlist> GetPlaylistHistory(string playlistId)
        {
            var playlists = new List<Playlist>();

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

                    using var reader = playlistCommand.ExecuteReader();
                    while (reader.Read())
                    {
                        playlists.Add(new Playlist
                        {
                            Id = playlistId,
                            TimeSaved = reader.GetDateTime(0),
                            Title = reader.GetString(1)
                        });
                    }
                }
                catch (SqliteException)
                {
                    // probably not the right way
                    return new List<Playlist>();
                }
            }

            return playlists;
        }

        // user can get timeSaved from calling GetPlaylistHistory(string playlistId)
        [HttpGet("{playlistId}/{timeSavedString}")]
        public IEnumerable<Video> GetVideos(string playlistId, string timeSavedString)
        {
            Console.WriteLine(timeSavedString);

            DateTime timeSaved = DateTime.Parse(timeSavedString);
            var videos = new List<Video>();

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

                    using var reader = playlistCommand.ExecuteReader();
                    while (reader.Read())
                    {
                        videos.Add(new Video
                        {
                            Id = reader.GetString(0)
                        });
                    }
                }
                catch (SqliteException)
                {
                    // probably not the right way
                    return new List<Video>();
                }
            }

            return videos;
        }
    }
}