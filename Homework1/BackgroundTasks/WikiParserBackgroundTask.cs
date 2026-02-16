using Homework1.Core.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Homework1.BackgroundTasks
{
    public class WikiParserBackgroundTask : BackgroundTask
    {
        private readonly IWikiParserService _wikiParserService;

        public WikiParserBackgroundTask(IWikiParserService wikiParserService) 
            : base(TimeSpan.FromMinutes(1), "WikiParserTask")
        {
            _wikiParserService = wikiParserService;
        }

        protected override async Task Execute(CancellationToken ct)
        {
            await _wikiParserService.ParseNewArticlesAsync(ct);
        }
    }
}
