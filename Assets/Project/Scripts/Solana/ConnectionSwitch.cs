using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionSwitch : MonoBehaviour
{
    [SerializeField] private Button btnConnectWallet;
    [SerializeField] private Button btnDisconnectWallet;
    [SerializeField] private GameObject walletAddressText;
    [SerializeField] private GameObject walletBalance;

    private void Start()
    {
       btnDisconnectWallet.onClick.AddListener(call:() => Web3.Instance.Logout());
    }
  
    private void OnEnable()
    {
        Web3.OnLogin += OnLogin;
        Web3.OnLogout += OnLogout;
    }

    private void OnDisable()
    {
        Web3.OnLogin -= OnLogin;
        Web3.OnLogout -= OnLogout;
    }

    private void OnLogin(Account obj)
    {
        btnConnectWallet.gameObject.SetActive(false);
        btnDisconnectWallet.gameObject.SetActive(true);
        walletAddressText.SetActive(true);
        walletBalance.SetActive(true);
    }

    private void OnLogout()
    {
        btnConnectWallet.gameObject.SetActive(true);
        btnDisconnectWallet.gameObject.SetActive(false);
        walletAddressText.SetActive(false);
        walletBalance.SetActive(false);
    }
}
