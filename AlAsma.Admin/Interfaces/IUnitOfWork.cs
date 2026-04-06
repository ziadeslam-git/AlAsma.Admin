using System.Threading.Tasks;
using AlAsma.Admin.Models;

namespace AlAsma.Admin.Interfaces
{
    public interface IUnitOfWork
    {
        IRepository<Author> Authors { get; }
        IRepository<Sale> Sales { get; }
        IRepository<Operation> Operations { get; }
        Task<int> SaveChangesAsync();
    }
}
