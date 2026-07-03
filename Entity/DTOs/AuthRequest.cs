using System.ComponentModel.DataAnnotations;

namespace Entity.DTOs;

public class RegisterRequest
{
    [Required(ErrorMessage = "Username is required")]
    [StringLength(64, MinimumLength = 4, ErrorMessage = "Username must be between 4 and 64 characters")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(256, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 256 characters")]
    public string Password { get; set; } = string.Empty;
}
