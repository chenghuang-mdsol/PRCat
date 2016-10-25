using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Newtonsoft.Json;
using Octokit;
using OfficeOpenXml;
using PRCat.Core;
using PRCat.Core.Helpers;
using SlackWebHooks;

namespace PRCat.Controllers
{
    public class GitHubApiController : ApiController
    {
        private GitHubSettings _settings = new GitHubSettings();
        [HttpPost]
        public void SendPRReport(string gitHubToken)
        {
            GitHubCore core = new GitHubCore(gitHubToken);
            var reports = core.GetPRPeports();
            var users = core.GetUsers();
            var client = new WebHookClient(ConfigurationManager.AppSettings["SendToSlackUrl"]);
            reports.ForEach(r =>
            {
                var message = BuildSlackMessage(r,  users);
                client.SendMessage(message);
            });
            using (var ms = ConvertToExcel(reports))
            {
                var fileName = $"PR_report_{DateTimeOffset.UtcNow.ToEasternTime():yyyy-dd-M--HH-mm-ss}.xlsx";
                AzureBlob.AzureBlob uploader = new AzureBlob.AzureBlob();
                string link = uploader.UploadBlobFromStream(ms, fileName);
                client.SendMessage(BuildReportLinkMessage(link));
            }

        }

        private Message BuildReportLinkMessage(string link)
        {
            var text = $"<{link}| Download Report Here>";
            Message message = new Message(text, null, ":cat:");
            return message;
        }
        private MessageWithAttachments BuildSlackMessage(PrReport report, Dictionary<string, User> users)
        {
            var text = $"*{report.Repository.CombinedName}*";
            MessageWithAttachments message = new MessageWithAttachments(BuildAttachments(report,users), text, null, ":cat:");
            return message;
        }

        private NewAttachment[] BuildAttachments(PrReport report, Dictionary<string, User> users)
        {
            var overdueHours = (_settings.OverdueTicks/3600);
            List<NewAttachment> result = new List<NewAttachment>();
            var tags = $"[{string.Join("][", _settings.LessImportantTags)}]";
            
            var fields0 = BuildFields(report, report.NotOverdue,users);
            var fields1 = BuildFields(report, report.OverdueAndImportant,users);
            var fields2 = BuildFields(report, report.OverdueButNotImportant,users);
            
            var a0 = new NewAttachment("This channel is not able to show attachment.", fields0, null,
                null, "good",$"Updated PRs (within {overdueHours} hours)", null, new[] { "fields" });
            var a1 = new NewAttachment("This channel is not able to show attachment.", fields1, null,
                null, "danger", $"Overdue (Not updated in {overdueHours} hours)", null, new[] { "fields" });
            var a2 = new NewAttachment("This channel is not able to show attachment.", fields2, null,
                null, "#DCDCDC", $"Overdue but marked as {tags}", null, new []{ "fields" } );
            if (report.NotOverdue.Count > 0)
                result.Add(a0);
            if (report.OverdueAndImportant.Count > 0)
                result.Add(a1);
            if (report.OverdueButNotImportant.Count > 0)
                result.Add(a2);
            return result.ToArray();
        }

        private Field[] BuildFields(PrReport report, List<Issue> issues, Dictionary<string, User> users)
        {
            var prRef = report.PullRequestRef;
            return
                issues.Select(
                        i =>
                        {
                            var tags = (i.Labels != null && i.Labels.Any()) ? $"\r\tTags: {i.Labels.ToTagString()}" : string.Empty;
                            var milestone = (i.Milestone != null) ? $"\r\tMilestone: {i.Milestone.Title}" : string.Empty;
                            return new Field("",
                                $"<{i.HtmlUrl}|{i.Title}>" +
                                $"\r\tAuthor: <{users[i.User.Login].HtmlUrl}|{users[i.User.Login].Name}>" +
                                $"\r\tCreated: `{prRef[i.Number].CreatedAt.ToEasternTime():MM/dd/yyyy hh:mm:ss K}`" +
                                $"\r\tUpdated:`{prRef[i.Number].UpdatedAt.ToEasternTime():MM/dd/yyyy hh:mm:ss K}`" +
                                $"\r\tOpen for: {GetDaysOrHourOpenString(prRef[i.Number].CreatedAt)}" +
                                milestone +
                                tags
                            );
                        }
                    )
                    .ToArray();
        }

