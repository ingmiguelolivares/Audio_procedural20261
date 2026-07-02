using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public static class IOSFileSharingPostProcess
{
    private const string FileSharingKey = "UIFileSharingEnabled";
    private const string OpenDocumentsInPlaceKey = "LSSupportsOpeningDocumentsInPlace";

    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
    {
        if (buildTarget != BuildTarget.iOS)
            return;

        string plistPath = Path.Combine(buildPath, "Info.plist");

        if (!File.Exists(plistPath))
        {
            Debug.LogError("[IOSFileSharingPostProcess] Info.plist not found at path: " + plistPath);
            return;
        }

        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        PlistElementDict root = plist.root;
        root.SetBoolean(FileSharingKey, true);
        root.SetBoolean(OpenDocumentsInPlaceKey, true);

        File.WriteAllText(plistPath, plist.WriteToString());

        Debug.Log("[IOSFileSharingPostProcess] iOS file sharing enabled. Set " +
                  FileSharingKey + " and " + OpenDocumentsInPlaceKey + " to true in Info.plist.");
    }
}
