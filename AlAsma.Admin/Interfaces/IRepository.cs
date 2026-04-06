using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AlAsma.Admin.Models;

namespace AlAsma.Admin.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(int id);
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
        Task<bool> ExistsAsync(int id);

        /// <summary>
        /// This is the approved IQueryable entry point for server-side queries.
        /// Consumers should use Query() for SQL-side filtering, aggregates, and pagination.
        /// No extra GetQueryable() method is needed.
        /// </summary>
        IQueryable<T> Query(bool asNoTracking = true);

        /// <summary>
        /// Counts entities server-side, optionally filtered by predicate.
        /// </summary>
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

        /// <summary>
        /// Checks existence server-side using predicate.
        /// </summary>
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    }
}
