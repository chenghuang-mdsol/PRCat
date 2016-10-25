using System.Collections.Generic;
using Octokit;

namespace PRCat.Core
{
    public class PrReport
    {
        public MonitoredRepo Repository { get; set; }
        public List<Issue> NotOverdue { get; set; } = new List<Issue>();
        public List<Issue> OverdueButNotImportant { get; set; } = new List<Issue>();
        public List<Issue> OverdueAndImportant { get; set; } = new List<Issue>();
        public Dictionary<int, PullRequest> PullRequestRef { get; set; } = new Dictionary<int, PullRequest>();
    }
}