using Newtonsoft.Json;
using Physio.IoT.PulsePersist.Configuration;
using System;
using System.IO;
using System.Net.Http.Headers;
using System.Text;

namespace Physio.IoT.PulsePersistPhysio.IoT.PulsePersist.Configuration
{
    public static class ConfigurationLoader
    {
        public static async Task<AppConfig> LoadAsync(Uri uri)
        {
            string json;
            if (uri.IsFile)
            {
                json = await File.ReadAllTextAsync(uri.LocalPath);
            }
            else
            {
                using (var httpClient = new HttpClient())
                {
                    var req = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = uri,
                        Headers = {
                        Accept = { new MediaTypeWithQualityHeaderValue("application/json") },
                        AcceptCharset = { new StringWithQualityHeaderValue(Encoding.UTF8.WebName) }
                    }
                    };

                    var resp = await httpClient.SendAsync(req);
                    resp.EnsureSuccessStatusCode();
                    json = await resp.Content.ReadAsStringAsync();
                }
            }
            return JsonConvert.DeserializeObject<AppConfig>(json) ?? throw new Exception("no valid configuration.");
        }
    }
}
