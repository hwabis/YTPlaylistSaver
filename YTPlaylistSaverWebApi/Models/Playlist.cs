using System.ComponentModel.DataAnnotations;

namespace YTPlaylistSaverWebApi.Models
{
    public class Playlist
    {
        [Required]
        public string? Id { get; set; }

        public string? Title;

        public DateTime TimeSaved { get; set; }
    }
}