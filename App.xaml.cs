namespace Ra3dCar;

public partial class App : Application
{
    /// <summary>
    /// A global event broadcasted whenever the Bluetooth connection state changes.
    /// Pages can subscribe to this event to update their UI instantly.
    /// </summary>
    public static event Action? ConnectionChanged;

    private static bool _isConnected = false;
    
    /// <summary>
    /// Gets or sets the global Bluetooth connection state.
    /// Acts as a smart property that alerts the entire app upon state changes.
    /// </summary>
    public static bool IsConnected
    {
        get => _isConnected;
        set
        {
            _isConnected = value;
            // Invoke the event to notify all subscribed pages instantly
            ConnectionChanged?.Invoke();
        }
    }

    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}