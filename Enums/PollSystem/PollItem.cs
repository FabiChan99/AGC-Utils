using System.ComponentModel.DataAnnotations;

namespace AGC_Management.Enums.PollSystem;

public class PollItem
{
    public string Id { get; set; }

    [Required(ErrorMessage = "Der Name der Umfrage ist erforderlich.")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Der Text der Umfrage ist erforderlich.")]
    public string Text { get; set; }
    [Required(ErrorMessage = "Der Kanal der Umfrage ist erforderlich.")]
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }

    public bool isExpiring { get; set; } = false;
    public DateTime ExpiryDate { get; set; } = DateTime.Now;
    public TimeOnly ExpiryTime { get; set; } = new(23, 59, 59);

    public bool isAnonymous { get; set; } = false;
    public bool isMultiChoice { get; set; } = false;

    [Required(ErrorMessage = "Mindestens eine Option ist erforderlich.")]
    public List<PollOption> Options { get; set; }

    public ulong CreatorId { get; set; }
}