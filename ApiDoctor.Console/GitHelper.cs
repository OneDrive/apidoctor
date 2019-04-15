/*
 * API Doctor
 * Copyright (c) Microsoft Corporation
 * All rights reserved. 
 * 
 * MIT License
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of 
 * this software and associated documentation files (the ""Software""), to deal in 
 * the Software without restriction, including without limitation the rights to use, 
 * copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
 * Software, and to permit persons to whom the Software is furnished to do so, 
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all 
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace ApiDoctor.ConsoleApp
{

    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    /// <summary>
    /// Provides a wrapper around processes involving the GIT app
    /// </summary>
    class GitHelper
    {

        private string GitExecutablePath { get; set; }
        private string RepoDirectoryPath { get; set; }
        public GitHelper(string pathToGitExecutable, string repoDirectoryPath)
        {
            if (!File.Exists(pathToGitExecutable))
                throw new ArgumentException("pathToGit did not specify a path to the GIT executable.");
            this.GitExecutablePath = pathToGitExecutable;

            if (!Directory.Exists(repoDirectoryPath))
                throw new ArgumentException("repoDirectoryPath does not exist.");
            this.RepoDirectoryPath = repoDirectoryPath;
        }

        /// <summary>
        /// Uses GIT to return a list of files modified in a PR request
        /// </summary>
        /// <param name="originalBranch"></param>
        /// <returns></returns>
        public string[] FilesChangedFromBranch(string originalBranch)
        {
            var baseFetchHeadIdentifier = RunGitCommand($"merge-base HEAD {originalBranch}").TrimEnd();
            var changedFiles = RunGitCommand($"diff --name-only HEAD {baseFetchHeadIdentifier}");

            var repoChanges = changedFiles.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var prefix = PrefixForWorkingPath();
            if (string.IsNullOrEmpty(prefix))
                return repoChanges;

            // Remove the prefix for the local working directory, if one exists.
            return (from f in repoChanges
                    where f.StartsWith(prefix)
                    select f.Substring(prefix.Length)).ToArray();
        }

        public string PrefixForWorkingPath()
        {
            return RunGitCommand("rev-parse --show-prefix").TrimEnd();
        }

        public static string FindGitLocation()
        {
            if (System.Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var sys32Folder = Environment.GetFolderPath(Environment.SpecialFolder.System);
                var wherePath = Path.Combine(sys32Folder, "where.exe");
                var gitPath = RunCommand(wherePath, "git.exe");
                if (!string.IsNullOrEmpty(gitPath))
                    return gitPath.TrimEnd();
            }

            return null;
        }

        public void CheckoutBranch(string branchName)
        {
            RunGitCommand($"branch { branchName }", false);
            RunGitCommand($"checkout { branchName }", false);
        }

        public void CommitChanges(string commitMessage)
        {
            RunGitCommand($"commit -m \"{ commitMessage }\"", false);
        }

        public void PushToOrigin(string accesstoken , string repoUrl)
        {
            var pushUrl = repoUrl.Replace("github.com", accesstoken+"@github.com");
            RunGitCommand($"push { pushUrl } --force", false);
        }

        public string GetCurrentBranchName()
        {
            return RunGitCommand("rev-parse --abbrev-ref HEAD").Replace(Environment.NewLine, "");
        }

        public string StageAllChanges()
        {
            return RunGitCommand("add -A ", false);
        }

        public void ResetChanges()
        {
            RunGitCommand("reset HEAD --hard", false);
        }

        public string GetRepositoryUrl()
        {
            return RunGitCommand("config --get remote.origin.url").Replace(Environment.NewLine, "");
        }

        public void CleanupChanges()
        {
            RunGitCommand("clean -fd", false);
        }

        public Boolean ChangesPresent()
        {
            var output = RunGitCommand("status --porcelain");
            return (!string.IsNullOrEmpty(output));
        }

        private static string RunCommand(string executable, string arguments, string workingDirectory = null, bool expectResponse = true)
        {
            ProcessStartInfo parameters = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = expectResponse,
                RedirectStandardOutput = expectResponse,
                FileName = executable,
                Arguments = arguments
            };

            if (null != workingDirectory)
                parameters.WorkingDirectory = workingDirectory;

            var p = Process.Start(parameters);

            StringBuilder sb = new StringBuilder();

            if (expectResponse)
            {
                string currentLine = null;
                while ((currentLine = p.StandardOutput.ReadLine()) != null)
                {
                    sb.AppendLine(currentLine);
                }
            }

            p.WaitForExit();

            return sb.ToString();

        }

        private string RunGitCommand(string arguments , bool expectResponse = true)
        {
            return RunCommand(this.GitExecutablePath, arguments, this.RepoDirectoryPath ,expectResponse);
        }
    }
}
