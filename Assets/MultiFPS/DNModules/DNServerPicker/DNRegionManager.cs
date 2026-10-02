using DNWebRequest;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

#if UNITY_EDITOR
[System.Serializable]
#endif
public class DNRegion
{
    public short Ping; //-1 stands for region being not available
    public string Url;
    public string Name;
    public Stopwatch _tripTimeStopwatch;

    public bool GameServerUsesSSL;

    public Action<short> OnStatusChanged;

    public DNRegion() 
    {
        _tripTimeStopwatch = new Stopwatch();
    }
    public void StartMeasuringTripTime() 
    {
        Ping = -1;
        OnStatusChanged?.Invoke(Ping);
        _tripTimeStopwatch.Start();
    }
    public void CalculateTripTime() 
    {
        _tripTimeStopwatch.Stop();
        double nanoSecs = _tripTimeStopwatch.ElapsedTicks * (1000.0 / Stopwatch.Frequency);
        Ping = (short)(nanoSecs);
        _tripTimeStopwatch.Reset();

        OnStatusChanged?.Invoke(Ping);
    }

    public void CancelCalculatingTripTime() 
    {
        _tripTimeStopwatch.Stop();
        _tripTimeStopwatch.Reset();
    }
}

public class DNRegionManager : MonoBehaviour
{
    public static DNRegionManager Sigleton;

    public DNRegion[] Regions { private set; get; }
    bool _lookingForBestRegion;

    public UnityAction OnAllRegionsChecked { set; get; }
    public UnityAction<DNRegion> OnRegionFound { set; get; }
    public UnityAction<DNRegion> OnBestRegionFound { set; get; }
    
    int _regionsChecked;

    void Awake() 
    {
        RegionsContainer[] containers = Resources.LoadAll<RegionsContainer>("");

        List<RegionDefinition> regionsList = new();
        for (int i = 0; i < containers.Length; i++)
            regionsList.AddRange(containers[i].Regions);

        if (Sigleton) 
        {
            UnityEngine.Debug.LogError("There is already instance of RegionManager spawned!");
            return;
        }
        Sigleton = this;
        Regions = new DNRegion [regionsList.Count];

        //map regions definition to state holders, state holders contain additon info if regions is available for example, and its latency
        for (int i = 0; i < regionsList.Count; i++) 
        {
            RegionDefinition regionDefinition = regionsList[i];
            DNRegion region = new () 
            {
                Ping = -1,
                Name = regionDefinition.RegionName,
                Url = regionDefinition.Url,
                GameServerUsesSSL = regionDefinition.GameServerUsesSSL,
            };
            Regions[i] = region;
        }
    }

    private void OnDestroy()
    {
        if (Sigleton == this)
            Sigleton = null;
    }

    public void GetBestRegion() 
    {
        _lookingForBestRegion = true;
        PingAllRegions();
    }

    public void PingAllRegions() 
    {
        if (_regionsChecked > 0 && _regionsChecked < Regions.Length)
            return;

        _regionsChecked = 0;

        for (int i = 0; i < Regions.Length; i++)
            StartCoroutine(PingRegion(Regions[i]));
    }

    IEnumerator PingRegion(DNRegion region)
    {
        UnityWebRequest unityWebRequest = DNWebRequests.CreateWebRequest($"{region.Url}/matchmaking/ping", DNRequestType.GET);
        region.StartMeasuringTripTime();

        yield return unityWebRequest.SendWebRequest();

        _regionsChecked++;

        if (unityWebRequest.result == UnityWebRequest.Result.Success)
        {
            //calculating only if success
            region.CalculateTripTime();//(short)Mathf.RoundToInt((float)(Time.timeAsDouble - pingStart) * 1000);
            OnRegionFound?.Invoke(region);

            if (_lookingForBestRegion)
            {
                OnBestRegionFound.Invoke(region);
                _lookingForBestRegion = false;
            }
        }
        else
        {
            region.CancelCalculatingTripTime();
            //if (_lookingForBestRegion && _regionsChecked >= Regions.Length)
            //_onBestRegionFound(null);
            region.Ping = -1;
        }
        if (_regionsChecked >= Regions.Length)
        {
            OnAllRegionsChecked?.Invoke();
        }
    }
}
