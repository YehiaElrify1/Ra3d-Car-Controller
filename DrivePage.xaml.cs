using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;

namespace Ra3dCar;

public partial class DrivePage : ContentPage
{
    private bool _isListening = false;

    public DrivePage()
    {
        InitializeComponent();
        // الاشتراك في حدث حالة الاتصال العام
        App.ConnectionChanged += OnConnectionStateChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateConnectionStatus();
    }

    private void OnConnectionStateChanged()
    {
        MainThread.BeginInvokeOnMainThread(() => 
        {
            UpdateConnectionStatus();
        });
    }

    private void UpdateConnectionStatus()
    {
        if (App.IsConnected)
        {
            ConnectionIndicator.Fill = Microsoft.Maui.Graphics.Color.Parse("#00FF00"); 
            ConnectionLabel.Text = "Connected";
            ConnectionLabel.TextColor = Microsoft.Maui.Graphics.Color.Parse("#00FF00");
            
            // بدء الاستماع لبيانات الحساسات القادمة من السيريال
            StartListeningToCarData();
        }
        else
        {
            ConnectionIndicator.Fill = Microsoft.Maui.Graphics.Color.Parse("#FF004D");
            ConnectionLabel.Text = "Disconnected";
            ConnectionLabel.TextColor = Microsoft.Maui.Graphics.Color.Parse("#61788C");
            
            RadarLabel.Text = "Radar: Standby";
            RadarCircle.Fill = Microsoft.Maui.Graphics.Color.Parse("#0D1625");
            RadarLabel.TextColor = Microsoft.Maui.Graphics.Color.Parse("#00D2FF");
        }
    }

    // ==========================================
    // أزرار التحكم الكلاسيكية (إرسال الحروف المباشرة)
    // ==========================================
    private async void OnForwardPressed(object sender, EventArgs e) => await App.SendCommand("F");
    private async void OnBackPressed(object sender, EventArgs e) => await App.SendCommand("B");
    private async void OnLeftPressed(object sender, EventArgs e) => await App.SendCommand("L");
    private async void OnRightPressed(object sender, EventArgs e) => await App.SendCommand("R");
    
    // عند رفع الإصبع عن أي اتجاه، يتم إرسال أمر التوقف فوراُ لأمان المركبة
    private async void OnButtonReleased(object sender, EventArgs e) => await App.SendCommand("S");

    // ==========================================
    // استقبال وفك تشفير داتا الحساسات المدمجة (D:xx,T:yy#)
    // ==========================================
    private async void StartListeningToCarData()
    {
        if (App.BthStream == null || !App.BthStream.CanRead || _isListening) return;

        _isListening = true;
        byte[] buffer = new byte[1024];
        string incomingData = "";

        try
        {
            while (_isListening && App.IsConnected && App.BthStream != null)
            {
                int bytesRead = await App.BthStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string chunk = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    incomingData += chunk;

                    int hashIndex;
                    while ((hashIndex = incomingData.IndexOf('#')) >= 0)
                    {
                        string message = incomingData.Substring(0, hashIndex);
                        incomingData = incomingData.Substring(hashIndex + 1); 

                        // تحليل السلسلة النصية المستقبلة
                        var dataParts = message.Split(',');
                        foreach (var part in dataParts)
                        {
                            if (part.StartsWith("D:"))
                            {
                                if (int.TryParse(part.Substring(2), out int dist))
                                {
                                    MainThread.BeginInvokeOnMainThread(() => UpdateRadarUI(dist));
                                }
                            }
                            else if (part.StartsWith("T:"))
                            {
                                if (double.TryParse(part.Substring(2), out double temp))
                                {
                                    // إرسال درجة الحرارة لايف لحدث صفحة الـ Analytics
                                    App.UpdateTemperature(temp);
                                }
                            }
                        }
                    }
                }
            }
        }
        catch 
        { 
            App.IsConnected = false; 
        }
        finally 
        { 
            _isListening = false; 
        }
    }

    private void UpdateRadarUI(int distance)
    {
        // تم دمج التحديثات البصرية داخل الواجهة الأساسية بأمان
        if (distance >= 999)
        {
            RadarLabel.Text = "Radar: Clear Path";
            RadarCircle.Fill = Microsoft.Maui.Graphics.Color.Parse("#052605");
            RadarLabel.TextColor = Microsoft.Maui.Graphics.Color.Parse("#00FF00");
            return;
        }

        RadarLabel.Text = $"Radar: {distance} cm";

        if (distance > 30)
        {
            RadarCircle.Fill = Microsoft.Maui.Graphics.Color.Parse("#052605"); // أخضر (أمان)
            RadarLabel.TextColor = Microsoft.Maui.Graphics.Color.Parse("#00FF00");
        }
        else if (distance > 15)
        {
            RadarCircle.Fill = Microsoft.Maui.Graphics.Color.Parse("#262205"); // أصفر (انتباه)
            RadarLabel.TextColor = Microsoft.Maui.Graphics.Color.Parse("#FFD700");
        }
        else
        {
            RadarCircle.Fill = Microsoft.Maui.Graphics.Color.Parse("#26050F"); // أحمر (خطر اصطدام)
            RadarLabel.Text = "OBSTACLE DETECTED!";
            RadarLabel.TextColor = Microsoft.Maui.Graphics.Color.Parse("#FF004D");
        }
    }
}