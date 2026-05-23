using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System.Collections.ObjectModel;

#if ANDROID
using Android.Bluetooth;
using Android.Content;
#endif

namespace Ra3dCar;

/// <summary>
/// The Setup page responsible for Bluetooth device discovery and connection management.
/// It handles scanning for nearby Bluetooth Classic devices (such as the HC-05 module),
/// managing Android permissions, and establishing a Serial Port Profile (SPP) connection.
/// </summary>
public partial class SetupPage : ContentPage
{
    /// <summary>
    /// The underlying Bluetooth client used for managing socket connections.
    /// </summary>
    private BluetoothClient? _bluetoothClient;

#if ANDROID
    /// <summary>
    /// Android Bluetooth adapter for device discovery
    /// </summary>
    private BluetoothAdapter? _bluetoothAdapter;

    /// <summary>
    /// Broadcast receiver for Bluetooth discovery events
    /// </summary>
    private BluetoothDiscoveryReceiver? _discoveryReceiver;

    /// <summary>
    /// Flag to track if discovery is in progress
    /// </summary>
    private bool _isDiscovering = false;
 
    /// <summary>
    /// Active Bluetooth socket connection on Android
    /// </summary>
    private Android.Bluetooth.BluetoothSocket? _androidSocket;
#endif

    /// <summary>
    /// An observable collection of discovered Bluetooth devices. 
    /// This is bound to the UI CollectionView to display the scan results dynamically.
    /// </summary>
    public ObservableCollection<CustomBluetoothDeviceInfo> PeerDevices { get; set; } = new();

    /// <summary>
    /// Gets or creates the Bluetooth client lazily.
    /// </summary>
    private BluetoothClient GetBluetoothClient()
    {
        _bluetoothClient ??= new BluetoothClient();
        return _bluetoothClient;
    }

    /// <summary>
    /// Initializes a new instance of the SetupPage.
    /// Sets up the UI components and binds the data source for the device list.
    /// </summary>
    public SetupPage()
    {
        InitializeComponent();
        DevicesListView.ItemsSource = PeerDevices;

#if ANDROID
        _bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
#endif
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
            var statusBlue = PermissionStatus.Granted;
#if ANDROID
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.S)
            {
                statusBlue = await Permissions.CheckStatusAsync<BluetoothPermissions>();
                if (statusBlue != PermissionStatus.Granted)
                    statusBlue = await Permissions.RequestAsync<BluetoothPermissions>();
            }
#endif

