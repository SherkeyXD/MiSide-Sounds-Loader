using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace MiSideSoundsLoader;

public class AudioFactory
{
    private static AudioFactory _instance;
    public static AudioFactory Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new AudioFactory();
            }
            return _instance;
        }
    }

    private Dictionary<string, string> clipNameToPathDict = [];
    private Dictionary<string, AudioClip> loadedAudioClipCache = [];

    public void InitializeClips(string folderPath)
    {
        string[] files = Directory.GetFiles(folderPath);
        foreach (string file in files)
        {
            AddClipPath(file);
        }
    }

    public void AddClipPath(string filepath)
    {
        string name = Path.GetFileNameWithoutExtension(filepath);
        string format = Path.GetExtension(filepath).Substring(1);
        clipNameToPathDict[name] = filepath;
        Plugin.Log.LogInfo($"Ready to replace clip: {name}.{format}");
    }

    public AudioClip LoadClip(string filepath)
    {
        string name = Path.GetFileNameWithoutExtension(filepath);
        AudioType type = GetAudioTypeFromExtension(
            Path.GetExtension(filepath).Substring(1).ToLower()
        );

        UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(filepath, type);
        try
        {
            www.SendWebRequest();

            while (!www.isDone) { } // Wait

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                clip.name = name;
                return clip;
            }
            else
            {
                Plugin.Log.LogWarning($"Failed to load clip: {www.error}");
            }
        }
        catch (Exception err)
        {
            Plugin.Log.LogError(
                $"Caught error while loading clip: {err.Message}, {err.StackTrace}"
            );
        }
        finally
        {
            www.Dispose();
        }

        return null;
    }

    public void UnloadClip(string name)
    {
        if (loadedAudioClipCache.TryGetValue(name, out AudioClip clip))
        {
            if (clip != null)
            {
                UnityEngine.Object.Destroy(clip);
                loadedAudioClipCache.Remove(name);
            }
        }
    }

    private AudioType GetAudioTypeFromExtension(string format)
    {
        switch (format)
        {
            case "ogg":
                return AudioType.OGGVORBIS;
            case "wav":
                return AudioType.WAV;
            case "aif":
            case "aiff":
                return AudioType.AIFF;
            case "acc":
                return AudioType.ACC;
            case "mp2":
            case "mp3":
                return AudioType.MPEG;
            default:
                return AudioType.UNKNOWN;
        }
    }

    public AudioClip GetClip(string name)
    {
        if (loadedAudioClipCache.TryGetValue(name, out AudioClip clip))
        {
            if (clip != null)
                return clip;
        }

        if (clipNameToPathDict.TryGetValue(name, out string filepath))
        {
            AudioClip NewClip = LoadClip(filepath);
            if (NewClip != null)
            {
                loadedAudioClipCache[name] = NewClip;
                return NewClip;
            }
            else
            {
                loadedAudioClipCache[name] = null;
                Plugin.Log.LogWarning($"Failed to load clip: {name}, adding to ignore list.");
            }
        }
        return null;
    }

    public void CheckClips()
    {
        foreach (var pair in clipNameToPathDict)
        {
            if (pair.Value == null)
            {
                Plugin.Log.LogWarning($"Clip \"{pair.Key}\" is null.");
            }
        }
    }

    public List<string> GetAllClipNames()
    {
        return new List<string>(clipNameToPathDict.Keys);
    }
}
