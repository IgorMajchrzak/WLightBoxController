//Copyright 2022 Igor Majchrzak
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using WLightBox;

namespace WLightBoxController
{
    public partial class MainWindow : Window
    {
        private bool _deviceFound = false;
        private WLightBox.WLightBox _wLightBoxObject = new WLightBox.WLightBox();
        private int _selectedEffect = 0;

        public MainWindow()
        {
            InitializeComponent();

            NotificationGrid.Visibility = Visibility.Visible;
            NotificationTbl.Text = "ŁĄCZENIE Z SIECIĄ";

            Thread networkScanThread = new Thread(GetNetworkDevicesAsync);
            networkScanThread.Start();
        }

        private async void GetNetworkDevicesAsync()
        {
            IPAddress localIPAddress = getlocalIPAddress();
            if (localIPAddress == null)
            {
                return;
            }
            List<string> activeNetworkDevices = new List<string>();
            Task[] tasks = new Task[254];
            string baseIPAddress = localIPAddress.ToString();
            baseIPAddress = baseIPAddress.Substring(0, baseIPAddress.LastIndexOf('.') + 1);
            for (int i = 1; i < 255; i++)
            {
                string ip = baseIPAddress + i.ToString();
                tasks[i - 1] = pingAsync(ip, activeNetworkDevices);
            }
            Task.WaitAll(tasks);

            foreach (string device in activeNetworkDevices)
            {
                try
                {
                    await _wLightBoxObject.EstablishConnection(device);
                }
                catch (Exception ex)
                {
                    //
                };
                if (_wLightBoxObject.DeviceInfo.Type == "wLightBox")
                {
                    _deviceFound = true;
                    break;
                }
            }
            if (!_deviceFound)
            {
                Dispatcher.Invoke(() =>
                {
                    NotificationTbl.Text = "BRAK POŁĄCZENIA Z URZĄDZENIEM";
                });
                var tryAgain = MessageBox.Show("Nie znaleziono urządzenia wLightBox w sieci lokalnej. Upewnij się, że urządzenie jest połączone z właściwą siecią i kliknij \"OK\" aby spróbować ponownie.", "Błąd połączenia", MessageBoxButton.OKCancel);
                if (tryAgain == MessageBoxResult.OK)
                {
                    GetNetworkDevicesAsync();
                }
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    NotificationGrid.Visibility = Visibility.Collapsed;
                    DeviceNameTbl.Text = "Nazwa urządzenia: " + _wLightBoxObject.DeviceInfo.DeviceName;
                    DeviceTypeTbl.Text = "Urządzenie: " + _wLightBoxObject.DeviceInfo.Product;
                    HwVersionTbl.Text = "Wersja urządzenia: " + _wLightBoxObject.DeviceInfo.Hv;
                    FwVersionTbl.Text = "Wersja oprogramowania: " + _wLightBoxObject.DeviceInfo.Fv;
                    DeviceAddressTbl.Text = "Adres w sieci: " + _wLightBoxObject.DeviceInfo.Ip;
                    SsidTbl.Text = "Nazwa podłączonej sieci: " + _wLightBoxObject.NetworkInfo.Ssid;
                });
                Thread monitoringThread = new Thread(MonitorValues);
                monitoringThread.Start();
            }
        }

        private void MonitorValues()
        {
            while (_deviceFound)
            {
                try
                {
                    _wLightBoxObject.GetCurrentLightingStateAsync().Wait();
                }
                catch (Exception ex)
                {
                    MessageBoxResult result = MessageBox.Show(ex.Message + " Wciśnij OK aby kontynuować, wciśnij Anuluj aby zamknąć połączenie.", "Błąd", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.Cancel)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            NotificationGrid.Visibility = Visibility.Visible;
                            NotificationTbl.Text = "BRAK POŁĄCZENIA";
                        });
                        return;
                    }
                }
                Dispatcher.Invoke(() =>
                {
                    CurrentColorDisplayTbl.Text = "Kolor: " + _wLightBoxObject.RGBW.CurrentColor;
                    CurrentColorFadeDisplayTbl.Text = "Czas przejścia koloru: " + _wLightBoxObject.RGBW.DurationsMs.ColorFade + "ms";
                    CurrentEffectIdDisplayTbl.Text = "Efekt: " + _wLightBoxObject.RGBW.EffectId;
                    CurrentEffectFadeDisplayTbl.Text = "Czas przejścia efektu: " + _wLightBoxObject.RGBW.DurationsMs.EffectFade + "ms";
                    CurrentEffectStepDisplayTbl.Text = "Czas trwania efektu: " + _wLightBoxObject.RGBW.DurationsMs.EffectStep + "ms";
                });
                Thread.Sleep(100);
            }
        }

        private IPAddress getlocalIPAddress()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }

        private async Task pingAsync(string ip, List<string> hostsList)
        {
            await Task.Run(() =>
            {
                Ping ping = new Ping();
                var reply = ping.Send(ip, 100);
                if (reply.Status == IPStatus.Success)
                {
                        hostsList.Add(ip);
                    Dispatcher.Invoke(() =>
                    {
                        NotificationTbl.Text = $"SKANOWANIE SIECI LOKALNEJ ({ip})";
                    });
                    return;
                }
                Dispatcher.Invoke(() =>
                {
                    NotificationTbl.Text = $"SKANOWANIE SIECI LOKALNEJ ({ip})";
                });
                return;
            });

            return;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;
            if (slider == null)
            {
                return;
            }
            int value = (int)Math.Floor(slider.Value);
            if (slider == RedAmountSlider)
            {
                RedAmountDisplayTbl.Text = value.ToString();
            }
            else if (slider == GreenAmountSlider)
            {
                GreenAmountDisplayTbl.Text = value.ToString();
            }
            else if (slider == BlueAmountSlider)
            {
                BlueAmountDisplayTbl.Text = value.ToString();
            }
            else if (slider == WarmWhiteAmountSlider)
            {
                WarmWhiteAmountDisplayTbl.Text = value.ToString();
            }
            else if (slider == ColdWhiteAmountSlider)
            {
                ColdWhiteDisplayTbl.Text = value.ToString();
            }
            else if (slider == ColorFadeSlider)
            {
                ColorFadeDisplayTbl.Text = value.ToString();
            }
            else if (slider == EffectFadeSlider)
            {
                EffectFadeDisplayTbl.Text = value.ToString();
            }
            else if (slider == EffectStepSlider)
            {
                EffectStepDisplayTbl.Text = value.ToString();
            }
        }

        private void SelectEffectIdBtn_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem == null)
            {
                return;
            }
            int number = Int32.Parse(Regex.Match(menuItem.Name, @"\d+").Value);
            _selectedEffect = number;
            SelectEffectId0Btn.Background = Brushes.Transparent;
            SelectEffectId1Btn.Background = Brushes.Transparent;
            SelectEffectId2Btn.Background = Brushes.Transparent;
            SelectEffectId3Btn.Background = Brushes.Transparent;
            SelectEffectId4Btn.Background = Brushes.Transparent;
            SelectEffectId5Btn.Background = Brushes.Transparent;
            SelectEffectId6Btn.Background = Brushes.Transparent;
            SelectEffectId7Btn.Background = Brushes.Transparent;
            menuItem.Background = Brushes.Lime;
        }

        private void ApplyLightingSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
            {
                return;
            }
            RGBWSettings newRgbwSettings = new RGBWSettings();
            newRgbwSettings.ColorMode = "7";
            newRgbwSettings.EffectId = _selectedEffect.ToString();
            newRgbwSettings.DesiredColor = ((int)Math.Floor(RedAmountSlider.Value)).ToString("x2");
            newRgbwSettings.DesiredColor += ((int)Math.Floor(GreenAmountSlider.Value)).ToString("x2");
            newRgbwSettings.DesiredColor += ((int)Math.Floor(BlueAmountSlider.Value)).ToString("x2");
            newRgbwSettings.DesiredColor += ((int)Math.Floor(WarmWhiteAmountSlider.Value)).ToString("x2");
            newRgbwSettings.DesiredColor += ((int)Math.Floor(ColdWhiteAmountSlider.Value)).ToString("x2");
            FadeStepDurations newFadeStepDurations = new FadeStepDurations();
            newFadeStepDurations.ColorFade = Math.Floor(ColorFadeSlider.Value).ToString();
            newFadeStepDurations.EffectFade = Math.Floor(EffectFadeSlider.Value).ToString();
            newFadeStepDurations.EffectStep = Math.Floor(EffectStepSlider.Value).ToString();
            newRgbwSettings.DurationsMs = newFadeStepDurations;

            _wLightBoxObject.SetLightingStateAsync(newRgbwSettings);
        }

        private void NetworkConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
            {
                return;
            }
            _wLightBoxObject.ConnectToWifikAsync(SsidInputTbx.Text,NetwordPasswdInputTbx.Password);
        }

        private void NetworkDisconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
            {
                return;
            }
            _wLightBoxObject.DisconnectFromWifiAsync();
        }
    }
}
