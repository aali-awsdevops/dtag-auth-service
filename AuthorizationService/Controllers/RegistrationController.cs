using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuthorizationService.Models;
using AuthorizationService.Data;
using Npgsql;
using AuthorizationService.Utils;
using Microsoft.AspNetCore.Authorization;

namespace AuthorizationService.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class RegistrationController : ControllerBase
    {
        private readonly AuthDbContext _db;

        public RegistrationController(AuthDbContext db)
        {
            _db = db;
        }

        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser([FromBody] UserCreateRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Invalid request");

            var user = new AuthUser
            {
                Id = Guid.NewGuid(),
                CustomerId = request.CustomerId,
                Email = request.Email,
                DisplayName = request.DisplayName,
                CreatedAt = DateTime.UtcNow,
                PasswordHash = request.Password != null? PasswordHasher.Hash(request.Password) : null
            };

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.AuthUsers.Add(user);

                var permissions = new[] { "basic_access", "read_profile" };
                foreach (var perm in permissions)
                {
                    _db.AuthUserPermissions.Add(new AuthUserPermission
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        Permission = perm,
                        GrantedAt = DateTime.UtcNow
                    });
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return CreatedAtAction(null, new { id = user.Id }, new { id = user.Id, email = user.Email });
            }
            catch (DbUpdateException dbEx) when (dbEx.InnerException is PostgresException pg && pg.SqlState == "23505")
            {
                await tx.RollbackAsync();
                return Conflict("A user with the given email already exists.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, ex.Message);
            }
        }
    }
}
