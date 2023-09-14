namespace Experimental.Channels.Services;

public sealed class UserService : IUserService
{
    private readonly Faker<User> _userFaker;

    public UserService()
    {
        _userFaker = new Faker<User>()
            .RuleFor(u => u.Id, f => f.Random.Guid())
            .RuleFor(u => u.FirstName, f => f.Person.FirstName)
            .RuleFor(u => u.LastName, f => f.Person.LastName)
            .RuleFor(u => u.Age, f => f.Random.Int(18, 80))
            .RuleFor(u => u.AvatarUrl, f => f.Person.Avatar);
    }

    public Task<IEnumerable<User>> GetUsers(int skip, int take)
    {
        if (skip >= 100)
        {
            return Task.FromResult(Enumerable.Empty<User>());
        }

        var users = _userFaker.Generate(take);

        return Task.FromResult<IEnumerable<User>>(users);
    }
}