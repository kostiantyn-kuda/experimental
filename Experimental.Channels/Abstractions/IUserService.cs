using Experimental.Channels.Models;

namespace Experimental.Channels.Abstractions;

public interface IUserService
{
    Task<IEnumerable<User>> GetUsers(int skip, int take);
}