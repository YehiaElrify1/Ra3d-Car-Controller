using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System.Collections.ObjectModel;

namespace Ra3dCar;

/// <summary>
/// The Setup page responsible for Bluetooth device discovery and connection management.
/// It handles scanning for nearby Bluetooth Classic devices (such as the HC-05 module),
/// managing Android permissions, and establishing a Serial Port Profile (SPP) connection.
/// </summary>
public partial class SetupPage : ContentPage
{
    /// <summary>
    /// The underlying Bluetooth client used for scanning and managing socket connections.
    /// </summary>
    private BluetoothClient _bluetoothClient = new BluetoothClient();

    /// <summary>
    /// An observable collection of discovered Bluetooth devices. 
    /// This is bound to the UI CollectionView to display the scan results dynamically.
    /// </summary>
    public ObservableCollection<BluetoothDeviceInfo> PeerDevices { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the SetupPage.
    /// Sets up the UI components and binds the data source for the device list.
    /// </summary>
    public SetupPage()
    {
        InitializeComponent();
        DevicesListView.ItemsSource = PeerDevices;
    }

    /// <summary>
    /// Event handler for the "Scan" button.
    /// Requests necessary Android permissions (Bluetooth and Location) and asynchronously
    /// searches for nearby discoverable Bluetooth devices.
    /// </summary>
    /// <param name="sender">The object that triggered the event.</param>
    /// <param name="e">The event arguments.</param>
    private async void OnScanClicked(object sender, EventArgs e)
    {
        try 
        {
            ScanIndicator.IsRunning = true;
            PeerDevices.Clear();

            // 1. Request Bluetooth permissions (and update the status variable if the user grants it)
            var statusBlue = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
            if (statusBlue != PermissionStatus.Granted)
                statusBlue = await Permissions.RequestAsync<Permissions.Bluetooth>();

            // 2. Request Location permissions (Mandatory on Android for Bluetooth scanning)
            var statusLoc = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (statusLoc != PermissionStatus.Granted)
                statusLoc = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            // Ensure the user has granted both required permissions
            if (statusBlue == PermissionStatus.Granted && statusLoc == PermissionStatus.Granted)
            {
                // Execute the discovery process (typically takes 10-12 seconds)
                var devices = await Task.Run(() => _bluetoothClient.DiscoverDevices());
                
                foreach (var device in devices)
                {
                    PeerDevices.Add(device);
                }

                if (PeerDevices.Count == 0)
                    await DisplayAlertAsync("Notice", "No devices found. Make sure HC-05 is on.", "OK");
            }
            else
            {
                await DisplayAlertAsync("Permission Denied", "Bluetooth and Location permissions are required to scan.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Scan failed: {ex.Message}", "OK");
        }
        finally 
        {
            ScanIndicator.IsRunning = false;
        }
    }

    /// <summary>
    /// Event handler triggered when a user selects a device from the list.
    /// Prompts for pairing confirmation and attempts to open a Serial Port connection.
    /// Updates the global application state upon success.
    /// </summary>
    /// <param name="sender">The CollectionView that triggered the event.</param>
    /// <param name="e">The selection changed event arguments containing the selected device.</param>
    private async void OnDeviceSelected(object sender, SelectionChangedEventArgs e)
    {
        // Retrieve the selected device using the modern CollectionView approach
        if (e.CurrentSelection.FirstOrDefault() is not BluetoothDeviceInfo device) return;

        // Clear the selection so the user can select the same device again if needed
        ((CollectionView)sender).SelectedItem = null;

        bool connect = await DisplayAlertAsync("Connect", $"Do you want to pair with {device.DeviceName}?", "Yes", "No");
        
        if (connect)
        {
            try
            {
                ScanIndicator.IsRunning = true;
                
                // Standard UUID for Serial Port Profile (SPP) used by HC-05/06 modules
                Guid serviceClass = BluetoothService.SerialPort;

                await Task.Run(() => _bluetoothClient.Connect(device.DeviceAddress, serviceClass));

                if (_bluetoothClient.Connected)
                {
                    // Update the global state to notify the rest of the application
                    App.IsConnected = true; 
                    await DisplayAlertAsync("Success", "Ra3d Car Connected!", "Let's Go");
                    DisconnectBtn.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Failed", $"Connection refused. Error: {ex.Message}", "OK");
            }
            finally
            {
                ScanIndicator.IsRunning = false;
            }
        }
    }

    /// <summary>
    /// Event handler for the "Terminate Connection" button.
    /// Closes the active Bluetooth socket, resets the client for future connections,
    /// and updates the global application state to reflect the disconnection.
    /// </summary>
    /// <param name="sender">The object that triggered the event.</param>
    /// <param name="e">The event arguments.</param>
    private async void OnDisconnectClicked(object sender, EventArgs e)
    {
        try
        {
            // 1. Terminate the actual physical connection with the module
            _bluetoothClient.Close();
            
            // Reinitialize the client object so it is clean and ready for a new connection
            _bluetoothClient = new BluetoothClient();

            // 2. Update the global application state
            App.IsConnected = false; 

            // 3. Hide the disconnect button from the current UI
            DisconnectBtn.IsVisible = false;

            await DisplayAlertAsync("Status", "Disconnected", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Disconnect failed: {ex.Message}", "OK");
        }
    }
}