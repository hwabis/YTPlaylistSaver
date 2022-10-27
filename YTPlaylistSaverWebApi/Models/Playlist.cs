using System.ComponentModel.DataAnnotations;

namespace YTPlaylistSaverWebApi.Models
{
    public class Playlist
    {
        [Required]
        public string? Id { get; set; }

        [Required]
        public DateTime TimeSaved { get; set; }

        [Required]
        public string? Title;
    }
}