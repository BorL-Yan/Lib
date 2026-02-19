using FPS_Battle.UIScripts;
using UnityEngine;

public abstract class ToggleButton : UIButton
{
    public bool isOn { get; set; }
    [SerializeField] protected GameObject _enabledIcon;

    private void OnEnable()
    {
        LoadSettings();
        Apply();
    }

    protected override void Click()
    {
        Toggle();
    }

    public virtual void Toggle()    
    {
        isOn = !isOn;
        Apply();
        SaveSettings();
    }

    protected virtual void Apply()
    {
        _enabledIcon.SetActive(isOn);
    }
    
    public abstract void SaveSettings();
    public abstract void LoadSettings();
}