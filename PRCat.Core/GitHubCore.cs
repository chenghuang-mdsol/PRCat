using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;
using PRCat.Core.Helpers;

namespace PRCat.Core
{
    public class GitHubCore
    {
        private GitHubSettings _settings = new GitHubSettings();

        public GitHubCore(string gitHubToken)
        {
            _settings.GitHubToken = gitHubToken;
        }
        public List<PrReport> GetPRPeports()
        {
            
            return _settings.GitHubRepos.Select(GetPRReportOfRepo).ToList();
        }

        public Dictionary<string, User> GetUsers()
        {
            GitHubClient github = new GitHubClient(new ProductHeaderValue("PRCat"));
            github.Credentials = new Credentials(_settings.GitHubToken);
            _settings.TeamMembers.Select(github.User.Get);
            List<Task<User>> waitlist = new List<Task<User>>();
            foreach (var m in _settings.TeamMembers)
            {
                var task = github.User.Get(m);
                waitlist.Add(task);
            }
            Task.WaitAll(waitlist.ToArray());
            return waitlist.Select(t => t.Result).ToDictionary(t => t.Login);
        }
        public PrReport GetPRReportOfRepo(MonitoredRepo repo)
        {
            PrReport result = new PrReport();
            GitHubClient github = new GitHubClient(new ProductHeaderValue("PRCat"));
            github.Credentials = new Credentials(_settings.GitHubToken);
            var task = github.PullRequest.GetAllForRepository(repo.Org, repo.Repo);
            task.Wait();
            var prResult = task.Result.Where(r => r.State == ItemState.Open).Where(r=> IsOurTeam(r, _settings)).ToList();

            List<Task<Issue>> waitlist = new List<Task<Issue>>();

            var prRef = result.PullRequestRef;
            foreach (var pr in prResult)
            {
                prRef.Add(pr.Number, pr);
                var t = github.Issue.Get(repo.Org, repo.Repo,pr.Number);
                waitlist.Add(t);
            }
            Task.WaitAll(waitlist.ToArray());
            foreach (var issueTask in waitlist)
            {
                var issue = issueTask.Result;
                var isOverdue = IsOverdue(prRef[issue.Number].UpdatedAt, _settings);
                if (!isOverdue)
                {
                    result.NotOverdue.Add(issue);
                }
                else
                {
                    if (IsPullRequestImportant(issue, _settings))
                    {
                        result.OverdueAndImportant.Add(issue);
                    }
                    else
                    {
                        result.OverdueButNotImportant.Add(issue);
                    }
                }
            }
            result.Repository = repo;
            result.OverdueAndImportant = result.OverdueAndImportant.OrderByDescending(r => r.Milestone?.DueOn != null).ToList();
            result.OverdueButNotImportant = result.OverdueButNotImportant.OrderByDescending(r => r.Milestone?.DueOn != null).ToList();
            result.NotOverdue = result.NotOverdue.OrderByDescending(r => r.Milestone?.DueOn != null).ToList();
            return result;

        }
        

        private bool IsPullRequestImportant(Issue issue, GitHubSettings settings)
        {
            return !issue.Labels.Any(l=>settings.LessImportantTags.Contains(l.Name.ToLower())) && !IsPRTitleContainsLessImportantTags(issue, settings);
        }

        private bool IsPRTitleContainsLessImportantTags(Issue issue, GitHubSettings settings)
        {
            return settings.LessImportantTags.Any(t => issue.Title.ToLower().Contains(t.ToLower()));
        }
        private bool IsOurTeam(PullRequest pr, GitHubSettings settings)
        {
            return settings.TeamMembers.Contains(pr.User.Login.ToLower());
        }

        private bool IsOverdue(DateTimeOffset dateTime, GitHubSettings settings)
        {
            var ticks = dateTime.UtcDateTime.GetEpochSeconds();
            var nowTicks = DateTime.UtcNow.GetEpochSeconds() ;
            return (nowTicks - ticks > settings.OverdueTicks);
        }


 

    }
}