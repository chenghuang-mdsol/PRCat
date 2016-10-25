using System.Collections.Generic;
using System.Linq;
using Octokit;

namespace PRCat.Core.Helpers
{
    public static class GitHubExtensions
    {
        public static string ToTagString(this IReadOnlyList<Label> labels)
        {
            return $"[{string.Join("][", labels.Select(l => l.Name))}]".Replace("[]", "");
        }

        public static string ToColorTagString(this IReadOnlyList<Label> labels)
        {
            var lableString =
                labels.Select(l =>
                {
                    var hasColor = !string.IsNullOrEmpty(l.Color);
                    var colorStringStart = hasColor ? $"<font color=\"{l.Color}\">" : string.Empty;
                    var colorStringEnd = hasColor ? "</font>" : string.Empty;
                    return $"[{colorStringStart + l.Name + colorStringEnd}]";
                });
            return string.Join("", lableString);
        }
    }
}