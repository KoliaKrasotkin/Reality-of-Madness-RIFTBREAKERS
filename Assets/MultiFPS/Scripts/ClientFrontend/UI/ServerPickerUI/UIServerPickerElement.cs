using System;
using UnityEngine;
using UnityEngine.UI;

public class UIServerPickerElement : MonoBehaviour
{
    [SerializeField] Text _regionNameRenderer;
    [SerializeField] Text _latencyRenderer;

    UIServerPicker _serverPickerUI;
    DNRegion _myRegion;

    public void Setup(UIServerPicker serverPicker, DNRegion region) 
    {
        GetComponent<Button>().onClick.AddListener(OnClick);

        _serverPickerUI = serverPicker;
        _regionNameRenderer.text = region.Name;

        _myRegion = region;
        _myRegion.OnStatusChanged += UpdateStatus;

        UpdateStatus(region.Ping);
    }

    private void OnClick()
    {
        _serverPickerUI.SelectRegion(_myRegion);
        _serverPickerUI.CloseMenu();
    }

    private void OnDestroy()
    {
        if(_myRegion != null)
            _myRegion.OnStatusChanged -= UpdateStatus;
    }

    void UpdateStatus(short ping) 
    {
        transform.SetAsLastSibling();
        _serverPickerUI.OnServerStatusUpdated();

        gameObject.SetActive(ping != -1);

        if (ping > -1)
        {
            _latencyRenderer.text = $"{ping}ms";
            _latencyRenderer.color =
                ping < 70 ? Color.green :
                ping < 120 ? Color.yellow :
                Color.red;
        }
    }
}
