using UnityEngine;
using TonSdk.Connect;
using System;
using System.Threading;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

public class TonConnectHandler : MonoBehaviour
{
    [Header("Plugin Settings")]
    [Tooltip("Toggle if you want to use injected/web wallets. \nOnly works in WebGL builds!")]
    public bool UseWebWallets = false;

    [Tooltip("Toggle if you want to restore saved connection from the storage. (recommended)")]
    public bool RestoreConnectionOnAwake = true;

    [Space(4)]
    [Header("TonConnect Settings")]
    [Tooltip("Url to the manifest with the Dapp metadata that will be displayed in the user's wallet.")]
    public string ManifestURL = "";

    [Tooltip(
        "Redefine wallets list source URL.Must be a link to a json file with following structure - https://github.com/ton-connect/wallets-list (optional)")]
    public string WalletsListSource = "";

    [Tooltip("Wallets list cache time to live in milliseconds. (optional)")]
    public int WalletsListCacheTTL = 0;

    [HideInInspector]
    public delegate void OnProviderStatusChange(Wallet wallet);

    [HideInInspector] public static event OnProviderStatusChange OnProviderStatusChanged;

    public TonConnect tonConnect { get; private set; }
    public WalletBalanceFetcher balanceFetcher;

    public CustomWalletConfig[] CustomWalletConfigs;

    [SerializeField] private UIManager uiManager;

    private async void Start()
    {
        CheckHandlerSettings();
        await CreateTonConnectInstance();
    }

    public async UniTask CreateTonConnectInstance()
    {
        if (tonConnect != null) return;
        
        TonConnectOptions options = new()
        {
            ManifestUrl = ManifestURL,
            WalletsListSource = WalletsListSource,
            WalletsListCacheTTLMs = WalletsListCacheTTL
        };

        RemoteStorage remoteStorage = new(new(PlayerPrefs.GetString), new(PlayerPrefs.SetString),
            new(PlayerPrefs.DeleteKey), new(PlayerPrefs.HasKey));

        // Additional connect options used to set custom SSE listener
        // cause, Unity should work with requests in IEnumerable class
        AdditionalConnectOptions additionalConnectOptions = new()
        {
            listenEventsFunction = new ListenEventsFunction(ListenEvents),
            sendGatewayMessage = new SendGatewayMessage(SendRequest)
        };

        tonConnect = new TonConnect(options, remoteStorage, additionalConnectOptions);
        tonConnect.OnStatusChange(OnStatusChange);

        if (RestoreConnectionOnAwake)
        {
            bool result = await tonConnect.RestoreConnection();
            print($"Connection restored: {result}");
        }
        else
        {
            remoteStorage.RemoveItem(RemoteStorage.KEY_CONNECTION);
            remoteStorage.RemoveItem(RemoteStorage.KEY_LAST_EVENT_ID);
        }
    }

    private void OnStatusChange(Wallet wallet) => OnProviderStatusChanged?.Invoke(wallet);

    private void CheckHandlerSettings()
    {
        // Here we check if the settings are valid

        // UseWebWallets must be true, only in WebGL
        // ManifestURL must not be empty
#if !UNITY_WEBGL || UNITY_EDITOR
        if (UseWebWallets)
        {
            UseWebWallets = false;
            Debug.LogWarning(
                "The 'UseWebWallets' property has been automatically disabled due to platform incompatibility. It should be used specifically in WebGL builds.");
        }
#endif
        if (ManifestURL.Length == 0)
            throw new ArgumentNullException(
                "'ManifestUrl' field cannot be empty. Please provide a valid URL in the 'ManifestUrl' field.");
    }

    //private void OnStatusChange(Wallet wallet) => OnProviderStatusChanged?.Invoke(wallet);
    //private void OnStatusChangeError(string error) => OnProviderStatusChangedError?.Invoke(error);

    private async UniTask SendPostRequest(string bridgeUrl, string postPath, string sessionId, string receiver, int ttl,
        string topic, byte[] message)
    {
        string url = $"{bridgeUrl}/{postPath}?client_id={sessionId}&to={receiver}&ttl={ttl}&topic={topic}";

        UnityWebRequest request = new(url, "POST")
        {
            uploadHandler = new UploadHandlerRaw(message)
        };
        //request.SetRequestHeader("mode", "no-cors");
        request.SetRequestHeader("Content-Type", "text/plain");

        var asyncOperation = request.SendWebRequest();
        while (!asyncOperation.isDone)
        {
            await UniTask.Yield();
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error while sending request: " + request.error);
        }
        else
        {
            print("Request successfully sent.");
        }
    }

