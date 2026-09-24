namespace Kmila.Shared.Interfaces;
/// <summary>
/// Represents a generic repository interface for data access operations.
/// </summary>
/// <typeparam name="T">The type of the entity.</typeparam>
public interface IRepository<T> : IDisposable
{
    /// <summary>
    /// Gets a list of all items of type T asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of items.</returns>
    Task<List<T>> GetListAsync();
    /// <summary>
    /// Gets an item by its identifier asynchronously.
    /// </summary>
    /// <param name="id">The identifier of the item.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the item.</returns>
    Task<T> GetByAsync(object id);
    /// <summary>
    /// Updates an existing item asynchronously.
    /// </summary>
    /// <param name="item">The item to update.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the operation was successful.</returns>
    Task<bool> UpdateAsync(T item);
    /// <summary>
    /// Removes an item asynchronously.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the operation was successful.</returns>
    Task<bool> RemoveAsync(T item);
    /// <summary>
    /// Creates a new item asynchronously.
    /// </summary>
    /// <param name="item">The item to create.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the operation was successful.</returns>
    Task<bool> CreateAsync(T item);
}