            // 2. Request Location permissions (Mandatory on Android for Bluetooth scanning)
            var statusLoc = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (statusLoc != PermissionStatus.Granted)
                statusLoc = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            // Ensure the user has granted both required permissions
            if (statusBlue == PermissionStatus.Granted && statusLoc == PermissionStatus.Granted)
            {
#if ANDROID
                // Use Android's native Bluetooth API
                if (_bluetoothAdapter != null)
                {
                    // 1. Add already paired (bonded) devices first
                    var bondedDevices = _bluetoothAdapter.BondedDevices;
                    if (bondedDevices != null)
                    {
                        foreach (var device in bondedDevices)
                        {
                            var address = InTheHand.Net.BluetoothAddress.Parse(device.Address);
                            bool exists = false;
                            foreach (var d in PeerDevices)
                            {
                                if (d.DeviceAddress == address)
                                {
                                    exists = true;
                                    break;
                                }
                            }
                            if (!exists)
                            {
                                PeerDevices.Add(new CustomBluetoothDeviceInfo(device.Name, address));
                            }
                        }
                    }

                    // 2. Register receiver for discovery events
                    _discoveryReceiver = new BluetoothDiscoveryReceiver(this, PeerDevices, ScanIndicator);
                    var filter = new IntentFilter(BluetoothDevice.ActionFound);
                    filter.AddAction(BluetoothAdapter.ActionDiscoveryFinished);
                    
                    var context = Android.App.Application.Context;
                    context.RegisterReceiver(_discoveryReceiver, filter, ReceiverFlags.Exported);

                    _isDiscovering = true;
                    bool started = _bluetoothAdapter.StartDiscovery();
                    if (!started)
                    {
                        _isDiscovering = false;
                        await DisplayAlert("Error", "Failed to start Bluetooth discovery. Make sure Bluetooth and Location/GPS are turned ON.", "OK");
                    }

                    // Wait for discovery to complete
                    while (_isDiscovering)
                    {
                        await Task.Delay(500);
                    }

                    if (PeerDevices.Count == 0)
                        await DisplayAlert("Notice", "No devices found. Make sure HC-05 is on.", "OK");
                }
#else
                // Fallback for non-Android platforms
                var devices = await Task.Run(() => GetBluetoothClient().DiscoverDevices());
                foreach (var device in devices)
                {
                    PeerDevices.Add(new CustomBluetoothDeviceInfo(device.DeviceName, device.DeviceAddress));
                }

                if (PeerDevices.Count == 0)
                    await DisplayAlert("Notice", "No devices found. Make sure HC-05 is on.", "OK");
#endif
            }
            else
            {
                await DisplayAlert("Permission Denied", "Bluetooth and Location permissions are required to scan.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Scan failed: {ex.Message}\nType: {ex.GetType().FullName}\nStack Trace: {ex.StackTrace}", "OK");
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
        if (e.CurrentSelection.FirstOrDefault() is not CustomBluetoothDeviceInfo device) return;

        // Clear the selection so the user can select the same device again if needed
        ((CollectionView)sender).SelectedItem = null;

        bool connect = await DisplayAlert("Connect", $"Do you want to pair with {device.DeviceName}?", "Yes", "No");
        
        if (connect)
        {
            try
            {
                ScanIndicator.IsRunning = true;
                
#if ANDROID
                // Android native RFCOMM connection using standard SPP UUID
                var nativeAdapter = BluetoothAdapter.DefaultAdapter;
                if (nativeAdapter == null)
                {
                    await DisplayAlert("Error", "Bluetooth adapter not found.", "OK");
                    return;
                }

                if (nativeAdapter.IsDiscovering)
                {
                    nativeAdapter.CancelDiscovery();
                }

                var rawAddress = device.DeviceAddress.ToString();
                string formattedAddress = rawAddress;
                if (rawAddress != null && rawAddress.Length == 12)
                {
                    formattedAddress = $"{rawAddress.Substring(0, 2)}:{rawAddress.Substring(2, 2)}:{rawAddress.Substring(4, 2)}:{rawAddress.Substring(6, 2)}:{rawAddress.Substring(8, 2)}:{rawAddress.Substring(10, 2)}";
                }
                var nativeDevice = nativeAdapter.GetRemoteDevice(formattedAddress);
                if (nativeDevice == null)
                {
                    await DisplayAlert("Error", "Could not resolve native Bluetooth device.", "OK");
                    return;
                }
                var uuid = Java.Util.UUID.FromString("00001101-0000-1000-8000-00805f9b34fb");
                var socket = nativeDevice.CreateRfcommSocketToServiceRecord(uuid);
                if (socket == null)
                {
                    await DisplayAlert("Error", "Could not create RFCOMM socket.", "OK");
                    return;
                }

                await Task.Run(() => socket.Connect());

                if (socket.IsConnected)
                {
                    App.IsConnected = true;
                    _androidSocket = socket;
                    App.BthStream = new BluetoothSocketStream(socket);
                    await DisplayAlert("Success", "Ra3d Car Connected!", "Let's Go");
                    DisconnectBtn.IsVisible = true;
                }
#else
                // Standard UUID for Serial Port Profile (SPP) used by HC-05/06 modules
                Guid serviceClass = BluetoothService.SerialPort;

                await Task.Run(() => GetBluetoothClient().Connect(device.DeviceAddress, serviceClass));

                if (GetBluetoothClient().Connected)
                {
                    // Update the global state to notify the rest of the application
                    App.IsConnected = true; 
                    // استخراج قناة نقل البيانات
                    App.BthStream = GetBluetoothClient().GetStream();
                    await DisplayAlert("Success", "Ra3d Car Connected!", "Let's Go");
                    DisconnectBtn.IsVisible = true;
                }
#endif
            }
            catch (Exception ex)
            {
                await DisplayAlert("Failed", $"Connection refused. Error: {ex.Message}", "OK");
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
            if (App.BthStream != null)
            {
                App.BthStream.Close();
                App.BthStream = null;
            }

            if (_bluetoothClient != null)
            {
                _bluetoothClient.Close();
                _bluetoothClient = null;
            }
            
#if ANDROID
            if (_androidSocket != null)
            {
                _androidSocket.Close();
                _androidSocket = null;
            }
#endif

            // 2. Update the global application state
            App.IsConnected = false; 

            // 3. Hide the disconnect button from the current UI
            DisconnectBtn.IsVisible = false;

            await DisplayAlert("Status", "Disconnected", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Disconnect failed: {ex.Message}", "OK");
        }
    }

#if ANDROID
    /// <summary>
    /// Broadcast receiver to handle Bluetooth discovery events on Android.
    /// </summary>
    private class BluetoothDiscoveryReceiver : BroadcastReceiver
    {
        private readonly SetupPage _page;
        private readonly ObservableCollection<CustomBluetoothDeviceInfo> _peerDevices;
        private readonly ActivityIndicator _scanIndicator;
 
        public BluetoothDiscoveryReceiver(SetupPage page, ObservableCollection<CustomBluetoothDeviceInfo> peerDevices, ActivityIndicator scanIndicator)
        {
            _page = page;
            _peerDevices = peerDevices;
            _scanIndicator = scanIndicator;
        }

        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent == null) return;

            string? action = intent.Action;

            if (action == BluetoothDevice.ActionFound)
            {
                var device = (BluetoothDevice?)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                if (device != null)
                {
                    var addressStr = device.Address;
                    if (addressStr != null)
                    {
                        var address = InTheHand.Net.BluetoothAddress.Parse(addressStr);
                        
                        // Check if we already have this device in our collection
                        bool exists = false;
                        foreach (var d in _peerDevices)
                        {
                            if (d.DeviceAddress == address)
                            {
                                exists = true;
                                break;
                            }
                        }

                        if (!exists)
                        {
                            var info = new CustomBluetoothDeviceInfo(device.Name, address);
                            _peerDevices.Add(info);
                        }
                    }
                }
            }
            else if (action == BluetoothAdapter.ActionDiscoveryFinished)
            {
                _page._isDiscovering = false;
                _scanIndicator.IsRunning = false;
                try
                {
                    context?.UnregisterReceiver(this);
                }
                catch {}
            }
        }
    }
#endif
}

