public partial class Utils {
    private static HttpClient http = new HttpClient()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    public static async Task<bool> IsGitRepo(string url)
    {
        try
        {
            string checkUrl = url.TrimEnd('/') + "/info/refs?service=git-upload-pack";
            HttpResponseMessage response = await http.GetAsync(checkUrl);
            
            if (!response.IsSuccessStatusCode) return false;

            string? contentType = response.Content.Headers.ContentType?.MediaType;
            return contentType == "application/x-git-upload-pack-advertisement";
        }
        catch
        {
            return false;
        }
    }
}