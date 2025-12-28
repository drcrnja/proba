using Microsoft.EntityFrameworkCore;
using Restoran.DAL.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace Restoran.DAL.Repositories
{
    public class NarudzbinaRepository : INarudzbinaRepository
    {
        private readonly RestoranContext _ctx;
        public NarudzbinaRepository(RestoranContext ctx) => _ctx = ctx;

        public Task<List<Narudzbina>> GetAllAsync() =>
            _ctx.Narudzbine.ToListAsync();

        public Task<Narudzbina?> GetByIdAsync(int id) =>
            _ctx.Narudzbine.FirstOrDefaultAsync(n => n.Id == id);

        public Task AddAsync(Narudzbina n) =>
            _ctx.Narudzbine.AddAsync(n).AsTask();

        public Task<List<Narudzbina>> GetByUserAsync(string userName) =>
            _ctx.Narudzbine.Where(n => n.CreatedBy == userName).ToListAsync();

        // DODAJ OVO:
        public Task<Narudzbina?> FirstOrDefaultAsync(Expression<Func<Narudzbina, bool>> predicate) =>
            _ctx.Narudzbine.FirstOrDefaultAsync(predicate);
    }
}