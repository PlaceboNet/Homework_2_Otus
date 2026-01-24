using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homework1.TelegramBot.Scenario
{
    public class ScenarioContext
    {
        public ScenarioType CurrentScenario { get; set; }
        public string? CurrentStep { get; set; }
        public Dictionary<string, object> Data { get; } = new();

        public ScenarioContext(ScenarioType scenario)
        {
            CurrentScenario = scenario;
        }

        public T GetData<T>(string key)
        {
            if (Data.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default!;
        }

        public void SetData(string key, object value)
        {
            Data[key] = value;
        }
    }
}
