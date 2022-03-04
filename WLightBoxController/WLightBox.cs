namespace WLightBox
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public class WLightBox
    {
        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler { UseProxy = false });
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true};
        public DeviceInfo DeviceInfo { get; set; }
        public NetworkInfo NetworkInfo { get; set; }
        public RGBWSettings RGBW { get; set; }
        public async Task GetDeviceInfoAsync()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync("http://" + this.DeviceInfo.Ip + "/info");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                this.DeviceInfo = JsonSerializer.Deserialize<DeviceInfo>(responseBody, _serializerOptions);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Błąd połączenia.", ex);
            }
        }
        public async Task EstablishConnection(string ip)
        {
            try
            {
                using (var tokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)))
                {
                    HttpResponseMessage response = await _httpClient.GetAsync("http://" + ip + "/info", tokenSource.Token);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    this.DeviceInfo = JsonSerializer.Deserialize<DeviceInfo>(responseBody, _serializerOptions);
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Błąd połączenia.", ex);
            }
        }
        public async Task UpdateFirmwareAsync()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync("http://" + this.DeviceInfo.Ip + "/api/ota/update", null);
                string responseString = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Błąd połączenia.", ex);
            }
        }
        public async Task GetNetworkInfoAsync()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync("http://" + this.DeviceInfo.Ip + "/api/device/network");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                this.NetworkInfo = JsonSerializer.Deserialize<NetworkInfo>(responseBody, _serializerOptions);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Błąd połączenia.", ex);
            }
        }
        public async Task SetInternalAccesPointAsync(bool apEnable, string apSSID, string apPasswd)
        {
            apSettings apSettingsData = new apSettings();
            apSettingsData.apEnable = apEnable.ToString();
            apSettingsData.apSSID = apSSID;
            apSettingsData.apPasswd = apPasswd;
            var network = new Dictionary<string, string>
            {
                {"network", JsonSerializer.Serialize(apSettingsData)}
            };
            try
            {
                var content = new FormUrlEncodedContent(network);
                HttpResponseMessage response = await _httpClient.PostAsync("http://" + this.DeviceInfo.Ip + "/api/device/set", content);
                string responseString = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Błąd połączenia.", ex);
            }
        }
        public async Task ScanNetworksAsync()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync("http://" + this.DeviceInfo.Ip + "/api/wifi/scan");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                var availableWifis = JsonSerializer.Deserialize<List<wifi>>(responseBody, _serializerOptions);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Błąd połączenia.", ex);
            }
        }
        public async Task ConnectToWifikAsync(string ssid, string pwd)
        {
            var connectionData = new Dictionary<string, string>
            {
                {"ssid", ssid},
                {"pwd", pwd}
            };
            try
            {
                var content = new FormUrlEncodedContent(connectionData);
                HttpResponseMessage response = await _httpClient.PostAsync("http://" + this.DeviceInfo.Ip + "/api/wifi/connect", content);
                string responseString = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Błąd połączenia.", ex);
            }
        }
        public async Task DisconnectFromWifiAsync()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync("http://" + this.DeviceInfo.Ip + "/api/wifi/disconnect", null);
                string responseString = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Błąd połączenia.", ex);
            }
        }
        public async Task GetCurrentLightingStateAsync()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync("http://" + this.DeviceInfo.Ip + "/api/rgbw/state");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                this.RGBW = JsonSerializer.Deserialize<RGBWSettings>(responseBody, _serializerOptions);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Błąd połączenia.", ex);
            }
        }
        public async Task SetLightingStateAsync(RGBWSettings desiredRGBWSettings)
        {
            var rgbwData = new Dictionary<string, string>
            {
                {"rgbw", JsonSerializer.Serialize(desiredRGBWSettings)}
            };
            try
            {
                var content = new FormUrlEncodedContent(rgbwData);
                HttpResponseMessage response = await _httpClient.PostAsync("http://" + this.DeviceInfo.Ip + "/api/rgbw/set", content);
                string responseString = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Błąd połączenia.", ex);
            }
        }
    }

    public struct DeviceInfo
    {
        public string DeviceName { get; set; }
        public string Product { get; set; }
        public string Type { get; set; }
        public string ApiLevel { get; set; }
        public string Hv { get; set; }
        public string Fv { get; set; }
        public string Id { get; set; }
        public string Ip { get; set; }
    }

    public struct NetworkInfo
    {
        public string Ssid { get; set; }
        public string Mac { get; set; }
        public string Station_status { get; set; }
    }

    public struct RGBWSettings
    {
        public string ColorMode { get; set; }
        public string EffectId { get; set; }
        public string DesiredColor { get; set; }
        public string CurrentColor { get; set; }
        public string LastOnColor { get; set; }
        public FadeStepDurations DurationsMs { get; set; }
    }

    public struct FadeStepDurations
    {
        public string ColorFade { get; set; }
        public string EffectFade { get; set; }
        public string EffectStep { get; set; }
    }

    internal struct apSettings
    {
        public string apEnable { get; set; }
        public string apSSID { get; set; }
        public string apPasswd { get; set; }
    }

    internal struct wifi
    {
        public string ssid { get; set; }
        public string rssi { get; set; }
        public string enc { get; set; }
    }
}