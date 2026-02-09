using Homework1.Core.DataAccess;
using Homework1.Core.Entities;
using LinqToDB;
using LinqToDB.Async;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1.Infrastructure.DataAccess.Repositories
{
    public class SqlToDoRepository : IToDoRepository
    {
        private readonly IDataContextFactory<ToDoDataContext> _factory;

        public SqlToDoRepository(IDataContextFactory<ToDoDataContext> factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            using var dbContext = _factory.CreateDataContext();

            var items = await dbContext.ToDoItems
                .LoadWith(i => i.User)
                .LoadWith(i => i.List)
                .LoadWith(i => i.List!.User)
                .Where(i => i.UserId == userId)
                .ToListAsync(cancellationToken);

            return items.Select(ModelMapper.MapFromModel).ToList().AsReadOnly();
        }

        public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            using var dbContext = _factory.CreateDataContext();

            var items = await dbContext.ToDoItems
                .LoadWith(i => i.User)
                .LoadWith(i => i.List)
                .LoadWith(i => i.List!.User)
                .Where(i => i.UserId == userId && i.State == (int)ToDoItemState.Active)
                .ToListAsync(cancellationToken);

            return items.Select(ModelMapper.MapFromModel).ToList().AsReadOnly();
        }

        public async Task<ToDoItem?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var dbContext = _factory.CreateDataContext();

            var item = await dbContext.ToDoItems
                .LoadWith(i => i.User)
                .LoadWith(i => i.List)
                .LoadWith(i => i.List!.User)
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

            return item != null ? ModelMapper.MapFromModel(item) : null;
        }

        public async Task AddAsync(ToDoItem item, CancellationToken cancellationToken = default)
        {
            using var dbContext = _factory.CreateDataContext();

            var model = ModelMapper.MapToModel(item);
            await dbContext.InsertAsync(model, token: cancellationToken);
        }

        public async Task UpdateAsync(ToDoItem item, CancellationToken cancellationToken = default)
        {
            using var dbContext = _factory.CreateDataContext();

            var model = ModelMapper.MapToModel(item);
            await dbContext.UpdateAsync(model, token: cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var dbContext = _factory.CreateDataContext();

            await dbContext.ToDoItems
                .Where(i => i.Id == id)
                .DeleteAsync(cancellationToken);
        }

        public async Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken cancellationToken = default)
        {
            using var dbContext = _factory.CreateDataContext();

            return await dbContext.ToDoItems
                .AnyAsync(i => i.UserId == userId && i.Name.ToLower() == name.ToLower(), cancellationToken);
        }

        public async Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            using var dbContext = _factory.CreateDataContext();

            return await dbContext.ToDoItems
                .CountAsync(i => i.UserId == userId && i.State == (int)ToDoItemState.Active, cancellationToken);
        }

        public async Task<IReadOnlyList<ToDoItem>> FindAsync(Guid userId, Func<ToDoItem, bool> predicate, CancellationToken cancellationToken = default)
        {
            using var dbContext = _factory.CreateDataContext();

            var items = await dbContext.ToDoItems
                .LoadWith(i => i.User)
                .LoadWith(i => i.List)
                .LoadWith(i => i.List!.User)
                .Where(i => i.UserId == userId)
                .ToListAsync(cancellationToken);

            var mappedItems = items.Select(ModelMapper.MapFromModel);
            return mappedItems.Where(predicate).ToList().AsReadOnly();
        }

        public async Task<IReadOnlyList<ToDoItem>> GetByUserIdAndListAsync(Guid userId, Guid? listId, CancellationToken cancellationToken = default)
        {
            using var dbContext = _factory.CreateDataContext();

            var query = dbContext.ToDoItems
                .LoadWith(i => i.User)
                .LoadWith(i => i.List)
                .LoadWith(i => i.List!.User)
                .Where(i => i.UserId == userId);

            if (listId.HasValue)
            {
                query = query.Where(i => i.ListId == listId.Value);
            }
            else
            {
                query = query.Where(i => i.ListId == null);
            }

            var items = await query.ToListAsync(cancellationToken);
            return items.Select(ModelMapper.MapFromModel).ToList().AsReadOnly();
        }
        
        public async Task<IReadOnlyList<ToDoItem>> GetActiveWithDeadline(Guid userId, DateTime from, DateTime to, CancellationToken ct)
        {
            using var dbContext = _factory.CreateDataContext();

            var items = await dbContext.ToDoItems
                .LoadWith(i => i.User)
                .LoadWith(i => i.List)
                .Where(i => i.UserId == userId && 
                          i.State == (int)ToDoItemState.Active && 
                          i.Deadline >= from && 
                          i.Deadline < to)
                .ToListAsync(ct);

            return items.Select(ModelMapper.MapFromModel).ToList().AsReadOnly();
        }
    }
}
