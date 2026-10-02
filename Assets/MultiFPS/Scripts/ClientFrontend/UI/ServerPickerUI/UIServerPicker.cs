using System;
using UnityEngine;
using UnityEngine.UI;
using DNServerList;
using Mirror.SimpleWeb;
using Mirror;
public class UIServerPicker : MonoBehaviour
{
    [SerializeField] Text _currentStateRenderer;
    [SerializeField] Button _showRegionsButton;
    [SerializeField] Button _closeMenuButton;
    [SerializeField] GameObject _regionButtonPrefab;
    [SerializeField] GameObject _menu;

    private void Start()
    {
        _currentStateRenderer.text = "Waiting for servers...";

        DNRegionManager regionManager = DNRegionManager.Sigleton;

        if (regionManager == null)
            return;

        regionManager.OnRegionFound += OnRegionFound;
        regionManager.OnAllRegionsChecked += OnAllRegionsChecked;
        regionManager.OnBestRegionFound += OnBestRegionFound;
        regionManager.GetBestRegion();
        ShowMenu();


        for (int i = 0; i < regionManager.Regions.Length - 1; i++)
        {
            UIServerPickerElement regionButton = Instantiate(_regionButtonPrefab, _regionButtonPrefab.transform.parent).
                GetComponent<UIServerPickerElement>();

            regionButton.Setup(this, regionManager.Regions[i]);
        }

        UIServerPickerElement lastregionButton = _regionButtonPrefab.
            GetComponent<UIServerPickerElement>();

        lastregionButton.Setup(this, regionManager.Regions[regionManager.Regions.Length - 1]);

        //after drawing ui hook events
        _showRegionsButton.GetComponent<Button>().onClick.AddListener(ButtonShowMenu);
        _closeMenuButton.GetComponent<Button>().onClick.AddListener(CloseMenu);

        //this will place cancel button underneath every region button
        _closeMenuButton.transform.parent.SetAsLastSibling();
    }

    private void OnBestRegionFound(DNRegion region)
    {
        SelectRegion(region);
    }

    private void OnRegionFound(DNRegion arg0)
    {
        if(!_closeMenuButton.gameObject.activeSelf)
            _closeMenuButton.gameObject.SetActive(true);
    }

    private void OnAllRegionsChecked()
    {
        DNRegion[] regions = DNRegionManager.Sigleton.Regions;
        for (int i = 0; i < regions.Length; i++)
        {
            if (regions[i].Ping > 0)
                return;
        }

        print("No region responded, calling them again...");
        DNRegionManager.Sigleton.GetBestRegion();
    }

    public void ButtonShowMenu() 
    {
        ShowMenu();
        DNRegionManager.Sigleton.PingAllRegions();
    }

    public void ShowMenu() 
    {
        _closeMenuButton.gameObject.SetActive(false);
        _menu.SetActive(true);
    }

    public void CloseMenu() 
    {
        _menu.SetActive(false);
    }

    internal void OnServerStatusUpdated()
    {
        _closeMenuButton.transform.parent.SetAsLastSibling();
    }

    internal void SelectRegion(DNRegion region) 
    {
        ServerListClient.RegionURL = region.Url;
        _menu.gameObject.SetActive(false);
        _currentStateRenderer.text = region.Name;

        NetworkManager nm = NetworkManager.singleton;
        if (nm == null)
            return;
            
        if (NetworkManager.singleton.TryGetComponent(out SimpleWebTransport simpleWebTransport)) 
        {
            simpleWebTransport.clientUseWss = region.GameServerUsesSSL;
        }
    }

    private void OnDestroy()
    {
        DNRegionManager regionManager = DNRegionManager.Sigleton;

        if (regionManager == null)
            return;

        regionManager.OnRegionFound -= OnRegionFound;
        regionManager.OnAllRegionsChecked -= OnAllRegionsChecked;
        regionManager.OnBestRegionFound -= OnBestRegionFound;
    }
}
