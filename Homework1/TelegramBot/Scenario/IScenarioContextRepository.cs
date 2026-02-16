using Homework1.TelegramBot.Scenario;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1.TelegramBot.Scenario
{
    public interface IScenarioContextRepository
    {
        Task<ScenarioContext?> GetContext(long userId, CancellationToken ct);
        Task SetContext(long userId, ScenarioContext context, CancellationToken ct);
        Task ResetContext(long userId, CancellationToken ct);
        Task<IReadOnlyList<ScenarioContext>> GetContexts(CancellationToken ct);
    }
}

namespace Homework1.TelegramBot.Scenario
{
    public class InMemoryScenarioContextRepository : IScenarioContextRepository
    {
        private readonly ConcurrentDictionary<long, ScenarioContext> _contexts = new();

        public Task<ScenarioContext?> GetContext(long userId, CancellationToken ct)
        {
            _contexts.TryGetValue(userId, out var context);
            return Task.FromResult(context);
        }

        public Task SetContext(long userId, ScenarioContext context, CancellationToken ct)
        {
            _contexts[userId] = context;
            return Task.CompletedTask;
        }

        public Task ResetContext(long userId, CancellationToken ct)
        {
            _contexts.TryRemove(userId, out _);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ScenarioContext>> GetContexts(CancellationToken ct)
        {
            var list = _contexts.Values.ToList();
            return Task.FromResult<IReadOnlyList<ScenarioContext>>(list);
        }
    }
}
