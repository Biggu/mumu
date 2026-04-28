using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

public class MumuButton : Button
{


    [Header("Text Component")]
    [SerializeField] private TextMeshProUGUI textComponent = null;

    [Header("Long Press Settings")]
    [SerializeField] private float longPressTime = 1.0f;
    [SerializeField] private float repeatInterval = 0.1f;

    private Action onClickAction = null;
    private Action onPressAction = null;
    private Action onLongPressAction = null;

    private bool isPointerDown = false;
    private float pressTime = 0f;
    private bool longPressTriggered = false;
    private CancellationTokenSource longPressCts = null;


    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        isPointerDown = true;
        pressTime = Time.time;
        longPressTriggered = false;

        onPressAction?.Invoke();

        // Cancel any existing long press
        longPressCts?.Cancel();
        longPressCts = new CancellationTokenSource();

        // Run async long press detection
        LongPressAsync(longPressCts.Token).Forget();
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);

        // Cancel long press
        longPressCts?.Cancel();
        longPressCts = null;

        if (isPointerDown)
        {
            float pressDuration = Time.time - pressTime;

            // If press was shorter than long press time, treat as click
            if (pressDuration < longPressTime)
            {
                onClickAction?.Invoke();
            }
        }

        isPointerDown = false;
        longPressTriggered = false;
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        // We handle click in OnPointerUp to distinguish from long press
        // So we don't call base.OnPointerClick here
    }

    private async UniTask LongPressAsync(CancellationToken token)
    {
        float elapsed = 0f;
        float repeatElapsed = 0f;

        try
        {
            while (isPointerDown && !token.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                repeatElapsed += Time.deltaTime;

                if (elapsed >= longPressTime)
                {
                    if (!longPressTriggered)
                    {
                        longPressTriggered = true;
                        repeatElapsed = 0f;
                        onLongPressAction?.Invoke();
                    }
                    else if (repeatElapsed >= repeatInterval)
                    {
                        repeatElapsed = 0f;
                        onLongPressAction?.Invoke();
                    }
                }

                await UniTask.Yield();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled
        }
    }

    #region Public Methods

    /// <summary>
    /// Add a click action (triggered on short press release)
    /// </summary>
    public void SetOnClick(Action _a)
    {
        onClickAction = _a;
    }

    /// <summary>
    /// Add a press action (triggered when pointer goes down)
    /// </summary>
    public void SetOnPress(Action _a)
    {
        onPressAction = _a;
    }

    /// <summary>
    /// Add a long press action (triggered after holding for longPressTime)
    /// </summary>
    public void SetOnLongPress(Action _a)
    {
        onLongPressAction = _a;
    }

    /// <summary>
    /// Clear all registered actions
    /// </summary>
    public void ClearAllActions()
    {
        onClickAction = null;
        onPressAction = null;
        onLongPressAction = null;
    }

    /// <summary>
    /// Set the button text
    /// </summary>
    public void SetText(string text)
    {
        if (textComponent != null)
        {
            textComponent.text = text;
        }
    }

    #endregion

    #region Inspector Methods

    /// <summary>
    /// Set long press duration threshold
    /// </summary>
    public void SetLongPressTime(float time)
    {
        longPressTime = Mathf.Max(0.1f, time);
    }

    /// <summary>
    /// Set repeat interval for long press
    /// </summary>
    public void SetRepeatInterval(float interval)
    {
        repeatInterval = Mathf.Max(0.01f, interval);
    }

    #endregion
}