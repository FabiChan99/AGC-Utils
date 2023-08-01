using DisCatSharp.Entities;

namespace AGC_Management.Services;

public class CatboxUploadService
{
    private const string CatboxApiUrl = "https://catbox.moe/user/api.php";

    public async Task<string> UploadImage(DiscordAttachment attachment)
    {
        using (var httpClient = new HttpClient())
        {
            try
            {
                // Herunterladen der Datei vom DiscordAttachment
                var response = await httpClient.GetAsync(attachment.Url);
                response.EnsureSuccessStatusCode();

                // Lesen des Inhalts der heruntergeladenen Datei
                var stream = await response.Content.ReadAsStreamAsync();

                using (var formData = new MultipartFormDataContent())
                {
                    formData.Add(new StreamContent(stream), "fileToUpload", attachment.Filename);
                    formData.Headers.Add("reqtype", "fileupload");

                    using (var uploadResponse = await httpClient.PostAsync(CatboxApiUrl, formData))
                    {
                        uploadResponse.EnsureSuccessStatusCode();
                        var responseContent = await uploadResponse.Content.ReadAsStringAsync();
                        var urlStart = responseContent.IndexOf("https");
                        var urlEnd = responseContent.LastIndexOf('"');
                        if (urlStart != -1 && urlEnd != -1)
                        {
                            var imageUrl = responseContent.Substring(urlStart, urlEnd - urlStart).Replace("\\", "");
                            return imageUrl;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Hochladen des Bildes: {ex.Message}");
            }

            return null;
        }
    }
}