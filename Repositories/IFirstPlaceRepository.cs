using RealistikOsu.Cron.Entities;

namespace RealistikOsu.Cron.Repositories;

public interface IFirstPlaceRepository
{
    public Task<List<FirstPlace>> GetAllByUserAsync(int userId);

    public Task CreateAsync(FirstPlace firstPlace);

    public Task DeleteAsync(FirstPlace firstPlace);
}