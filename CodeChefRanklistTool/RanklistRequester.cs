using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace CodeChefRanklistTool
{
    public class RanklistRequester
    {
        private const string RanklistUrl = "https://www.codechef.com/rankings";
        private const string RanklistApiUrl = "https://www.codechef.com/api/rankings";
        private const string XsrfTokenHeader = "x-csrf-token";
        private const string XsrfStatementVariable = "window.csrfToken";
        private const int MaxRetryLimit = 5;

        private readonly HttpClient _httpClient = new HttpClient();

        public async Task<RanklistResponse> GetRanklistAsync(string contestId)
        {
            var tokens = await GetMainPageElements(contestId);
            var response = await TryGetPageAsync(contestId, tokens, 1);
            response.Contest.DisplayName = tokens.ContestName;

            Debug.Assert(response.AvailablePages != null, "response.AvailablePages != null");
            var pageCount = response.AvailablePages.Value;
            Console.WriteLine($"Total number of pages: {pageCount}");
            for (var page = 2; page <= pageCount; page++)
            {
                var pageResponse = await TryGetPageAsync(contestId, tokens, page);
                response.Participants.AddRange(pageResponse.Participants);
            }

            return response;
        }

        private async Task<RanklistResponse> TryGetPageAsync(string contestId, MainPageElements tokens, int page)
        {
            for (var i = 0; i < MaxRetryLimit; i++)
                try
                {
                    return await GetPageAsync(contestId, tokens, page);
                }
                catch (CaptchaEncounteredException)
                {
                    Console.WriteLine("Captcha encountered, going to sleep for 2 min");
                    Thread.Sleep(TimeSpan.FromMinutes(2));
                }

            throw new CaptchaEncounteredException("Too many captchas, exiting");
        }

        private async Task<RanklistResponse> GetPageAsync(string contestId, MainPageElements tokens, int page)
        {
            Console.WriteLine($"Requesting page {page} of contest {contestId}");

            using var message = CreateRanklistPageRequestMessage(contestId, tokens, page);
            var response = await _httpClient.SendAsync(message);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine(content);
                throw new HttpErrorException();
            }

            var json = await response.Content.ReadAsStringAsync();
            try
            {
                return JsonSerializer.Deserialize<RanklistResponse>(json);
            }
            catch (JsonException)
            {
                await File.WriteAllTextAsync("failed_json.json", json);
                if (json.TrimStart()[0] == '<')
                    // it is most likely a HTML captcha
                    throw new CaptchaEncounteredException();

                throw;
            }
        }

        private HttpRequestMessage CreateRanklistPageRequestMessage(string contestId, MainPageElements tokens,
            int page)
        {
            var url = $"{RanklistApiUrl}/{contestId}?sortBy=rank&order=asc&page={page}&itemsPerPage=50";
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Add(XsrfTokenHeader, tokens.XsrfToken);
            return requestMessage;
        }

        private async Task<MainPageElements> GetMainPageElements(string contestId)
        {
            var contestUrl = $"{RanklistUrl}/{contestId}";
            var response = await _httpClient.GetAsync(contestUrl);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Got {response.StatusCode} from {contestUrl}");
                throw new HttpErrorException();
            }

            var tokens = new MainPageElements();

            var rawHtml = await response.Content.ReadAsStringAsync();
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(rawHtml);

            var xsrfNode = htmlDoc.DocumentNode.SelectSingleNode("//body/script");
            var xsrfStatement = xsrfNode.InnerText;
            tokens.XsrfToken = ParseXsrfStatement(xsrfStatement);
            Console.WriteLine($"Got xsrf token: {tokens.XsrfToken}");

            try
            {
                var breadCrumbLinks = htmlDoc.DocumentNode.SelectNodes("//div[@class='breadcrumb']/a");
                var contestLink = breadCrumbLinks[1];
                tokens.ContestName = contestLink.InnerText;
                Console.WriteLine($"Got contest name: {tokens.ContestName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to get contest name");
                Console.WriteLine(ex);
                // if the HTML structure changes, this will fail; it's not so important, so we don't want to 
                // crash the program because of this
            }

            return tokens;
        }

        private string ParseXsrfStatement(string xsrfStatement)
        {
            Console.WriteLine($"xsrf statement: {xsrfStatement}");
            xsrfStatement = xsrfStatement.Trim();
            if (xsrfStatement.StartsWith(XsrfStatementVariable))
                xsrfStatement = xsrfStatement[XsrfStatementVariable.Length..];
            else
                throw new XsrfStatementParserException();

            xsrfStatement = xsrfStatement.TrimStart();
            if (xsrfStatement.StartsWith("="))
                xsrfStatement = xsrfStatement[1..];
            else
                throw new XsrfStatementParserException();

            xsrfStatement = xsrfStatement.TrimStart();

            if (xsrfStatement.StartsWith('\'') && xsrfStatement.EndsWith("\';") && xsrfStatement.Length > 2)
                xsrfStatement = xsrfStatement.Substring(1, xsrfStatement.Length - 3);
            else
                throw new XsrfStatementParserException();

            return xsrfStatement;
        }

        private class MainPageElements
        {
            public string ContestName { get; set; }
            public string XsrfToken { get; set; }
        }
    }
}