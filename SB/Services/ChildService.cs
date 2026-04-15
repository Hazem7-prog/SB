using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SB.DTOs;
using SB.Interfaces;
using SB.Models;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SB.Services
{
    public class ChildService : IChildService
    {
        private readonly SBDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ChildService> _logger;

        public ChildService(
            SBDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ChildService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ChildResponse> AddChildAsync(ChildRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var userId = GetCurrentUserId(); // may be null for anonymous callers

            var simNormalized = request.SimCardNum?.Trim();
            if (!string.IsNullOrWhiteSpace(simNormalized) && simNormalized.Length > 32)
                throw new ValidationException("SimCardNum must be 32 characters or fewer.");

            // uniqueness check as before but allow null userId; check across non-deleted entries:
            if (!string.IsNullOrWhiteSpace(simNormalized))
            {
                var simLower = simNormalized.ToLowerInvariant();
                var exists = await _dbContext.Children
                    .AsNoTracking()
                    .AnyAsync(c => c.SimCardNum != null && c.SimCardNum.ToLower() == simLower && !c.IsDeleted);

                if (exists)
                    throw new ValidationException("This phone number is already used.");
            }

            var child = new Child
            {
                Name = request.Name,
                Age = request.Age,
                Gender = request.Gender,
                SimCardNum = simNormalized,
                Image = request.Image,
                Notes = request.Notes,
                IsActive = true,
                IsDeleted = false,
                UserId = userId // may be null
            };

            _dbContext.Children.Add(child);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Child created (Id: {ChildId}) for UserId: {UserId}", child.ChildId, userId);

            return MapToResponse(child);
        }

        public async Task DeleteChildAsync(int id)
        {
            var userId = GetCurrentUserId();
            //if (string.IsNullOrWhiteSpace(userId))
            //    throw new UnauthorizedAccessException("Authenticated user required.");

            var child = await _dbContext.Children
                .Where(c => c.ChildId == id && c.UserId == userId && !c.IsDeleted)
                .FirstOrDefaultAsync();

            if (child == null)
                throw new KeyNotFoundException("Child not found.");

            child.IsDeleted = true;
            child.IsActive = false;

            _dbContext.Children.Update(child);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Child soft-deleted (Id: {ChildId}) for UserId: {UserId}", id, userId);
        }

        public async Task<IEnumerable<ChildResponse>> GetAllChildrenAsync()
        {
            var userId = GetCurrentUserId();
            //if (string.IsNullOrWhiteSpace(userId))
            //    throw new UnauthorizedAccessException("Authenticated user required.");

            var children = await _dbContext.Children
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .AsNoTracking()
                .ToListAsync();

            return children.Select(MapToResponse).ToList();
        }

        public async Task<ChildResponse> GetChildByIdAsync(int id)
        {
            var userId = GetCurrentUserId();
            //if (string.IsNullOrWhiteSpace(userId))
            //    throw new UnauthorizedAccessException("Authenticated user required.");

            var child = await _dbContext.Children
                .AsNoTracking()
                .Where(c => c.ChildId == id && c.UserId == userId && !c.IsDeleted)
                .FirstOrDefaultAsync();

            if (child == null)
                throw new KeyNotFoundException("Child not found.");

            return MapToResponse(child);
        }

        public async Task UpdateChildAsync(int id, ChildUpdate request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var userId = GetCurrentUserId();
            //if (string.IsNullOrWhiteSpace(userId))
            //    throw new UnauthorizedAccessException("Authenticated user required.");

            var child = await _dbContext.Children
                .Where(c => c.ChildId == id && c.UserId == userId && !c.IsDeleted)
                .FirstOrDefaultAsync();

            if (child == null)
                throw new KeyNotFoundException("Child not found.");

            // Server-side uniqueness check (ignore current child)
            var simNormalized = request.SimCardNum?.Trim();
            if (!string.IsNullOrWhiteSpace(simNormalized))
            {
                var simLower = simNormalized.ToLower();
                var exists = await _dbContext.Children
                    .AnyAsync(c => c.SimCardNum != null && c.SimCardNum.ToLower() == simLower && c.ChildId != id && !c.IsDeleted);

                if (exists)
                    throw new ValidationException("This phone number is already used.");
            }

            child.Name = request.Name;
            child.Age = request.Age;
            child.Gender = request.Gender;
            child.SimCardNum = simNormalized;
            child.Image = request.Image;
            child.Notes = request.Notes;

            _dbContext.Children.Update(child);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Child updated (Id: {ChildId}) for UserId: {UserId}", id, userId);
        }

        private static ChildResponse MapToResponse(Child child)
        {
            // Map expects a non-null Child; callers check for null before calling.
            // Removing nullable return to avoid CS8603.
            return new ChildResponse
            {
                ChildId = child.ChildId,
                Name = child.Name,
                Age = child.Age,
                Gender = child.Gender,
                SimCardNum = child.SimCardNum,
                Image = child.Image,
                Notes = child.Notes
            };
        }

        private string? GetCurrentUserId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User == null) return null;

            var idClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier) ??
                          httpContext.User.FindFirst("sub") ??
                          httpContext.User.FindFirst("id");

            return idClaim?.Value;
        }
    }
}
