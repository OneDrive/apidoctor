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

namespace ApiDoctor.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using ApiDoctor.Validation.Error;
    using ApiDoctor.Validation.Params;
    using ApiDoctor.Validation.Http;
    using ApiDoctor.Validation.Json;

    public static class MethodValidationExtensionMethods
    {
        /// <summary>
        /// Performs validation of a method (request/response) with a given service
        /// target and zero or more test scenarios
        /// </summary>
        /// <param name="method"></param>
        /// <param name="account"></param>
        /// <param name="credentials"></param>
        /// <returns></returns>
        public static async Task<ValidationResults> ValidateServiceResponseAsync(
            this MethodDefinition method,
            ScenarioDefinition[] scenarios,
            IServiceAccount primaryAccount,
            IServiceAccount secondaryAccount,
            ValidationOptions options = null)
        {
            if (null == method)
                throw new ArgumentNullException("method");
            if (null == primaryAccount)
                throw new ArgumentNullException("primaryAccount");
            
            ValidationResults results = new ValidationResults();

            if (scenarios.Length == 0)
            {
                // If no descenarios are defined for this method, add a new default scenario
                scenarios = new ScenarioDefinition[] {
                    new ScenarioDefinition {
                        Description = "verbatim",
                        Enabled = true,
                        MethodName = method.Identifier,
                        RequiredScopes = method.RequiredScopes,
                        RequiredApiVersions = method.RequiredApiVersions,
                        RequiredTags = method.RequiredTags
                    }
                };
//                results.AddResult("init", new ValidationMessage(null, "No scenarios were defined for method {0}. Will request verbatim from docs.", method.Identifier), ValidationOutcome.None);
            }

            if (scenarios.Any() && !scenarios.Any(x => x.Enabled))
            {
                results.AddResult("init", 
                    new ValidationWarning(ValidationErrorCode.AllScenariosDisabled, null, method.SourceFile.DisplayName, "All scenarios for method {0} were disabled.", method.Identifier),
                    ValidationOutcome.Skipped);
                
                return results;
            }

            foreach (var scenario in scenarios.Where(x => x.Enabled))
            {
                try
                {
                    await ValidateMethodWithScenarioAsync(method, scenario, primaryAccount, secondaryAccount, results, options);
                }
                catch (Exception ex)
                {
                    results.AddResult(
                        "validation",
                        new ValidationError(
                            ValidationErrorCode.ExceptionWhileValidatingMethod,
                            method.SourceFile.DisplayName,
                            method.SourceFile.DisplayName,
                            ex.Message));
                }
            }

            return results;
        }


        private static async Task ValidateMethodWithScenarioAsync(
            MethodDefinition method,
            ScenarioDefinition scenario,
            IServiceAccount primaryAccount,
            IServiceAccount secondaryAccount,
            ValidationResults results,
            ValidationOptions options = null)
        {
            if (null == method)
                throw new ArgumentNullException("method");
            if (null == scenario)
                throw new ArgumentNullException("scenario");
            if (null == primaryAccount)
                throw new ArgumentNullException("primaryAccount");
            if (null == results)
                throw new ArgumentNullException("results");

            var actionName = scenario.Description;

            // Check to see if the account + scenario scopes are aligned

            string[] requiredScopes = method.RequiredScopes.Union(scenario.RequiredScopes).ToArray();
 
            if (!primaryAccount.Scopes.ProvidesScopes(requiredScopes, options.IgnoreRequiredScopes))
            {
                var missingScopes = from scope in requiredScopes where !primaryAccount.Scopes.Contains(scope) select scope;

                results.AddResult(actionName,
                    new ValidationWarning(ValidationErrorCode.RequiredScopesMissing,
                    null,
                    method.SourceFile.DisplayName,
                    "Scenario was not run. Scopes required were not available: {0}", missingScopes.ComponentsJoinedByString(",")));
                return;
            }

            string[] requiredApiVersions = method.RequiredApiVersions.Union(scenario.RequiredApiVersions).ToArray();

            if (!primaryAccount.ApiVersions.ProvidesApiVersions(requiredApiVersions))
            {
                var missingApiVersions = from apiVersion in requiredApiVersions where !primaryAccount.ApiVersions.Contains(apiVersion) select apiVersion;

                results.AddResult(actionName,
                    new ValidationWarning(ValidationErrorCode.RequiredApiVersionsMissing,
                    null,
                    method.SourceFile.DisplayName,
                    "Scenario was not run. Api Versions required were not available: {0}", missingApiVersions.ComponentsJoinedByString(",")));
                return;
            }

            string[] requiredTags = method.RequiredTags.Union(scenario.RequiredTags).ToArray();

            if (!primaryAccount.Tags.ProvidesTags(requiredTags))
            {
                var missingTags = from tag in requiredTags where !primaryAccount.Tags.Contains(tag) select tag;

                results.AddResult(actionName,
                    new ValidationWarning(ValidationErrorCode.RequiredTagsMissing,
                    null,
                    method.SourceFile.DisplayName,
                    "Scenario was not run. Tags required were not available: {0}", missingTags.ComponentsJoinedByString(",")));
                return;
            }

            // Generate the tested request by "previewing" the request and executing
            // all test-setup procedures
            long startTicks = DateTimeOffset.UtcNow.Ticks;
            var requestPreviewResult = await method.GenerateMethodRequestAsync(scenario, method.SourceFile.Parent, primaryAccount, secondaryAccount);
            TimeSpan generateMethodDuration = new TimeSpan(DateTimeOffset.UtcNow.Ticks - startTicks);
            
            // Check to see if an error occured building the request, and abort if so.
            var generatorResults = results[actionName /*+ " [test-setup requests]"*/];
            generatorResults.AddResults(requestPreviewResult.Messages, requestPreviewResult.IsWarningOrError ? ValidationOutcome.Error :  ValidationOutcome.Passed);
            generatorResults.Duration = generateMethodDuration;

            if (requestPreviewResult.IsWarningOrError)
            {
                return;
            }

            // We've done all the test-setup work, now we have the real request to make to the service
            HttpRequest requestPreview = requestPreviewResult.Value;

            results.AddResult(
                actionName,
                new ValidationMessage(null, method.SourceFile.DisplayName, "Generated Method HTTP Request:\r\n{0}", requestPreview.FullHttpText()));

            HttpParser parser = new HttpParser();
            HttpResponse expectedResponse = null;
            if (!string.IsNullOrEmpty(method.ExpectedResponse))
            {
                expectedResponse = parser.ParseHttpResponse(method.ExpectedResponse);
            }

            // Execute the actual tested method (the result of the method preview call, which made the test-setup requests)
            startTicks = DateTimeOffset.UtcNow.Ticks;
            var issues = new IssueLogger();
            var actualResponse = await requestPreview.GetResponseAsync(primaryAccount, issues);
            TimeSpan actualMethodDuration = new TimeSpan(DateTimeOffset.UtcNow.Ticks - startTicks);

            var requestResults = results[actionName];
            if (actualResponse.RetryCount > 0)
            {
                requestResults.AddResults(
                    new ValidationError[]
                    { new ValidationWarning(ValidationErrorCode.RequestWasRetried, null, method.SourceFile.DisplayName, "HTTP request was retried {0} times.", actualResponse.RetryCount) });
            }

            requestResults.AddResults(
                new ValidationError[]
                { new ValidationMessage(null, "HTTP Response:\r\n{0}", actualResponse.FullText(false)) });
            requestResults.Duration = actualMethodDuration;

            // Perform validation on the method's actual response
            method.ValidateResponse(actualResponse, expectedResponse, scenario, issues, options);

            requestResults.AddResults(issues.Issues.ToArray());

            // TODO: If the method is defined as a long running operation, we need to go poll the status 
            // URL to make sure that the operation finished and the response type is valid.

            if (issues.Issues.WereErrors())
                results.SetOutcome(actionName, ValidationOutcome.Error);
            else if (issues.Issues.WereWarnings())
                results.SetOutcome(actionName, ValidationOutcome.Warning);
            else
                results.SetOutcome(actionName, ValidationOutcome.Passed);
        }

    }


    public class ValidationResults
    {
        private List<ActionResults> results = new List<ActionResults>();

        public void AddResult(string actionName, ValidationError error, ValidationOutcome? outcome = null )
        {
            this[actionName].AddResults(new ValidationError[] { error }, outcome);
        }

        public void AddResults(string actionName, ValidationError[] errors, ValidationOutcome? outcome = null)
        {
            this[actionName].AddResults(errors, outcome);
        }

        public void SetOutcome(string actionName, ValidationOutcome outcome)
        {
            ActionFromName(actionName).Outcome = outcome;
        }

        public ActionResults[] Results
        {
            get { return results.ToArray(); }
        }

        public ActionResults this[string name] 
        {
            get { return ActionFromName(name); }
        }

        public ValidationOutcome OverallOutcome 
        {
            get
            {
                if (results.Any(x => x.Outcome == ValidationOutcome.Error))
                    return ValidationOutcome.Error;
                if (results.Any(x => x.Outcome == ValidationOutcome.Warning))
                    return ValidationOutcome.Warning;
                if (results.All(x => x.Outcome == ValidationOutcome.Skipped))
                    return ValidationOutcome.Skipped;
                if (results.All(x => x.Outcome == ValidationOutcome.Passed))
                    return ValidationOutcome.Passed;

                ValidationOutcome working = 0;
                foreach (var result in results)
                {
                    working |= result.Outcome;
                }
                return working;
            }
        }

        internal ActionResults ActionFromName(string name)
        {
            var existingAction = (from r in results where r.Name == name select r).FirstOrDefault();
            if (null == existingAction)
            {
                existingAction = new ActionResults { Name = name };
                results.Add(existingAction);
            }
            return existingAction;
        }

        public class ActionResults
        {
            public string Name { get; set; }

            public List<ValidationError> Errors { get; private set; }

            public ValidationOutcome Outcome { get; set; }

            public TimeSpan Duration { get; set; }


            public ActionResults()
            {
                Errors = new List<ValidationError>();
            }

            public void AddResults(ValidationError[] errors, ValidationOutcome? outcome = null)
            {
                if (errors.Length == 0)
                    return;

                this.Errors.AddRange(errors);
                if (outcome.HasValue)
                    this.Outcome = outcome.Value;
            }
        }
    }

    [Flags()]
    public enum ValidationOutcome
    {
        None = 0,
        Skipped = 1 << 0,
        Passed = 1 << 1,
        Warning = 1 << 2,
        Error = 1 << 3
    }




}
