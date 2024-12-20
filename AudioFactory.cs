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

    private Dictionary<string, string> replacedClips = [];

    public void LoadAllClips(string folderPath)
    {
        string[] files = Directory.GetFiles(folderPath);
        foreach (string file in files)
        {
            AddClip(file);
        }
    }

    public void AddClip(string filepath)
    {
        string name = Path.GetFileNameWithoutExtension(filepath);
        string format = Path.GetExtension(filepath).Substring(1);
        replacedClips[name] = filepath;
        Plugin.Log.LogInfo($"Ready to replace clip: {name}.{format}");
    }

    public AudioClip LoadClip(string filepath)
    {
        string name = Path.GetFileNameWithoutExtension(filepath);
        string format = Path.GetExtension(filepath).Substring(1).ToLower();
        AudioType type = AudioType.UNKNOWN;
        switch (format)
        {
            case "ogg":
                type = AudioType.OGGVORBIS;
                break;
            case "wav":
                type = AudioType.WAV;
                break;
            case "aif":
            case "aiff":
                type = AudioType.AIFF;
                break;
            case "acc":
                type = AudioType.ACC;
                break;
            case "mp2":
            case "mp3":
                type = AudioType.MPEG;
                break;
        }

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

    public AudioClip GetClip(string name)
    {
        if (replacedClips.TryGetValue(name, out string filepath))
        {
            AudioClip clip = LoadClip(filepath);
            if (clip != null)
            {
                return clip;
            }
        }
        return null;
    }

    public void CheckClips()
    {
        foreach (var pair in replacedClips)
        {
            if (pair.Value == null)
            {
                Plugin.Log.LogWarning($"Clip \"{pair.Key}\" is null.");
            }
        }
    }

    public List<string> GetAllClipNames()
    {
        return new List<string>(replacedClips.Keys);
    }
}
