using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class WalletBalanceFetcher : MonoBehaviour
{
    //TODO: УБРАТЬ ТЕСТНЕТ ПОСЛЕ ТЕСТОВ
    private const string balanceApiUrl = "https://testnet.toncenter.com/api/v2/getAddressBalance?address=";

    public async Task<long> GetWalletBalanceAsync(string walletAddress)
    {
        string url = balanceApiUrl + walletAddress;
        UnityWebRequest request = UnityWebRequest.Get(url);
        
        var asyncOperation = request.SendWebRequest();
        
        while (!asyncOperation.isDone)
            await Task.Yield();
        
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error fetching balance: " + request.error);
            return 0;
        }

        string jsonResponse = request.downloadHandler.text;
        var balanceResponse = JsonUtility.FromJson<BalanceResponse>(jsonResponse);
        if (balanceResponse != null && balanceResponse.ok && long.TryParse(balanceResponse.result, out long balance))
        {
            return balance;
        }
        Debug.LogError("Error parsing balance response.");
        return 0;
    }

    [System.Serializable]
    public class BalanceResponse
    {
        public bool ok;
        public string result;
    }

    [System.Serializable]
    private class BalanceResult
    {
        public long balance;
    }
}
