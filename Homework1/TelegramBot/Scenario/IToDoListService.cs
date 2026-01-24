using Homework1.Core.DataAccess;
using Homework1.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1.TelegramBot.Scenario
{
    public interface IToDoListService
    {
        Task<ToDoList> Add(ToDoUser user, string name, CancellationToken ct);
        Task<ToDoList?> Get(Guid id, CancellationToken ct);
        Task Delete(Guid id, CancellationToken ct);
        Task<IReadOnlyList<ToDoList>> GetUserLists(Guid userId, CancellationToken ct);
    }

    // ./Core/Services/ToDoListService.cs
    public class ToDoListService : IToDoListService
    {
        private readonly IToDoListRepository _repository;
        private const int MaxListNameLength = 10;

        public ToDoListService(IToDoListRepository repository)
        {
            _repository = repository;
        }

        public async Task<ToDoList> Add(ToDoUser user, string name, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Название списка не может быть пустым");
            }

            if (name.Length > MaxListNameLength)
            {
                throw new InvalidOperationException($"Название списка не может быть длиннее {MaxListNameLength} символов");
            }

            var exists = await _repository.ExistsByName(user.Id, name, ct);
            if (exists)
            {
                throw new InvalidOperationException($"Список с именем '{name}' уже существует");
            }

            var list = new ToDoList(user, name);
            await _repository.Add(list, ct);
            return list;
        }

        public Task<ToDoList?> Get(Guid id, CancellationToken ct)
        {
            return _repository.Get(id, ct);
        }

        public Task Delete(Guid id, CancellationToken ct)
        {
            return _repository.Delete(id, ct);
        }

        public Task<IReadOnlyList<ToDoList>> GetUserLists(Guid userId, CancellationToken ct)
        {
            return _repository.GetByUserId(userId, ct);
        }
    }
}
