namespace Kmila;

/// <summary>
/// Main content page that hosts the BlazorWebView component.
/// This page serves as the bridge between the MAUI native shell and the Blazor hybrid UI.
/// </summary>
public partial class MainPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainPage"/> class
    /// and loads the XAML layout containing the BlazorWebView.
    /// </summary>
    public MainPage()
    {
        InitializeComponent();
    }
}
