using Lovelace.UserManagerAPI.Data;
using Lovelace.UserManagerAPI.Models;
using Lovelace.UserManagerAPI.Services.IServices;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Lovelace.UserManagerAPI.Services
{
    public class LogService : ILogService
    {
        private readonly ApplicationDbContext _context;

        public LogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<LogResponse>> GetLogsAsync()
        {
            var logs = await _context.Logs.Select(x => new LogResponse
            {
                CreatedAt = x.CreatedAt,
                Description = x.Description,
                UserName = x.UserName,
            }).OrderByDescending(x => x.CreatedAt).ToListAsync();
            return logs;
        }

        public async Task SaveNewLog(string userName, string description)
        {
            var newLog = new Log()
            {
                UserName = userName,
                Description = description
            };

            await _context.Logs.AddAsync(newLog);
            await _context.SaveChangesAsync();
        }
    }
}
