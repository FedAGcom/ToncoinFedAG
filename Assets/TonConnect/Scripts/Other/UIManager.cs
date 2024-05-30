using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using TonSdk.Connect;
using TonSdk.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Message = TonSdk.Connect.Message;

/*using FirebaseWebGL.Examples.Utils;
using FirebaseWebGL.Scripts.FirebaseBridge;
using FirebaseWebGL.Scripts.Objects;*/

public class UIManager : MonoBehaviour
{
    [Header("UI References")] [SerializeField]
    private GameObject[] windows;

    [SerializeField] private GameObject customWalletPrefab;

    [SerializeField] private Transform customWalletTransform;
    [SerializeField] private List<CustomTonWallet> customWallets = new();

    [SerializeField] private Button connectWithButton, sendTranButton;
    [SerializeField] private TMP_InputField addressInput, sumInput;

    [SerializeField] private RawImage qr;
    [SerializeField] private TextMeshProUGUI walletId;

    public TonConnectHandler tonConnectHandler;
    [SerializeField] private GameManager gm;

    [Header("GAME VALUES")] public TextMeshProUGUI testValue;

    public User CurrentUser { get; private set; }
    
    private TouchScreenKeyboard keyboard;

    private void Awake()
    {
        TonConnectHandler.OnProviderStatusChanged -= OnProviderStatusChange;
        TonConnectHandler.OnProviderStatusChanged += OnProviderStatusChange;
        gm = FindObjectOfType<GameManager>();
    }

    private void OnProviderStatusChange(Wallet wallet)
    {
        print("OnProviderStatusChange");
        if (tonConnectHandler.tonConnect.IsConnected)
        {
            Debug.Log("Wallet connected. Address: " + wallet.Account.Address + ". Platform: " +
                      wallet.Device.Platform +
                      "," + wallet.Device.AppName + "," + wallet.Device.AppVersion);
            //TODO: тут мы получаем реальный баланс кошелька
            /*try
                {
                    long balance = await tonConnectHandler.balanceFetcher.GetWalletBalanceAsync(wallet.Account.Address.ToString(AddressType.Base64));
                    Debug.Log("Wallet balance: " + balance);
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error getting wallet balance: " + ex.Message);
                }*/

            if (wallet.Account.Address != null)
                RegisterOrAuthenticationUser(wallet.Account.Address.ToString());
        }
        else
        {
            ShowConnectModal();
        }
    }

    //Окно с кошельками
    private async void ShowConnectModal()
    {
        if (tonConnectHandler.tonConnect.IsConnected)
        {
            Debug.LogWarning(
                "Wallet already connected. The connection window has not been opened. Before proceeding, please disconnect from your wallet.");
            return;
        }
        await tonConnectHandler.CreateTonConnectInstance();

        if (customWallets.Count > 0)
            foreach (var t in customWallets)
            {
                Destroy(t.gameObject);
            }

        customWallets.Clear();

        foreach (var t in tonConnectHandler.CustomWalletConfigs)
        {
            WalletConfig wallet = new WalletConfig()
            {
                Name = t.Name,
                AboutUrl = t.AboutUrl,
                AppName = t.AppName,
                BridgeUrl = t.BridgeUrl,
                Image = t.Image,
                JsBridgeKey = t.JsBridgeKey,
                UniversalUrl = t.UniversalUrl
            };
            GameObject go = Instantiate(customWalletPrefab, customWalletTransform);
            CustomTonWallet cw = go.GetComponent<CustomTonWallet>();
            cw.walletConfig = wallet;
            customWallets.Add(cw);
        }

        windows[0].SetActive(false);
        windows[1].SetActive(true);
        windows[2].SetActive(false);
        windows[3].SetActive(false);
        windows[4].SetActive(false);
        Debug.Log("Form1. Wallets.");
    }

