using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UICursorManager : MonoBehaviour
{
    static List<GameObject> _cursorRequestors = new List<GameObject>();
    static UICursorManager _instance;

#if UNITY_EDITOR
    public List<GameObject> _cursorReuqestorList;
#endif

    private void Awake()
    {
#if UNITY_EDITOR
        _cursorReuqestorList = _cursorRequestors;
#endif

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _cursorRequestors.RemoveAll(x => x == null);
    }

    

    /// <summary>
    /// If true, player can be controlled, for example, when pause menu is shwoing, it is false, so player cannot move
    /// or look around
    /// </summary>
    public static bool GamePlayInput()
    {
        return _cursorRequestors.Count == 0;
    }

    public static void RequestCursor(GameObject requestHolder)
    {
        if (_cursorRequestors.Contains(requestHolder))
        {
            Debug.LogError($"MultiFPS.UI: Object {requestHolder.name} is already holding cursor request.");
            return;
        }

        _cursorRequestors.Add(requestHolder);

        if (_cursorRequestors.Count == 1)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
        }
    }

    public static void ReleaseCursor(GameObject requestHolder)
    {
        if (!_cursorRequestors.Contains(requestHolder))
        {
            Debug.LogError($"MultiFPS.UI: Object {requestHolder.name} is not holding cursor, so it can't release it.");
            return;
        }

        _cursorRequestors.Remove(requestHolder);

        if (_cursorRequestors.Count == 0)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
