using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRCat.Core
{
    public class GitHubSettings
    {
        public List<string> TeamMembers { get; set; } =
            ConfigurationManager.AppSettings["GitHubMembers"].Split(',').Select(p => p.Trim().ToLower()).Distinct().ToList();
        public List<string> LessImportantTags { get; set; } =
            ConfigurationManager.AppSettings["LessImportantTags"].Split(',').Select(p => p.Trim().ToLower()).ToList();

        public string LessImportantTagsCombined => $"[{string.Join("][", LessImportantTags)}]";


        public long OverdueTicks { get; set; } = long.Parse(ConfigurationManager.AppSettings["OverdueTicks"]);

        public string GitHubToken { get; set; }

        public List<MonitoredRepo> GitHubRepos = ConfigurationManager.AppSettings["GitHubRepos"].Split(',').Select(p => MonitoredRepo.Parse(p.Trim())).ToList();
    }
}
