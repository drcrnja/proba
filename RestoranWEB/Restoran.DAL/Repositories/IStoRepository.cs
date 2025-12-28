using Restoran.DAL.Entities;

namespace Restoran.DAL.Repositories
{
    public interface IStoRepository
    {
        //prikazuje sve stolove
        Task<List<Sto>> GetAllAsync();
        //sto po idu
        Task<Sto?> GetByIdAsync(int id);
        //dali uopste postoji sto sa tim idom
        Task<bool> ExistsAsync(int id);
    }
}
