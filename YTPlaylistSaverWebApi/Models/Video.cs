using System.ComponentModel.DataAnnotations;

namespace YTPlaylistSaverWebApi.Models
{
    public class Video
    {
        [Required]
        public string? Id { get; set; }

        [Required]
        public string? Title { get; set; }

        public string? ChannelTitle { get; set; }

        public string? ChannelId { get; set; }
    }
}