#if ANDROID
/// <summary>
/// Custom Bluetooth permissions request for Android 12 (API 31) and higher.
/// </summary>
public class BluetoothPermissions : Microsoft.Maui.ApplicationModel.Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions
    {
        get
        {
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.S)
            {
                return new (string, bool)[]
                {
                    ("android.permission.BLUETOOTH_SCAN", true),
                    ("android.permission.BLUETOOTH_CONNECT", true)
                };
            }
            
            return Array.Empty<(string, bool)>();
        }
    }
}
#endif
 
/// <summary>
/// Custom bindable class for Bluetooth devices to avoid PlatformNotSupportedException on Android.
/// </summary>
public class CustomBluetoothDeviceInfo
{
    public string DeviceName { get; set; }
    public InTheHand.Net.BluetoothAddress DeviceAddress { get; set; }
 
    public CustomBluetoothDeviceInfo(string? name, InTheHand.Net.BluetoothAddress address)
    {
        DeviceName = string.IsNullOrWhiteSpace(name) ? address.ToString() : name;
        DeviceAddress = address;
    }
}

#if ANDROID
/// <summary>
/// Custom stream class that wraps Android's BluetoothSocket to allow reading/writing on a single stream.
/// </summary>
public class BluetoothSocketStream : System.IO.Stream
{
    private readonly System.IO.Stream _input;
    private readonly System.IO.Stream _output;
    private readonly Android.Bluetooth.BluetoothSocket _socket;

    public BluetoothSocketStream(Android.Bluetooth.BluetoothSocket socket)
    {
        _socket = socket;
        _input = socket.InputStream!;
        _output = socket.OutputStream!;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override void Flush() => _output.Flush();

    public override int Read(byte[] buffer, int offset, int count) => _input.Read(buffer, offset, count);
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
        => _input.ReadAsync(buffer, offset, count, cancellationToken);

    public override void Write(byte[] buffer, int offset, int count) => _output.Write(buffer, offset, count);
    public override Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
        => _output.WriteAsync(buffer, offset, count, cancellationToken);

    public override long Seek(long offset, System.IO.SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Close()
    {
        _input.Close();
        _output.Close();
        _socket.Close();
        base.Close();
    }
}
#endif