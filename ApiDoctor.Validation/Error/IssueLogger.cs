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

namespace ApiDoctor.Validation.Error
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.RegularExpressions;

    public class IssueLogger
    {
        private List<IssueLogger> children = new List<IssueLogger>();
        private Dictionary<string, int> localSuppressions = new Dictionary<string, int>(SuppressionComparer.Instance);
        private HashSet<string> globalSuppressions = new HashSet<string>(SuppressionComparer.Instance);
        private List<ValidationMessage> messages = new List<ValidationMessage>();
        private List<ValidationWarning> warnings = new List<ValidationWarning>();
        private List<ValidationError> errors = new List<ValidationError>();

        private bool onlyKeepUniqueIssues { get; set; }
        private bool similarIssuesFound { get; set; }

        public int IssuesInCurrentScope { get; private set; }

        public int DebugLine { get; set; }

        /// <summary>
        /// All issues in current scope and child scopes (but not parents)
        /// </summary>
        public IEnumerable<ValidationError> Issues =>
            this.Messages.Cast<ValidationError>().
            Concat(this.Warnings).
            Concat(this.Errors);

        /// <summary>
        /// All errors in current scope and child scopes (but not parents)
        /// </summary>
        public IEnumerable<ValidationError> Errors =>
            this.errors.Where(HandleSuppression).Concat(this.children.SelectMany(c => c.Errors));

        /// <summary>
        /// All warnings in current scope and child scopes (but not parents)
        /// </summary>
        public IEnumerable<ValidationWarning> Warnings =>
            this.warnings.Where(HandleSuppression).Concat(this.children.SelectMany(c => c.Warnings));

        /// <summary>
        /// All messages in current scope and child scopes (but not parents)
        /// </summary>
        public IEnumerable<ValidationMessage> Messages =>
            this.messages.
                Concat(this.children.SelectMany(c => c.Messages));

        public string Source { get; private set; } = string.Empty;

        public List<string> UnusedSuppressions
        {
            get
            {
                return this.globalSuppressions.Except(this.UsedSuppressions, SuppressionComparer.Instance).ToList();
            }
        }

        public List<string> UsedSuppressions
        {
            get
            {
                return this.localSuppressions.
                           //Where(sup => sup.Value > 0).
                           Select(sup => sup.Key).
                       Union(this.children.
                             SelectMany(c => c.UsedSuppressions)).
                       ToList();
            }
        }

        public void Error(ValidationErrorCode code, string message, Exception exception = null, [CallerLineNumber]int lineNumber = 0)
        {
            LaunchDebuggerIfNeeded(lineNumber);
            AddIfNeeded(new ValidationError(code, this.Source, message + $"\r\n{exception}"), this.errors);
            this.IssuesInCurrentScope++;
        }

        public void Warning(ValidationErrorCode code, string message, Exception exception = null, [CallerLineNumber]int lineNumber = 0)
        {
            LaunchDebuggerIfNeeded(lineNumber);
            AddIfNeeded(new ValidationWarning(code, this.Source, message + $"\r\n{exception}"), this.warnings);
            this.IssuesInCurrentScope++;
        }

        public void Warning(ValidationWarning warning, [CallerLineNumber]int lineNumber = 0)
        {
            LaunchDebuggerIfNeeded(lineNumber);
            AddIfNeeded(warning, this.warnings);
            this.IssuesInCurrentScope++;
        }

        public void Message(string message, [CallerLineNumber]int lineNumber = 0)
        {
            LaunchDebuggerIfNeeded(lineNumber);
            this.messages.Add(new ValidationMessage(this.Source, message));
            this.IssuesInCurrentScope++;
        }

        public IssueLogger For(string source, bool onlyKeepUniqueErrors = false)
        {
            var logger = new IssueLogger
            {
                onlyKeepUniqueIssues = onlyKeepUniqueErrors,
                globalSuppressions = this.globalSuppressions,
                DebugLine = this.DebugLine,
                Source = this.Source + (string.IsNullOrEmpty(this.Source) ? "" : "/") + source,
            };

            this.children.Add(logger);
            return logger;
        }

        public void AddSuppressions(IEnumerable<string> suppressions, [CallerLineNumber]int lineNumber = 0)
        {
            LaunchDebuggerIfNeeded(lineNumber);
            foreach (var sup in suppressions)
            {
                this.globalSuppressions.Add(sup);
            }
        }

        public void LaunchDebuggerIfNeeded(int lineNumber)
        {
            if (lineNumber > 0 && lineNumber == this.DebugLine)
            {
#if DEBUG
                Debugger.Launch();
#endif
            }
        }

        private bool HandleSuppression(ValidationError error)
        {
            if (this.globalSuppressions.Contains(error.ErrorText))
            {
                int count;
                this.localSuppressions.TryGetValue(error.ErrorText, out count);
                this.localSuppressions[error.ErrorText] = count + 1;

                // do not return the warning (suppress)
                return false;
            }

            // return the warning (don't suppress)
            return true;
        }

        private void AddIfNeeded<T>(ValidationError issue, List<T> issues) where T : ValidationError
        {
            if (issue.Source == null)
            {
                issue.Source = this.Source;
            }

            if (this.onlyKeepUniqueIssues)
            {
                if (issues.Any(i => i.Code == issue.Code && i.Message == issue.Message))
                {
                    if (!this.similarIssuesFound)
                    {
                        this.warnings.Add(new ValidationWarning(ValidationErrorCode.SkippedSimilarErrors, this.Source, "Similar errors were skipped."));
                        this.similarIssuesFound = true;
                    }
                }
                else
                {
                    if (DocSet.SchemaConfig != null && DocSet.SchemaConfig.TreatErrorsAsWarningsWorkloads.Any(s => !string.IsNullOrWhiteSpace(this.Source) && this.Source.Contains(s)))
                    {
                        if (!issue.IsWarning)
                        {
                            this.warnings.Add(new ValidationWarning(issue.Code, issue.Source,
                                $"Treating Error as Warning {Environment.NewLine} {issue.Message}"));
                        }
                    }
                    else
                    {
                        issues.Add((T)issue);
                    }
                }
            }
            else
            {

                if (DocSet.SchemaConfig != null && DocSet.SchemaConfig.TreatErrorsAsWarningsWorkloads.Any(s => !string.IsNullOrWhiteSpace(this.Source) && this.Source.Contains(s)))
                {
                    if (!issue.IsWarning)
                    {
                        this.warnings.Add(new ValidationWarning(issue.Code, issue.Source,
                            $"Treating Error as Warning {Environment.NewLine} {issue.Message}"));
                    }
                }
                else
                {
                    issues.Add((T)issue);
                }
            }
        }

        private class SuppressionComparer : IEqualityComparer<string>
        {
            // matches the variable part of a stack trace: "  at blah blah blah) blah"
            private static Regex stackTracePattern = new Regex("\\n\\s*at\\s.*\\).*", RegexOptions.Compiled | RegexOptions.Singleline);

            // matches the variable part of known paths like: " /blah/blah/resources/someresource.md"
            private static Regex knownPathPattern = new Regex(":\\s(/.*)/((api)|(resources))/.*\\.md", RegexOptions.Compiled | RegexOptions.Singleline);

            public static SuppressionComparer Instance { get; } = new SuppressionComparer();

            private SuppressionComparer()
            {
            }

            public bool Equals(string x, string y)
            {
                return Sanitize(x).Equals(Sanitize(y));
            }

            public int GetHashCode(string obj)
            {
                return Sanitize(obj).GetHashCode();
            }

            private string Sanitize(string x)
            {
                return TrimWhiteSpace(TrimParentsOfKnownPaths(TrimStackTrace(x)));
            }

            private string TrimWhiteSpace(string x)
            {
                if (string.IsNullOrEmpty(x))
                {
                    return string.Empty;
                }

                var sb = new StringBuilder(x.Length);
                foreach (var c in x.Where(c => !char.IsWhiteSpace(c)))
                {
                    sb.Append(char.ToLowerInvariant(c));
                }

                return sb.ToString();
            }

            private string TrimStackTrace(string x)
            {
                var match = stackTracePattern.Match(x);
                if (match.Success)
                {
                    for (int i = 0; i < match.Captures.Count; i++)
                    {
                        x = x.Replace(match.Captures[i].Value, string.Empty);
                    }
                }

                return x;
            }

            // for well-known paths like /resources/ and /api/,
            // trim any preceding hierarchy.
            private string TrimParentsOfKnownPaths(string x)
            {
                var match = knownPathPattern.Match(x);
                if (match.Success && match.Groups.Count > 2)
                {
                    var parentPathGroup = match.Groups[1];
                    for (int i = 0; i < parentPathGroup.Captures.Count; i++)
                    {
                        x = x.Replace(parentPathGroup.Captures[i].Value, string.Empty);
                    }
                }

                return x;
            }
        }
    }
}
