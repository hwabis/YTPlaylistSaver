using Microsoft.Data.Sqlite;
using System.Windows;

namespace YTPlaylistSaver
{
    public partial class App : Application
    {
        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            using (var connection = new SqliteConnection("Data Source=database.db"))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    CREATE TABLE IF NOT EXISTS playlist (
                        id VARCHAR(100),
                        time_saved DATETIME,
                        title VARCHAR(100),
                        PRIMARY KEY(id, time_saved)
                    );

                    CREATE TABLE IF NOT EXISTS video (
                        id VARCHAR(100) PRIMARY KEY,
                        title VARCHAR(100),
                        channel_title VARCHAR(100),
                        channel_id VARCHAR(100)
                    );

                    CREATE TABLE IF NOT EXISTS video_in_playlist (
                        playlist_id VARCHAR(100),
                        playlist_time_saved DATETIME,
                        video_id VARCHAR(100),
                        video_index INTEGER,
                        FOREIGN KEY(playlist_id, playlist_time_saved) REFERENCES playlist(id, time_saved),
                        FOREIGN KEY(video_id) REFERENCES video(id)
                    );
                ";

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (SqliteException ex)
                {
                    MessageBox.Show(ex.ToString(), "Error");
                    Shutdown();
                }
            }

            new MainWindow().Show();
        }
    }
}
