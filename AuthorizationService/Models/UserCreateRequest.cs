using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AuthorizationService.Models
{
    public class UserCreateRequest
    {
        public int? CustomerId { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }
        // Optional plain-text password when creating a user. If provided it will be hashed before storage.
        public string? Password { get; set; }
        public string DisplayName { get; set; }
    }

    public class AuthUser
    {
        public Guid Id { get; set; }
        public int? CustomerId { get; set; }
        public string Email { get; set; } = null!;
        // Stored password hash (PBKDF2)
        public string? PasswordHash { get; set; }
        public string? DisplayName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<AuthUserPermission> Permissions { get; set; } = new List<AuthUserPermission>();
    }

    public class AuthUserPermission
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Permission { get; set; } = null!;
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

        public AuthUser? User { get; set; }
    }
}
