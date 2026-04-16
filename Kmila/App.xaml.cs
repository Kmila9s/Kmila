namespace Kmila;

/// <summary>
/// Main application class for the Kmila MAUI app.
/// Inherits from <see cref="Application"/> and configures the app's initial window and page.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class
    /// and loads the XAML-defined resources and styles.
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Creates the main application window with the root page.
    /// </summary>
    /// <param name="activationState">The activation state provided by the platform, or <c>null</c> if not available.</param>
    /// <returns>A new <see cref="Window"/> containing the <see cref="MainPage"/> with the title "Kmila".</returns>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage()) { Title = "Kmila" };
    }
}
