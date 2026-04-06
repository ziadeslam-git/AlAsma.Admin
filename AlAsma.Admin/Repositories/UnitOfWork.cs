using System;
using System.Threading.Tasks;
using AlAsma.Admin.Data;
using AlAsma.Admin.Interfaces;
using AlAsma.Admin.Models;

namespace AlAsma.Admin.Repositories
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly AppDbContext _context;
        private IRepository<Author>? _authors;
        private IRepository<Sale>? _sales;
        private IRepository<Operation>? _operations;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public IRepository<Author> Authors => _authors ??= new GenericRepository<Author>(_context);

        public IRepository<Sale> Sales => _sales ??= new GenericRepository<Sale>(_context);

        public IRepository<Operation> Operations => _operations ??= new GenericRepository<Operation>(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
