/*
 * Markdown Scanner
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

namespace ApiDocs.ConsoleApp
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


        private static string RunCommand(string executable, string arguments, string workingDirectory = null)
        {
            ProcessStartInfo parameters = new ProcessStartInfo();
            parameters.CreateNoWindow = true;
            parameters.UseShellExecute = false;
            parameters.RedirectStandardError = true;
            parameters.RedirectStandardOutput = true;
            parameters.FileName = executable;
            parameters.Arguments = arguments;
            if (null != workingDirectory)
            parameters.WorkingDirectory = workingDirectory;


            var p = Process.Start(parameters);

            StringBuilder sb = new StringBuilder();
            string currentLine = null;
            while ((currentLine = p.StandardOutput.ReadLine()) != null)
            {
                sb.AppendLine(currentLine);
            }

            return sb.ToString();

        }

        private string RunGitCommand(string arguments)
        {
            return RunCommand(this.GitExecutablePath, arguments, this.RepoDirectoryPath);
        }
    }
}
