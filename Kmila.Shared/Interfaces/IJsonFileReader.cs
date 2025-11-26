namespace Kmila.Shared.Interfaces
{
    public interface IJsonFileReader
    {
        Task<T?> ReadJsonAsync<T>(string filePath);
    }
}
