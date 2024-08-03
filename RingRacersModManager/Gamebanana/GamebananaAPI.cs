using System.Text.Json;
using System.Threading.Tasks;

namespace RingRacersModManager.Gamebanana;
public static class GamebananaAPI {

    public static async Task<GamebananaMod> GetMods(uint gameID, uint page, uint pageAmount) {
        using var httpResponse = await Program.HttpClient.GetAsync($"https://gamebanana.com/apiv11/Mod/Index?_nPerpage={pageAmount}&_aFilters%5BGeneric_Game%5D={gameID}&_nPage={page}");
        byte[] fileBytes = await httpResponse.Content.ReadAsByteArrayAsync();
        return JsonSerializer.Deserialize(fileBytes, ModManagerJsonContext.Default.GamebananaMod);
    }
}
