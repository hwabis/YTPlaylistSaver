namespace YTPlaylistSaver
{
    public class ResultTableModel
    {
        public string VideoName { get; set; }
        public string VideoId { get; set; }
        public string Uploader { get; set; }

        public ResultTableModel(string videoName, string videoId, string uploader)
        {
            VideoName = videoName;
            VideoId = videoId;
            Uploader = uploader;
        }
    }
}
