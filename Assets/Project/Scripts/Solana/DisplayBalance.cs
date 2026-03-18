using TMPro;
using UnityEngine;
using System.Collections;
using System.Globalization;

public class DisplayBalance : MonoBehaviour
{
    private TextMeshProUGUI _txtBalance;
    private Coroutine _pollRoutine;

    private void Awake()
    {
        _txtBalance = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        _pollRoutine = StartCoroutine(PollLidBalance());
    }

    private void OnDisable()
    {
        if (_pollRoutine != null)
        {
            StopCoroutine(_pollRoutine);
            _pollRoutine = null;
        }
    }

    private IEnumerator PollLidBalance()
    {
        while (true)
        {
            if (_txtBalance != null)
            {
                if (SolanaManager.Instance == null || !SolanaManager.Instance.IsWalletConnected())
                {
                    _txtBalance.text = "LID: —";
                }
                else
                {
                    var t = SolanaManager.Instance.GetLidBalanceUiAsync();
                    yield return new WaitUntil(() => t.IsCompleted);

                    if (t.IsFaulted)
                    {
                        _txtBalance.text = "LID: ?";
                    }
                    else
                    {
                        _txtBalance.text = $"LID: {t.Result.ToString(CultureInfo.InvariantCulture)}";
                    }
                }
            }

            yield return new WaitForSeconds(1f);
        }
    }
}
