using Microsoft.Data.Sqlite;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace YTPlaylistSaver
{
    public partial class ResultsWindow : Window
    {
        public ObservableCollection<ResultTableModel> TableItems { get; private set; }
        public string PlaylistTitle { get; private set; }
        public DateTime CurrentPlaylistDatetime { get; private set; }

        private string playlistId;

        public ResultsWindow(string playlistId, string playlistTitle, DateTime dateTime)
        {
            InitializeComponent();

            DataContext = this;
            TableItems = new ObservableCollection<ResultTableModel>();

            this.playlistId = playlistId;
            PlaylistTitle = $"Viewing playlist {playlistTitle} at time:";
            CurrentPlaylistDatetime = dateTime;

            updateTable();
        }

        private void goToMainWindow(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            Close();
        }

        /// <summary>
        /// Updates the videos table, getting the correct playlist version using <see cref="CurrentPlaylistDatetime"/>.
        /// </summary>
        private void updateTable()
        {
            using (var connection = new SqliteConnection("Data Source=database.db"))
            {
                connection.Open();

                try
                {
                    // Get the entries from the video_in_playlist table with an id matching playlistId
                    // and with the latest playlist_time_saved
                    var playlistCommand = connection.CreateCommand();
                    playlistCommand.CommandText =
                    @"
                        SELECT video.title, video.id, video.channel_title
                        FROM video_in_playlist INNER JOIN video ON video_in_playlist.video_id=video.id
                        WHERE video_in_playlist.playlist_id=@playlist_id AND video_in_playlist.playlist_time_saved=@playlist_time_saved
                        ORDER BY video_in_playlist.video_index ASC
                    ";
                    playlistCommand.Parameters.AddWithValue("@playlist_id", playlistId);
                    playlistCommand.Parameters.AddWithValue("@playlist_time_saved", CurrentPlaylistDatetime);

                    using (var reader = playlistCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            TableItems.Add(new ResultTableModel(reader.GetString(0), reader.GetString(1), reader.GetString(2)));
                        }
                    }
                }
                catch (SqliteException ex)
                {
                    MessageBox.Show(ex.ToString(), "Error");
                    return;
                }
            }
        }
    }
}
