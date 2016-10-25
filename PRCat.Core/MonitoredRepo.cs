namespace PRCat.Core
{
    public class MonitoredRepo
    {
        public static MonitoredRepo Parse(string str)
        {
            var parsed = str.Split('\\', '/');
            return new MonitoredRepo()
            {
                Org = parsed[0],
                Repo = parsed[1]

            };
        }

        public string CombinedName => $"{Org}/{Repo}";
        public string Org { get; set; }
        public string Repo { get; set; }
    }
}