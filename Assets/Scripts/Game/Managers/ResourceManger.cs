﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using Tangzx.ABSystem;
using UnityEngine;
using UnityEngine.Networking;

public class ResourceManger : Singleton<ResourceManger>
{
    public AssetBundleManager AbManager;

    private const string urlParentPath = "ftp://154.8.204.224/AssetBundles/";    //"file:///D:/local/AssetBundles/";

    private string file_ParentSaveUrl;
    private Dictionary<string, Hash128> localHash = new Dictionary<string, Hash128>();
    private Queue<string> downLoadFiles = new Queue<string>();
    private int maxDownCount = 0;

    private FileInfo file;

    public void Init()
    {
        AbManager = gameObject.AddComponent<AssetBundleManager>();
        AbManager.Init(() =>
        {
            InitComplete();
        });
    }

    public static string GetSavePath()
    {
        string filePath = null;
#if UNITY_EDITOR
        filePath = string.Format("file://{0}/StreamingAssets/AssetBundles/", Application.dataPath);
#elif UNITY_STANDALONE_WIN
        filePath = string.Format("file://{0}/StreamingAssets/AssetBundles/", Application.dataPath);
#elif UNITY_ANDROID
        filePath = string.Format("jar:file://{0}!/assets/AssetBundles/", Application.dataPath);
#elif UNITY_IOS
        filePath = string.Format("file://{0}/Raw/AssetBundles/", Application.dataPath);
#else
        throw new System.NotImplementedException();
#endif
        return filePath;
    }

    public void InitComplete()
    {
        play();
    }

    public void play()
    {
        file_ParentSaveUrl = GetSavePath();

        AssetBundle ab = AssetBundle.LoadFromFile(file_ParentSaveUrl.Replace("file://", "") + "AssetBundles");
        AssetBundleManifest localMF = ab.LoadAsset("AssetBundleManifest") as AssetBundleManifest;
    
        string[] names = localMF.GetAllAssetBundles();

        for (int i = 0; i < names.Length; ++i)
        {
            localHash.Add(names[i], localMF.GetAssetBundleHash(names[i]));
        }

        ab.Unload(true);
        ab = null;

        StartCoroutine(DownManifest(urlParentPath + "AssetBundles"));
    }

    IEnumerator DownManifest(string url)
    {
        using (UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(url))
        {
            yield return request.SendWebRequest();

            if(request.isDone)
            {
                AssetBundle ab = (request.downloadHandler as DownloadHandlerAssetBundle).assetBundle;
                AssetBundleManifest manifest = ab.LoadAsset("AssetBundleManifest") as AssetBundleManifest;

                string[] names = manifest.GetAllAssetBundles();

                for (int i = 0; i< names.Length; ++i)
                {
                    if(!manifest.GetAssetBundleHash(names[i]).Equals(localHash[names[i]]))
                    {
                        Debug.Log("出现不同的文件");
                        Debug.Log("web:" + manifest.GetAssetBundleHash(names[i]) + ",local:" + localHash[names[i]]);
                        downLoadFiles.Enqueue(names[i]);
                    }
                }
                maxDownCount = downLoadFiles.Count;
                StartCoroutine(DownAssetBundle());
            }
        }
    }

    IEnumerator DownAssetBundle()
    {
        if(downLoadFiles.Count == 0)
        {
            StartCoroutine(UpdateMF());
            yield break;
        }

        string abName = downLoadFiles.Dequeue();

        string url = urlParentPath + abName;
        string localPath = file_ParentSaveUrl + abName;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            while (!request.isDone)
            {
                Debug.Log(maxDownCount - downLoadFiles.Count + "/" + maxDownCount + ":" + request.downloadProgress/ 100.0f);
            }
            CreatFile(localPath, request.downloadHandler.data);
            StartCoroutine(DownAssetBundle());
        }
    }

    IEnumerator UpdateMF()
    {
        string abName = "AssetBundles";

        string url = urlParentPath + abName;
        string localPath = file_ParentSaveUrl + abName;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            while (!request.isDone)
            {
                Debug.Log("更新资源目录:" + request.downloadProgress / 100.0f);
            }
            CreatFile(localPath, request.downloadHandler.data);
            BattleManger.Instance.Init();
        }
    }

    void CreatFile(string path, byte[] bytes)
    {
        path = path.Replace("file://", "");
        if (File.Exists(path))//判断一下本地是否有了该音频  如果有就不需下载
        {
            File.Delete(path);
        }

        FileInfo file = new FileInfo(path);
        Stream stream;
        stream = file.Create();
        stream.Write(bytes, 0, bytes.Length);
        stream.Close();
        stream.Dispose();
    }
}
