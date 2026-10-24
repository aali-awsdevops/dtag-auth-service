using AuthorizationService.Data;
using SharedAuth.Extensions;
using AuthorizationService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthorizationService.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class PermissionsController : ControllerBase
    {
        private readonly AuthDbContext _db;

        public PermissionsController(AuthDbContext db)
        {
            _db = db;
        }

        public class CreatePermissionRequest
        {
            public Guid UserId { get; set; }
            public string Permission { get; set; } = null!;
        }

        public class UpdatePermissionRequest
        {
            public string Permission { get; set; } = null!;
        }

        // GET /Permissions?userId={userId}
        [HttpGet]
        public async Task<IActionResult> GetPermissions([FromQuery] Guid? userId)
        {
            IQueryable<AuthUserPermission> query = _db.AuthUserPermissions;
            if (userId.HasValue)
            {
                query = query.Where(p => p.UserId == userId.Value);
            }
            else
            {
                var loginUserId = this.User.GetUserId();
                if (loginUserId != null)
                {
                    query = query.Where(p => p.UserId == loginUserId.Value);
                }
                else
                {
                    return BadRequest("Invalid User");
                }
            }

                var list = await query.AsNoTracking()
                    .Select(p => new { p.Id, p.UserId, p.Permission, p.GrantedAt })
                    .ToListAsync();

            return Ok(list);
        }

        // GET /Permissions/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetPermission(Guid id)
        {
            var perm = await _db.AuthUserPermissions.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (perm == null) return NotFound();
            return Ok(new { perm.Id, perm.UserId, perm.Permission, perm.GrantedAt });
        }

        // POST /Permissions
        [HttpPost]
        public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequest request)
        {
            if (request == null || request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.Permission))
                return BadRequest("Invalid request");

            var userExists = await _db.AuthUsers.AnyAsync(u => u.Id == request.UserId);
            if (!userExists) return NotFound("User not found");

            var permission = new AuthUserPermission
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Permission = request.Permission.Trim(),
                GrantedAt = DateTime.UtcNow
            };

            _db.AuthUserPermissions.Add(permission);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPermission), new { id = permission.Id }, new { permission.Id, permission.UserId, permission.Permission, permission.GrantedAt });
        }

        // PUT /Permissions/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdatePermission(Guid id, [FromBody] UpdatePermissionRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Permission))
                return BadRequest("Invalid request");

            var existing = await _db.AuthUserPermissions.FirstOrDefaultAsync(p => p.Id == id);
            if (existing == null) return NotFound();

            existing.Permission = request.Permission.Trim();
            _db.AuthUserPermissions.Update(existing);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // DELETE /Permissions/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeletePermission(Guid id)
        {
            var existing = await _db.AuthUserPermissions.FirstOrDefaultAsync(p => p.Id == id);
            if (existing == null) return NotFound();

            _db.AuthUserPermissions.Remove(existing);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
