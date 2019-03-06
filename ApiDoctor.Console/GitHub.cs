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
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class GitHub
    {

        public const int maxCommentLength = 65536;

        public static string accessToken;

        public static string repositoryUrl;

        private static async Task PostToApiAsync(string path, object body)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://api.github.com");
                client.DefaultRequestHeaders.Accept.Clear();
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", accessToken);
                client.DefaultRequestHeaders.Add("User-Agent", "api-doctor");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                try
                {
                    StringContent bodyString = new StringContent
                    (
                        JsonConvert.SerializeObject(body)
                    );

                    HttpResponseMessage response = await client.PostAsync(path, bodyString);
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"GitHub API response: {response.ReasonPhrase}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occured: {ex.Message}");
                }
				
            }

        }

        public static async Task PostPullRequestCommentAsync(int pullRequestNumber, string comment)
        {
            try
            {
                var body = new
                {
                    body = comment
                };

                var repositoryOwner = new Uri(repositoryUrl).Segments[1].Replace("/", "");
                var repositoryName = new Uri(repositoryUrl).Segments[2].Replace("/", "");

                var path = $"/repos/{repositoryOwner}/{repositoryName}/issues/{pullRequestNumber}/comments";
                await PostToApiAsync(path, body);
            }
            catch
            {
                //ignored
            }
        }
    }
}