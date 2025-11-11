using BlazorBootstrap;

namespace Kmila.Shared.Models;
/// <summary>
/// A utility class for creating toast messages.
/// </summary>
public class ToastMessages
{
    /// <summary>
    /// Creates a new <see cref="ToastMessage"/> with the specified title, message, and type.
    /// </summary>
    /// <param name="Title">The title of the toast message.</param>
    /// <param name="Message">The message content of the toast message.</param>
    /// <param name="Type">The type of the toast message (e.g., success, error, info).</param>
    /// <returns>A new <see cref="ToastMessage"/> instance.</returns>
    public static ToastMessage Message(string Title, string Message, ToastType Type = ToastType.Success) =>
        new()
        {
            Title = Title,
            Message = Message,
            Type = Type,
            HelpText = $"{DateTime.Now}"
        };
}