namespace Ra3dCar;

public partial class AnalyticsPage : ContentPage
{
    public AnalyticsPage()
    {
        InitializeComponent();

        // الاشتراك في حدث تغير الحرارة العالمي المنطلق من البلوتوث
        App.TemperatureChanged += OnTemperatureUpdated;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // تحديث الشاشة فوراً بآخر قيمة مسجلة ومحفوظة عند فتح التاب
        UpdateThermalUI(App.CurrentTemperature);
    }

    private void OnTemperatureUpdated(double currentTemp)
    {
        // التأكد التام من تحديث عناصر واجهة المستخدم على الـ Main Thread لمنع الكراش
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateThermalUI(currentTemp);
        });
    }

    private void UpdateThermalUI(double temp)
    {
        lblBigTemp.Text = $"{temp} °C";

        // تغيير الألوان والتحذيرات ديناميكياً حسب درجة الحرارة
        if (temp > 55)
        {
            // حالة الخطر الحراري العالي (أحمر)
            lblBigTemp.TextColor = Microsoft.Maui.Graphics.Color.Parse("#FF004D");
            lblStatus.Text = "OVERHEATING WARNING!";
            lblStatus.TextColor = Microsoft.Maui.Graphics.Color.Parse("#FF004D");
            lblSafetyNotice.Text = "Critical temperature! Please stop the car to cool down.";
            lblSafetyNotice.TextColor = Microsoft.Maui.Graphics.Color.Parse("#FF004D");
        }
        else if (temp > 40)
        {
            // حالة تحذيرية متوسطة (أصفر)
            lblBigTemp.TextColor = Microsoft.Maui.Graphics.Color.Parse("#FFD700");
            lblStatus.Text = "WARM";
            lblStatus.TextColor = Microsoft.Maui.Graphics.Color.Parse("#FFD700");
            lblSafetyNotice.Text = "Load is slightly high. Keep monitoring.";
            lblSafetyNotice.TextColor = Microsoft.Maui.Graphics.Color.Parse("#FFD700");
        }
        else
        {
            // الحالة المستقرة والطبيعية (أبيض وأخضر)
            lblBigTemp.TextColor = Microsoft.Maui.Graphics.Colors.White;
            lblStatus.Text = "SYSTEM OK";
            lblStatus.TextColor = Microsoft.Maui.Graphics.Color.Parse("#00FF00");
            lblSafetyNotice.Text = "Monitoring motors and battery health...";
            lblSafetyNotice.TextColor = Microsoft.Maui.Graphics.Color.Parse("#61788C");
        }
    }
}