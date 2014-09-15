using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace CodeChurn
{
    internal class TfsAnalyser
    {
        private readonly VersionControlServer _vcs;

        public TfsAnalyser(string url)
        {
            var collection = ConnectToTeamProjectCollection(url, null);
            _vcs = (VersionControlServer) collection.GetService(typeof (VersionControlServer));
        }

        public TfsAnalyser(string url, string user, string password, string domain)
        {
            var networkCredential = new NetworkCredential(user, password, domain);
            var collection = ConnectToTeamProjectCollection(url, networkCredential);
            _vcs = (VersionControlServer) collection.GetService(typeof (VersionControlServer));
        }

        /// <summary> 
        /// Gets User Statistics (how many changes each user has made) 
        /// </summary> 
        /// <param name="path">Path in the form "$/My Project/"</param> 
        /// <param name="cancellationToken">Cancellation token</param> 
        public IEnumerable<SourceControlStatistic> GetUserStatistics(string path, CancellationToken cancellationToken)
        {
            return GetChangesetsForProject(path, cancellationToken).GroupBy(c => c.Committer).Select(g =>
                new SourceControlStatistic {Key = g.Key, Count = g.Count()}).OrderByDescending(s => s.Count);
        }

        /// <summary> 
        /// Gets Churn Statistics (how many times has each file been modified) 
        /// </summary> 
        /// <param name="path">Path in the form "$/My Project/"</param> 
        /// <param name="cancellationToken">Cancellation token</param> 
        public IEnumerable<SourceControlStatistic> GetChurnStatistics(string path, CancellationToken cancellationToken)
        {
            return GetChangesetsForProject(path, cancellationToken)
                .Select(GetChangesetWithChanges)
                .SelectMany(c => c.Changes) // select the actual changed files 
                //.Where(c => c.Item.ServerItem.Contains("/Source/")) // filter out just the files we are interested in 
                .Where(c => c.Item.ServerItem.EndsWith(".cs"))
                .Where(c => ((int) c.ChangeType & (int) ChangeType.Edit) == (int) ChangeType.Edit)
                // don't count merges 
                //.Select(c => Regex.Replace(c.Item.ServerItem, @"^.+/Source/", ""))
                .Select(c => c.Item.ServerItem)
                // count changes to the same file on different branches 
                .GroupBy(c => c)
                .Select(g =>
                    new SourceControlStatistic {Key = g.Key, Count = g.Count()}).OrderByDescending(s => s.Count);
        }

        private Changeset GetChangesetWithChanges(Changeset c)
        {
            return _vcs.GetChangeset(c.ChangesetId, includeChanges: true, includeDownloadInfo: false);
        }

        private IEnumerable<Changeset> GetChangesetsForProject(string path, CancellationToken cancellationToken)
        {
            const Int32 noDeletion = 0;
            const String anyUser = null;
            // Change versions here. ex: VersionSpec.ParseSingleSpec("12345", user: null)
            const VersionSpec fromFirstChangeset = null; 
            const VersionSpec toLatestChangeset = null;
            const Int32 allChanges = Int32.MaxValue;
            return _vcs.QueryHistory(
                path,
                VersionSpec.Latest,
                noDeletion,
                RecursionType.Full,
                anyUser,
                fromFirstChangeset,
                toLatestChangeset,
                allChanges,
                includeChanges: true,
                slotMode: false)
                .Cast<Changeset>()
                .TakeWhile(changeset => !cancellationToken.IsCancellationRequested);
        }

        private static TfsTeamProjectCollection ConnectToTeamProjectCollection(string url, ICredentials networkCredential)
        {
            var teamProjectCollection = new TfsTeamProjectCollection(new Uri(url), networkCredential);
            teamProjectCollection.EnsureAuthenticated();
            return teamProjectCollection;
        }
    }
}
