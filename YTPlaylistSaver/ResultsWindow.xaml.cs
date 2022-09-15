using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace YTPlaylistSaver
{
    public partial class ResultsWindow : Window
    {
        public ObservableCollection<ResultTableModel> TableItems { get; private set; }

        private DateTime currentPlaylistDatetime;
        /// <summary>
        /// List of datetimes for the playlist, ordered from early --> late
        /// </summary>
        private List<DateTime> playlistDatetimes;
        private int currentDateTimeIndex;

        private string playlistId;

        public ResultsWindow(string playlistId, string playlistTitle)
        {
            InitializeComponent();

            DataContext = this;
            TableItems = new ObservableCollection<ResultTableModel>();

            this.playlistId = playlistId;
            PlaylistHeaderTextBlock.Text = $"Playlist:\n {playlistTitle}";

            playlistDatetimes = new List<DateTime>();
            using (var connection = new SqliteConnection("Data Source=database.db"))
            {
                connection.Open();

                try
                {
                    var playlistCommand = connection.CreateCommand();
                    playlistCommand.CommandText =
                    @"
                        SELECT playlist.time_saved
                        FROM playlist
                        WHERE playlist.id=@playlist_id
                        ORDER BY playlist.time_saved ASC
                    ";
                    playlistCommand.Parameters.AddWithValue("@playlist_id", playlistId);

                    using (var reader = playlistCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            playlistDatetimes.Add(reader.GetDateTime(0));
                        }
                    }
                }
                catch (SqliteException ex)
                {
                    MessageBox.Show(ex.ToString(), "Error");
                    return;
                }
            }

            currentDateTimeIndex = playlistDatetimes.Count - 1;
            currentPlaylistDatetime = playlistDatetimes[currentDateTimeIndex];
            updateCurrentDatetimeTextBlock();

            updateTable();
        }

        private void goLeftDatetime(object sender, RoutedEventArgs e)
        {
            if (currentDateTimeIndex <= 0)
                return;

            currentPlaylistDatetime = playlistDatetimes[--currentDateTimeIndex];
            updateCurrentDatetimeTextBlock();

            updateTable();
        }

        private void goRightDatetime(object sender, RoutedEventArgs e)
        {
            if (currentDateTimeIndex >= playlistDatetimes.Count - 1)
                return;

            currentPlaylistDatetime = playlistDatetimes[++currentDateTimeIndex];
            updateCurrentDatetimeTextBlock();

            updateTable();
        }

        private void goToMainWindow(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            Close();
        }

        private void updateCurrentDatetimeTextBlock()
        {
            CurrentPlaylistDatetimeTextBlock.Text = currentPlaylistDatetime.ToString();
        }

        /// <summary>
        /// Updates the videos table, getting the correct playlist version using <see cref="currentPlaylistDatetime"/>.
        /// </summary>
        private void updateTable()
        {
            using (var connection = new SqliteConnection("Data Source=database.db"))
            {
                connection.Open();

                try
                {
                    var playlistCommand = connection.CreateCommand();
                    playlistCommand.CommandText =
                    @"
                        SELECT video.title, video.id, video.channel_title
                        FROM video_in_playlist INNER JOIN video ON video_in_playlist.video_id=video.id
                        WHERE video_in_playlist.playlist_id=@playlist_id AND video_in_playlist.playlist_time_saved=@playlist_time_saved
                        ORDER BY video_in_playlist.video_index ASC
                    ";
                    playlistCommand.Parameters.AddWithValue("@playlist_id", playlistId);
                    playlistCommand.Parameters.AddWithValue("@playlist_time_saved", currentPlaylistDatetime);

                    TableItems.Clear();
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
