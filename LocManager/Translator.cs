using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

public class TranslationResult
{
    public DetectedLanguage DetectedLanguage { get; set; }
    public TextResult SourceText { get; set; }
    public Translation[] Translations { get; set; }
}

public class DetectedLanguage
{
    public string Language { get; set; }
    public float Score { get; set; }
}

public class TextResult
{
    public string Text { get; set; }
    public string Script { get; set; }
}

public class Alignment
{
    public string Proj { get; set; }
}

public class SentenceLength
{
    public int[] SrcSentLen { get; set; }
    public int[] TransSentLen { get; set; }
}

public class Translation
{
    public string Text { get; set; }
    public TextResult Transliteration { get; set; }
    public string To { get; set; }
    public Alignment Alignment { get; set; }
    public SentenceLength SentLen { get; set; }
}

namespace LocManager
{
    public static class Translator
    {
        private static string Key = System.Environment.GetEnvironmentVariable("AzureKey", EnvironmentVariableTarget.User)
            ?? throw new InvalidOperationException();
        private const string route = "/translate?api-version=3.0&to=";
        private const string endpoint = "https://api.cognitive.microsofttranslator.com";
        private static readonly HttpClient client = new HttpClient
        {
            DefaultRequestHeaders = { { "Ocp-Apim-Subscription-Key", Key },
                { "Ocp-Apim-Subscription-Region", "westeurope" } }
        };
        public static async Task<string?> Translate(string text, string language)
        {
            Console.WriteLine(Key);
            object[] body = new object[] { new { Text = text } };
            var requestBody = JsonConvert.SerializeObject(body);
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                // Build the request.
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(endpoint + route+language);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", Key);
                request.Headers.Add("Ocp-Apim-Subscription-Region", "westeurope");

                // Send the request and get response.
                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                // Read response as a string.
                string result = await response.Content.ReadAsStringAsync();
                var a = JsonConvert.DeserializeObject<TranslationResult[]>(result);
                // Iterate over the deserialized results.
                return a?[0].Translations[0].Text;
            }
        }
    }
}
