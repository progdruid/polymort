using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

public class TextPropertyUIField : MonoBehaviour
{
    //fields////////////////////////////////////////////////////////////////////////////////////////////////////////////
    [SerializeField] private TMP_Text textField;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private RectTransform target;

    public event Action<bool> EditingStateChangeEvent;
    
    //initialisation////////////////////////////////////////////////////////////////////////////////////////////////////
    private void Awake()
    {
        Assert.IsNotNull(textField);
        Assert.IsNotNull(inputField);
        Assert.IsNotNull(target);
    }
    
    //public interface//////////////////////////////////////////////////////////////////////////////////////////////////
    public RectTransform Target => target;
    public void SetProperty(PropertyHandle handle)
    {
        textField.text = handle.PropertyName + ": ";
        textField.rectTransform.sizeDelta = new Vector2(textField.preferredWidth, textField.rectTransform.sizeDelta.y);
        if (inputField.transform is RectTransform inputRect)
            inputRect.anchoredPosition = new Vector2(textField.preferredWidth, textField.rectTransform.anchoredPosition.y);
        
        inputField.text = handle.PropertyDefaultValue;
        inputField.contentType = handle.PropertyType switch
        {
            PropertyType.Decimal => TMP_InputField.ContentType.DecimalNumber,
            PropertyType.Integer => TMP_InputField.ContentType.IntegerNumber,
            PropertyType.Text => TMP_InputField.ContentType.Standard,
            _ => throw new ArgumentOutOfRangeException()
        };
        inputField.onSelect.AddListener((s) => EditingStateChangeEvent?.Invoke(true));
        inputField.onEndEdit.AddListener((s) =>
        {
            object val = handle.PropertyType switch
            {
                PropertyType.Decimal => float.Parse(s),
                PropertyType.Integer => int.Parse(s),
                PropertyType.Text => s,
                _ => throw new ArgumentOutOfRangeException()
            };
            handle.Setter.Invoke(s);
            EditingStateChangeEvent?.Invoke(false);
        });
        handle.ChangeEvent.AddListener(text => inputField.text = text.ToString());
    }
}