    private void SendRequest(string bridgeUrl, string postPath, string sessionId, string receiver, int ttl,
        string topic, byte[] message)
    {
        SendPostRequest(bridgeUrl, postPath, sessionId, receiver, ttl, topic, message).Forget();
    }

    private void ListenEvents(CancellationToken cancellationToken, string url, ProviderMessageHandler handler,
        ProviderErrorHandler errorHandler)
    {
        ListenForEvents(cancellationToken, url, handler, errorHandler).Forget();
    }

    private async UniTask ListenForEvents(CancellationToken cancellationToken, string url,
        ProviderMessageHandler handler, ProviderErrorHandler errorHandler)
    {
        UnityWebRequest request = new(url)
        {
            method = UnityWebRequest.kHttpVerbGET
        };
        request.SetRequestHeader("Accept", "text/event-stream");

        DownloadHandlerBuffer handlerBuff = new();
        request.downloadHandler = handlerBuff;

        AsyncOperation operation = request.SendWebRequest();

        int currentPosition = 0;

        while (!cancellationToken.IsCancellationRequested && !operation.isDone)
        {
            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                errorHandler(new Exception("SSE request error: " + request.error));
                print("Err");
                break;
            }

            string text = handlerBuff.text.Substring(currentPosition);

            string[] lines = text.Split('\n');
            foreach (string line in lines)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    handler(line);
                }
            }

            currentPosition += text.Length;

            await UniTask.Yield();
        }
    }

    /// <summary>
    /// Handle injected wallet message from js side. Don't use it directly
    /// </summary>
    public void OnInjectedWalletMessageReceived(string message)
    {
        tonConnect.ParseInjectedProviderMessage(message);
    }

    public async Task<List<WalletConfig>> LoadWallets(string url)
    {
        List<WalletConfig> wallets = new();
        UnityWebRequest www = UnityWebRequest.Get(url);

        var asyncOperation = www.SendWebRequest();
        while (!asyncOperation.isDone)
        {
            await UniTask.Yield();
        }

        if (asyncOperation.webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("HTTP Error: " + www.error);
        }
        else
        {
            List<Dictionary<string, object>> walletsList = null;
            var response = asyncOperation.webRequest.downloadHandler.text;
            walletsList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);
            foreach (var t in walletsList)
            {
                if (t == null)
                {
                    print("Not supported wallet: is not a dictionary -> " + t);
                    continue;
                }

                if (!t.ContainsKey("name") || !t.ContainsKey("image") ||
                    !t.ContainsKey("about_url") || !t.ContainsKey("bridge"))
                {
                    print("Not supported wallet. Config -> " + t);
                    continue;
                }

                List<Dictionary<string, object>> bridges =
                    JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(t["bridge"]
                        .ToString());
                if (bridges == null || bridges.Count == 0)
                {
                    print("Not supported wallet: bridges is not a list or len is equal 0, config -> " +
                          t);
                    continue;
                }

                WalletConfig walletConfig = new WalletConfig()
                {
                    Name = t["name"].ToString(),
                    Image = t["image"].ToString(),
                    AboutUrl = t["about_url"].ToString(),
                    AppName = t["app_name"].ToString()
                };

                foreach (var bridge in bridges)
                {
                    if (bridge.TryGetValue("type", out object value) && value.ToString() == "sse")
                    {
                        if (!bridge.TryGetValue("url", out var value1))
                        {
                            print("Not supported wallet: bridge url not found, config -> " + t);
                            continue;
                        }

                        walletConfig.BridgeUrl = value1.ToString();
                        if (t.TryGetValue("universal_url", out object urlUni))
                            walletConfig.UniversalUrl = urlUni.ToString();
                        if (walletConfig.JsBridgeKey != null) walletConfig.JsBridgeKey = null;
                        wallets.Add(walletConfig);
                    }
                    else if (value?.ToString() == "js")
                    {
                        if (!bridge.TryGetValue("key", out var value1))
                        {
                            print("Not supported wallet: bridge key not found, config -> " + t);
                            continue;
                        }

                        walletConfig.JsBridgeKey = value1.ToString();
                        if (walletConfig.BridgeUrl != null) walletConfig.BridgeUrl = null;
                        wallets.Add(walletConfig);
                        print($"bridge key js: {walletConfig.JsBridgeKey}");
                    }

                    print("===");
                }

                if (walletConfig.BridgeUrl == null && walletConfig.JsBridgeKey == null) continue;
            }
        }

        return wallets;
    }
}

[Serializable]
public class CustomWalletConfig
{
    public string Name;
    public Sprite Sprite;
    public string Image;
    public string AboutUrl;
    public string AppName;
    public string BridgeUrl;
    public string JsBridgeKey;
    public string UniversalUrl;
}