namespace Kmila.Shared.Interfaces;

/// <summary>
/// Defines a contract for reading and deserializing JSON files asynchronously.
/// </summary>
/// <remarks>
/// This interface provides a generic method for reading JSON files and deserializing
/// them into strongly-typed objects. Implementations should handle platform-specific
/// file access (e.g., file system for desktop, embedded resources for web).
///
/// Note: This interface is currently not implemented by JSONFileReader, which uses
/// a more specific ReadFPGAJsonFileAsync method instead. Consider implementing this
/// interface in JSONFileReader for better abstraction and testability.
/// </remarks>
public interface IJsonFileReader
{
    /// <summary>
    /// Reads a JSON file from the specified path and deserializes it to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the JSON content into.</typeparam>
    /// <param name="filePath">The path to the JSON file to read.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the deserialized object of type T, or null if the file cannot be read or deserialized.
    /// </returns>
    /// <exception cref="System.IO.FileNotFoundException">
    /// Thrown when the specified file does not exist.
    /// </exception>
    /// <exception cref="System.Text.Json.JsonException">
    /// Thrown when the JSON content cannot be deserialized to the specified type.
    /// </exception>
    Task<T?> ReadJsonAsync<T>(string filePath);
}
