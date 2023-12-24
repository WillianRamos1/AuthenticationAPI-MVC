using Lovelace.UserManagerAPI.Models;
using System.Security.Claims;

namespace Lovelace.UserManagerAPI.Services.IServices
{
    public interface ILogService
    {
        Task SaveNewLog(string userName, string description);
        Task<IEnumerable<LogResponse>> GetLogsAsync();
    }
}
