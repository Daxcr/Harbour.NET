using System.Net.Http.Headers;

public partial class Utils {
    public static async Task<bool> IsArchive(string url)
    {
        try
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new RangeHeaderValue(0, 261);
            HttpResponseMessage response = await http.SendAsync(request);

            byte[] bytes = await response.Content.ReadAsByteArrayAsync();

            if (bytes.Length >= 2 && bytes[0] == 0x50 && bytes[1] == 0x4B)
                return true;

            if (bytes.Length >= 2 && bytes[0] == 0x1F && bytes[1] == 0x8B)
                return true;

            if (bytes.Length >= 262 && bytes[257] == 0x75 && bytes[258] == 0x73 && bytes[259] == 0x74 && bytes[260] == 0x61 && bytes[261] == 0x72)
                return true;

            return false;
        }
        catch
        {
            return false;
        }
    }
}