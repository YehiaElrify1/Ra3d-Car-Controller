namespace Ra3dCar;

public partial class App : Application
{
    // ==========================================
    // 1. أحداث وحالة الاتصال (Connection State)
    // ==========================================
    public static event Action? ConnectionChanged;
    
    private static bool _isConnected = false;
    public static bool IsConnected
    {
        get => _isConnected;
        set
        {
            _isConnected = value;
            ConnectionChanged?.Invoke(); // تنبيه كل الشاشات بتغير حالة الاتصال
        }
    }

    // ==========================================
    // 2. قناة نقل البيانات (Bluetooth Stream)
    // ==========================================
    public static System.IO.Stream? BthStream { get; set; }

    // ==========================================
    // 3. أحداث وحالة الحرارة (Temperature State)
    // ==========================================
    public static event Action<double>? TemperatureChanged;
    public static double CurrentTemperature { get; private set; }
    
    public static void UpdateTemperature(double temp)
    {
        CurrentTemperature = temp;
        TemperatureChanged?.Invoke(temp); // تنبيه شاشة الـ Analytics بتغير الحرارة
    }

    // ==========================================
    // 4. دالة إرسال الأوامر للعربية (Command Sender)
    // ==========================================
    public static async Task SendCommand(string command)
    {
        if (IsConnected && BthStream != null && BthStream.CanWrite)
        {
            try
            {
                byte[] buffer = System.Text.Encoding.ASCII.GetBytes(command);
                await BthStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch
            {
                // لو حصل مشكلة في السلك أو البلوتوث قفل، افصل التطبيق بأمان
                IsConnected = false; 
            }
        }
    }

    // ==========================================
    // 5. دوال التطبيق الأساسية
    // ==========================================
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}