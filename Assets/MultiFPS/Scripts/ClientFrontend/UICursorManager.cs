using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UICursorManager : MonoBehaviour
{
    static List<GameObject> _cursorRequestors = new List<GameObject>();
    static readonly HashSet<GameObject> nonModal = new HashSet<GameObject>();
    public static bool HasRequest(GameObject holder)=>holder&&_cursorRequestors.Contains(holder);
    public static bool NonModalOpen => _cursorRequestors.Exists(x=>x&&nonModal.Contains(x));
    public static bool IsTopRequestIn(Transform root){_cursorRequestors.RemoveAll(x=>!x);return _cursorRequestors.Count>0&&_cursorRequestors[_cursorRequestors.Count-1].transform.IsChildOf(root);}
    public static bool IsTopRequest(GameObject holder){_cursorRequestors.RemoveAll(x=>!x);return _cursorRequestors.Count>0&&_cursorRequestors[_cursorRequestors.Count-1]==holder;}
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
        _cursorRequestors.RemoveAll(x=>!x);return _cursorRequestors.TrueForAll(x=>nonModal.Contains(x));
    }

    public static void RequestCursor(GameObject requestHolder, bool allowGameplay = false)
    {
        if (_cursorRequestors.Contains(requestHolder))
        {
            Debug.LogError($"MultiFPS.UI: Object {requestHolder.name} is already holding cursor request.");
            return;
        }

        _cursorRequestors.Add(requestHolder);if(allowGameplay)nonModal.Add(requestHolder);

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

        _cursorRequestors.Remove(requestHolder);nonModal.Remove(requestHolder);

        if (_cursorRequestors.Count == 0)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
