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
                        id VARCHAR(100) PRIMARY KEY,
                        name VARCHAR(100)
                    );

                    CREATE TABLE IF NOT EXISTS video (
                        id VARCHAR(100) PRIMARY KEY,
                        name VARCHAR(100)
                    );

                    CREATE TABLE IF NOT EXISTS video_in_playlist (
                        playlist_id VARCHAR(100),
                        video_id VARCHAR(100),
                        video_index INTEGER,
                        FOREIGN KEY(playlist_id) REFERENCES playlist(id), 
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
