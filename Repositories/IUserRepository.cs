using RealistikOsu.Cron.Entities;

namespace RealistikOsu.Cron.Repositories;

public interface IUserRepository
{
    public Task<List<User>> GetAllAsync();

    public Task UpdateAsync(User user);

    public Task<IEnumerable<int>> GetExpiredFrozenUserIdsAsync();

    public Task RestrictForExpiredFreezeAsync(int userId, int botId);
}
