using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public sealed record CreateUserRequest(
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    string Name);