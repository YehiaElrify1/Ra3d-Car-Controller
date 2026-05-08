namespace Ra3dCar;

/// <summary>
/// The primary page for controlling the Ra3d car.
/// It listens for real-time Bluetooth connection updates and adjusts the UI accordingly.
/// </summary>
public partial class DrivePage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the DrivePage class.
    /// Subscribes to the global connection event to listen for state changes.
    /// </summary>
    public DrivePage()
    {
        InitializeComponent();

        // Subscribe to the real-time global connection event
        App.ConnectionChanged += OnConnectionStateChanged;
    }

    /// <summary>
    /// Triggered automatically when the page becomes visible on the screen.
    /// Ensures the UI reflects the most current connection state immediately upon loading.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateConnectionStatus();
    }

    /// <summary>
    /// Event handler that triggers immediately when the connection state changes (connected or disconnected).
    /// </summary>
    private void OnConnectionStateChanged()
    {
        // Since the event might be fired from a background thread (e.g., during Bluetooth scanning),
        // we must marshal the UI update to the Main (UI) Thread to prevent application crashes.
        MainThread.BeginInvokeOnMainThread(() => 
        {
            UpdateConnectionStatus();
        });
    }

    /// <summary>
    /// Updates the visual indicators (colors and text) on the page based on the current Bluetooth state.
    /// </summary>
    private void UpdateConnectionStatus()
    {
        if (App.IsConnected)
        {
            // State: Connected (Neon Green visual feedback)
            ConnectionIndicator.Fill = Microsoft.Maui.Graphics.Color.Parse("#00FF00"); 
            ConnectionLabel.Text = "Connected";
            ConnectionLabel.TextColor = Microsoft.Maui.Graphics.Color.Parse("#00FF00");
        }
        else
        {
            // State: Disconnected (Crimson Red / Gray visual feedback)
            ConnectionIndicator.Fill = Microsoft.Maui.Graphics.Color.Parse("#FF004D");
            ConnectionLabel.Text = "Disconnected";
            ConnectionLabel.TextColor = Microsoft.Maui.Graphics.Color.Parse("#61788C");
        }
    }
}