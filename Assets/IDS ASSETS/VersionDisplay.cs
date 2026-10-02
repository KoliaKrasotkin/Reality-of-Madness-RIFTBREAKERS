using UnityEngine;
using UnityEngine.UI;

public class VersionDisplay : MonoBehaviour
{
    public Text versionText;

    void Start()
    {
        versionText.text = "YOU WORKING ON VERSION: " + Application.version;
    }
}
