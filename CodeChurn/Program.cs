using System;
using System.IO;
using System.Threading;

namespace CodeChurn
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO: Specify these for your environment.
            const string tfsUrl = ""; // ex: http://myserver:8080/tfs/MyCollection
            const string tfsUser = "";
            const string tfsPassword = "";
            const string tfsDomain = "";
            const string codePath = ""; // ex: $/MyProject/
            const string resultsPath = "codechurn.csv";

            var tfsAnalyzer = new TfsAnalyser(tfsUrl, tfsUser, tfsPassword, tfsDomain);
            var churnStatistics = tfsAnalyzer.GetChurnStatistics(codePath, new CancellationToken());
            using (var streamWriter = new StreamWriter(resultsPath, append:false))
            {
                foreach (var stats in churnStatistics)
                {
                    streamWriter.WriteLine("{0},{1}", stats.Count, stats.Key);
                }
            }
        }
    }
}