        private string GetDaysOrHourOpenString(DateTimeOffset created)
        {
            var now = DateTimeOffset.UtcNow;
            return now - created > TimeSpan.FromDays(1) ? $"{(int)(now - created).TotalDays} Day(s)" : $"{(int)(now - created).TotalHours} Hour(s)";
        }

        private void FillOneExcelRow(ExcelWorksheet ws, int row, int startColumn, object[] values)
        {
            for (int i = startColumn; i < values.Count() + startColumn; i++)
            {
                ws.Cells[row, i].Value = values[i];
            }
        }
        private void FillIssueToExcelRows(ExcelWorksheet ws, int row,  List<Issue> reports,string categoryString,  Dictionary<int, PullRequest> prRef)
        {
            foreach (var item in reports)
            {
                row++;
                var updated = item.UpdatedAt.HasValue ? (object)item.UpdatedAt.Value.ToEasternTime() : null;
                var tags = (item.Labels != null && item.Labels.Any()) ? item.Labels.ToTagString() : string.Empty;
                var milestone = (item.Milestone != null) ? item.Milestone.Title : string.Empty;
                FillOneExcelRow(ws, row, 1, new object[]
                {
                    item.Number, $"{prRef[item.Number].User.Login}", $"{item.CreatedAt.ToEasternTime()}", $"{updated}", $"{item.Title}", categoryString,  $"{item.HtmlUrl}", $"{milestone}", $"{tags}"
                });
            }
        }

        private void SetAutoFit(ExcelWorksheet ws, int start, int end)
        {
            for (int i = start; i <= end; i++)
            {
                ws.Column(i).AutoFit();
            }
        }
        private Stream ConvertToExcel(List<PrReport> reports)
        {
            ExcelPackage pkg = new ExcelPackage();
            
            foreach(var report in reports)
            {
                var prRef = report.PullRequestRef;
                var ws = pkg.Workbook.Worksheets.Add(report.Repository.CombinedName);
                FillOneExcelRow(ws, 1, 1,
                    new object[]
                    {
                        "Issue Number", "Author", "Created At", "Updated At", "Title", "Category", "Url", "Milestone","Tags"
                    });

                var row = 1;
                FillIssueToExcelRows(ws, row, report.NotOverdue, $"Updated in {(int)_settings.OverdueTicks / 3600} hours", prRef);
                row += report.NotOverdue.Count();
                FillIssueToExcelRows(ws, row, report.OverdueAndImportant, $"NOT updated in {(int)_settings.OverdueTicks / 3600} hours", prRef);
                row += report.OverdueButNotImportant.Count();
                FillIssueToExcelRows(ws, row, report.OverdueButNotImportant, $"NOT updated in {(int)_settings.OverdueTicks / 3600} hours, but marked as {_settings.LessImportantTagsCombined}", prRef);
                SetAutoFit(ws, 1, 9);
            }

            var content = pkg.GetAsByteArray();
            return new MemoryStream(content);

        }


    }

    public class NewAttachment : Attachment
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "footer")]
        public string Footer { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "footer_icon")]
        public string FooterIcon { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "mrkdwn_in")]
        public string[] MarkDownIn { get; set; }
        public NewAttachment(string fallback, IReadOnlyList<Field> fields, string text = null, string pretext = null, string color = null, string footer =null, string footericon = null, string[] markdownin = null) : base(fallback, fields, text, pretext, color)
        {
            Footer = footer;
            FooterIcon = footericon;
            MarkDownIn = markdownin;
        }
    }
}
