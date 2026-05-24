using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Net;
using Android.OS;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Java.Interop;

namespace InventoryReconciliation.Mobile;

[Activity(
    Label = "@string/app_name",
    MainLauncher = true,
    Theme = "@style/AppTheme",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.KeyboardHidden)]
public sealed class MainActivity : Activity
{
    private static readonly string[] RuntimePermissions =
    [
        Manifest.Permission.Camera,
        Manifest.Permission.RecordAudio,
        Manifest.Permission.AccessFineLocation,
        Manifest.Permission.AccessCoarseLocation
    ];

    private WebView? _portalWebView;
    private ProgressBar? _pageProgress;
    private View? _offlinePanel;
    private string _portalUrl = string.Empty;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        Window?.SetStatusBarColor(Color.ParseColor("#080D19"));
        Window?.SetNavigationBarColor(Color.ParseColor("#080D19"));

        _portalUrl = NormalizeUrl(GetManagedAzureUrl() ?? GetString(Resource.String.azure_app_url));
        SetContentView(Resource.Layout.activity_main);

        _portalWebView = FindViewById<WebView>(Resource.Id.portalWebView);
        _pageProgress = FindViewById<ProgressBar>(Resource.Id.pageProgress);
        _offlinePanel = FindViewById(Resource.Id.offlinePanel);

        FindViewById<Button>(Resource.Id.retryButton)?.SetOnClickListener(new ClickHandler(() =>
        {
            ShowOffline(false);
            _portalWebView?.LoadUrl(_portalUrl);
        }));

        ConfigureWebView();
        RequestRuntimePermissions();
        _portalWebView?.LoadUrl(_portalUrl);
    }

    public override void OnBackPressed()
    {
        if (_portalWebView?.CanGoBack() == true)
        {
            _portalWebView.GoBack();
            return;
        }

        base.OnBackPressed();
    }

    protected override void OnDestroy()
    {
        _portalWebView?.StopLoading();
        _portalWebView?.Destroy();
        base.OnDestroy();
    }

    private void ConfigureWebView()
    {
        if (_portalWebView is null)
        {
            return;
        }

        WebView.SetWebContentsDebuggingEnabled(false);
        var cookieManager = CookieManager.Instance;
        if (cookieManager is not null)
        {
            cookieManager.SetAcceptCookie(true);
            cookieManager.SetAcceptThirdPartyCookies(_portalWebView, true);
        }

        var settings = _portalWebView.Settings;
        settings.JavaScriptEnabled = true;
        settings.DomStorageEnabled = true;
        settings.DatabaseEnabled = true;
        settings.LoadWithOverviewMode = true;
        settings.UseWideViewPort = true;
        settings.BuiltInZoomControls = false;
        settings.DisplayZoomControls = false;
        settings.MediaPlaybackRequiresUserGesture = false;
        settings.MixedContentMode = MixedContentHandling.NeverAllow;
        settings.UserAgentString = $"{settings.UserAgentString} ReconIQMobile/1.0 Intune";

        _portalWebView.SetWebViewClient(new PortalWebViewClient(this));
        _portalWebView.SetWebChromeClient(new PortalChromeClient(this));
        _portalWebView.SetDownloadListener(new DownloadListener(this));
    }

    private void RequestRuntimePermissions()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.M)
        {
            return;
        }

        var missingPermissions = RuntimePermissions
            .Where(permission => CheckSelfPermission(permission) != Permission.Granted)
            .ToArray();

        if (missingPermissions.Length > 0)
        {
            RequestPermissions(missingPermissions, 1001);
        }
    }

    private void OpenExternalUrl(string url)
    {
        try
        {
            StartActivity(new Intent(Intent.ActionView, global::Android.Net.Uri.Parse(url)));
        }
        catch
        {
            Toast.MakeText(this, "No app is available to open this link.", ToastLength.Short)?.Show();
        }
    }

    private void ShowOffline(bool visible)
    {
        if (_offlinePanel is not null)
        {
            _offlinePanel.Visibility = visible ? ViewStates.Visible : ViewStates.Gone;
        }

        if (_portalWebView is not null)
        {
            _portalWebView.Visibility = visible ? ViewStates.Gone : ViewStates.Visible;
        }
    }

    private void SetPageProgress(int progress)
    {
        if (_pageProgress is null)
        {
            return;
        }

        _pageProgress.Progress = progress;
        _pageProgress.Visibility = progress is > 0 and < 100 ? ViewStates.Visible : ViewStates.Gone;
    }

    private static string NormalizeUrl(string? value)
    {
        var url = string.IsNullOrWhiteSpace(value)
            ? "https://inventoryreconciliation.azurewebsites.net/"
            : value.Trim();

        return url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? url
            : $"https://{url.TrimStart('/')}";
    }

    private string? GetManagedAzureUrl()
    {
        if (GetSystemService(RestrictionsService) is not RestrictionsManager restrictionsManager)
        {
            return null;
        }

        var restrictions = restrictionsManager.ApplicationRestrictions;
        return string.IsNullOrWhiteSpace(restrictions?.GetString("azure_app_url"))
            ? null
            : restrictions.GetString("azure_app_url");
    }

    private sealed class PortalWebViewClient(MainActivity activity) : WebViewClient
    {
        public override bool ShouldOverrideUrlLoading(WebView? view, IWebResourceRequest? request)
        {
            var url = request?.Url?.ToString();
            return ShouldOpenExternally(url);
        }

        public override bool ShouldOverrideUrlLoading(WebView? view, string? url) =>
            ShouldOpenExternally(url);

        public override void OnPageStarted(WebView? view, string? url, Bitmap? favicon)
        {
            activity.ShowOffline(false);
            activity.SetPageProgress(8);
            base.OnPageStarted(view, url, favicon);
        }

        public override void OnPageFinished(WebView? view, string? url)
        {
            activity.SetPageProgress(100);
            CookieManager.Instance?.Flush();
            base.OnPageFinished(view, url);
        }

        public override void OnReceivedError(WebView? view, IWebResourceRequest? request, WebResourceError? error)
        {
            if (request?.IsForMainFrame == true)
            {
                activity.ShowOffline(true);
            }

            base.OnReceivedError(view, request, error);
        }

        private bool ShouldOpenExternally(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            activity.OpenExternalUrl(url);
            return true;
        }
    }

    private sealed class PortalChromeClient(MainActivity activity) : WebChromeClient
    {
        public override void OnProgressChanged(WebView? view, int newProgress)
        {
            activity.SetPageProgress(newProgress);
            base.OnProgressChanged(view, newProgress);
        }

        public override void OnPermissionRequest(PermissionRequest? request)
        {
            activity.RunOnUiThread(() =>
            {
                request?.Grant(request.GetResources());
            });
        }

        public override void OnGeolocationPermissionsShowPrompt(string? origin, GeolocationPermissions.ICallback? callback)
        {
            callback?.Invoke(origin, true, false);
        }
    }

    private sealed class DownloadListener(MainActivity activity) : Java.Lang.Object, IDownloadListener
    {
        public void OnDownloadStart(string? url, string? userAgent, string? contentDisposition, string? mimetype, long contentLength)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                activity.OpenExternalUrl(url);
            }
        }
    }

    private sealed class ClickHandler(Action action) : Java.Lang.Object, View.IOnClickListener
    {
        public void OnClick(View? view) => action();
    }
}
