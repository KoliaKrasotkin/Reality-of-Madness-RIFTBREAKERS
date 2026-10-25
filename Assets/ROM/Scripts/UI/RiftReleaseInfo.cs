using System;
using System.Globalization;
using UnityEngine;
namespace ROM.UI.Sprint01 {
[CreateAssetMenu(menuName="Rift/Release/Version",fileName="RiftReleaseInfo")]
public sealed class RiftReleaseInfo:ScriptableObject {
 [Tooltip("Visible release name; keep FaF abbreviated.")]public string VersionName="0.1 Alpha (FaF)";
 [Tooltip("Fixed closing identifier HHmmDDMMAAAA in Argentina UTC-03. Edit only when a patch closes; never a runtime clock.")]public string PatchId="183124102026";
 [Tooltip("Audit data: closing time of the source checkpoint, not the upload time.")]public string ClosedAtArgentina="2026-10-24T18:31:04-03:00";
 public string SourceCheckpoint="f0f8ccb6150940d66c3c1e4a8d74219da6644698";
 [Tooltip("Clear only when the local corrections are included in a verified published checkpoint.")]public bool LocalChangesUnpublished=true;
 public bool HasValidPatchId=>PatchId!=null&&PatchId.Length==12&&DateTime.TryParseExact(PatchId,"HHmmddMMyyyy",CultureInfo.InvariantCulture,DateTimeStyles.None,out _);
 public string DisplayLabel=>VersionName+" · Patch "+(HasValidPatchId?PatchId:"sin cierre");
 public string StatusLabel=>LocalChangesUnpublished?"Cambios locales sin publicar":"";
 static RiftReleaseInfo fallback;
 public static RiftReleaseInfo Load{get{var asset=Resources.Load<RiftReleaseInfo>("RiftReleaseInfo");if(asset)return asset;if(!fallback){fallback=CreateInstance<RiftReleaseInfo>();fallback.hideFlags=HideFlags.HideAndDontSave;}return fallback;}}
}
}