    //Окно с возможными коннектами кошельков
    public void OpenWalletQRContent(WalletConfig config)
    {
        var connectUrl = tonConnectHandler.tonConnect.Connect(config).Result;
        var qrCodeTexture = QRGenerator.EncodeString(connectUrl);

        windows[1].SetActive(false);
        windows[2].SetActive(true);

        connectWithButton.onClick.RemoveAllListeners();
        connectWithButton.onClick.AddListener(delegate { OpenWalletUrl(connectUrl); });
        qr.texture = qrCodeTexture;
        Debug.Log("Form2. Types of connection.");
    }

    //Переход по ссылке
    private void OpenWalletUrl(string url)
    {
        string escapedUrl = Uri.EscapeUriString(url);
        Application.OpenURL(escapedUrl);
    }


    //Отключение от тона
    public async void DisconnectWalletButtonClick()
    {
        gm.StopEngineGame();
        tonConnectHandler.RestoreConnectionOnAwake = false;
        await tonConnectHandler.tonConnect.Disconnect();
        ShowConnectModal();
        Debug.Log("Wallet Disconnected...");
    }

    //Выполняем транзакцию
    private async void SendTXModalSendButtonClick()
    {
        Address receiver = new(addressInput.text);
        Coins amount = new(double.Parse(sumInput.text));

        Message[] sendTons =
        {
            new Message(receiver, amount),
            //new Message(receiver, amount),
        };
        long validUntil = DateTimeOffset.Now.ToUnixTimeSeconds() + 600;
        SendTransactionRequest transactionRequest = new SendTransactionRequest(sendTons, validUntil);
        await tonConnectHandler.tonConnect.SendTransaction(transactionRequest);
    }

    //Открываем первое окно
    public void ShowConnectModalOnClick()
    {
        ShowConnectModal();
    }

    //Региструем или же аутентифицируем пользователя
    public void RegisterOrAuthenticationUser(string wallet)
    {
        try
        {
            Debug.Log("Form3. Connected to ton.");
            walletId.text = wallet;

            windows[0].SetActive(false);
            windows[1].SetActive(false);
            windows[2].SetActive(false);
            windows[3].SetActive(true);
            windows[4].SetActive(false);

            //TODO: REGISTER USER OR AUTH HERE IF ITS WE
            CurrentUser = LoadUser();

            if (CurrentUser == null)
            {
                CurrentUser = new User
                {
                    Id = 1,
                    Name = "TestUser",
                    WalletId = wallet,
                    TestValue = 0
                };
                SaveUser(CurrentUser);
                Debug.Log("New user created and saved.");
            }
            else
            {
                testValue.text = CurrentUser.TestValue.ToString();
            }

            gm.Initialize();
            Debug.Log("Game manager instantiated.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Произошла ошибка: {ex.Message}");
            Debug.LogError($"Stack Trace: {ex.StackTrace}");
        }
    }

    public void SaveUser(User user)
    {
        string userJson = JsonUtility.ToJson(user);
        Debug.Log("Saving user: " + userJson);
        PlayerPrefs.SetString("User", userJson);
        PlayerPrefs.Save();
    }

    User LoadUser()
    {
        string userJson = PlayerPrefs.GetString("User", null);
        Debug.Log("Loaded user: " + userJson);
        if (string.IsNullOrEmpty(userJson))
        {
            return null;
        }

        return JsonUtility.FromJson<User>(userJson);
    }

    public void DeleteUser()
    {
        PlayerPrefs.DeleteAll();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ShowSendTXModal()
    {
        windows[3].SetActive(false);
        windows[4].SetActive(true);
        sendTranButton.onClick.RemoveAllListeners();
        sendTranButton.onClick.AddListener(SendTXModalSendButtonClick);
        Debug.Log("Form4. Send coins.");
    }
    
    public void OpenMobileKeyboard(TMP_InputField text)
    {   
        keyboard = TouchScreenKeyboard.Open(text.text, TouchScreenKeyboardType.Default); 
    }

    private void OnDestroy()
    {
        TonConnectHandler.OnProviderStatusChanged -= OnProviderStatusChange;
    }
}