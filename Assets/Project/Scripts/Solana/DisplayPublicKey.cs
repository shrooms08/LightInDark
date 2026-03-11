using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using TMPro;
using UnityEngine;

public class DisplayPublicKey : MonoBehaviour
{
    private TextMeshProUGUI _txtPublicKey;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _txtPublicKey = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    private void OnEnable()
    {
        Web3.OnLogin += OnLogin;
    }

    private void OnDisable()
    {
        Web3.OnLogin -= OnLogin;
    }

    private void OnLogin(Account account)
    {
        _txtPublicKey.text = (string)account.PublicKey;
    }
}
