using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public sealed record ScheduleMeetingRequest(
    [Required(ErrorMessage = "Participant IDs are required")]
    [MinLength(1, ErrorMessage = "At least one participant is required")]
    List<int> ParticipantIds,

    [Range(15, 480, ErrorMessage = "Duration must be between 15 and 480 minutes")]
    int DurationMinutes,

    [Required(ErrorMessage = "Earliest start time is required")]
    DateTime EarliestStart,

    [Required(ErrorMessage = "Latest end time is required")]
    DateTime LatestEnd);