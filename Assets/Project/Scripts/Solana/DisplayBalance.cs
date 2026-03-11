using System.Globalization;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using TMPro;
using UnityEngine;

public class DisplayBalance : MonoBehaviour
{
    private TextMeshProUGUI _txtBalance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _txtBalance = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        Web3.OnBalanceChange += OnBalanaceChange;
    }

    private void Osable()
    {
        Web3.OnBalanceChange -= OnBalanaceChange;
    }

    private void OnBalanaceChange(double amount)
    {
        _txtBalance.text = amount.ToString(CultureInfo.InvariantCulture);
    }
}
