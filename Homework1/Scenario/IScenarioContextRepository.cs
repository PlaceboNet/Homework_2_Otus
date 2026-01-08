using Homework1.Scenario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1.Scenario
{
    public interface IScenarioContextRepository
    {
        Task<ScenarioContext?> GetContext(long userId, CancellationToken ct);
        Task SetContext(long userId, ScenarioContext context, CancellationToken ct);
        Task ResetContext(long userId, CancellationToken ct);
    }
}

// ./TelegramBot/Scenarios/InMemoryScenarioContextRepository.cs
namespace TelegramBot.Scenarios
{
    public class InMemoryScenarioContextRepository : IScenarioContextRepository
    {
        private readonly Dictionary<long, ScenarioContext> _contexts = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public async Task<ScenarioContext?> GetContext(long userId, CancellationToken ct)
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                return _contexts.TryGetValue(userId, out var context) ? context : null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task SetContext(long userId, ScenarioContext context, CancellationToken ct)
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                _contexts[userId] = context;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task ResetContext(long userId, CancellationToken ct)
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                _contexts.Remove(userId);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
