using Domain.Models;

namespace LogTime.Api.Contracts;

public interface IUserRepository
{
    Task<bool> ValidateCredentialsAsync(string user, string password);
    Task<bool> IsUserNotAllowedAsync(string userId);
    Task<User> GetInfo(string userId);
}
