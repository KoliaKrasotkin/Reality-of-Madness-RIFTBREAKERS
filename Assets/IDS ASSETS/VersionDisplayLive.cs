using UnityEngine;
using UnityEngine.UI;

public class VersionDisplayLive : MonoBehaviour
{
    public Text versionText;

    void Start()
    {
        versionText.text = "TBFZ Version: " + Application.version;
    }
}
