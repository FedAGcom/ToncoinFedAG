using TonSdk.Connect;
using UnityEngine;
using UnityEngine.UI;

public class CustomTonWallet : MonoBehaviour
{
    public RawImage image;
    public WalletConfig walletConfig;

    public UIManager uiManager;

    private void Awake()
    {
        uiManager = FindObjectOfType<UIManager>();
    }

    public void OpenWallet()
    {
        uiManager.OpenWalletQRContent(walletConfig);
    }
}
