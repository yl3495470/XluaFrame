﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using XLua;
using System;
using System.IO;
using Tangzx.ABSystem;

public class BattleManger : Singleton<BattleManger>
{
    private Action _luaStart;
    private Action _luaUpdate;
    private Action _luaOnDestroy;

    internal static LuaEnv luaEnv = new LuaEnv(); //all lua behaviour shared one luaenv only!
    internal static float lastGCTime = 0;
    internal const float GCInterval = 1;//1 second 

    private LuaTable scriptEnv;

    AssetBundleManager manager;


    public void Init()
    {
        InitLua();
        Debug.Log("Create Battle");
    }

    void Awake()
    {

    }

    void InitLua()
    {
        Debug.Log("Time1:" + Time.time);
        //异步加载，会有时间间隔，但是可以继续执行
        AssetBundleLoader loader = ResourceManger.Instance.AbManager.Load("Assets.XLua.MyLua.Resources.GameManger.lua.txt", (a) =>
        {
            Debug.Log("Time2:" + Time.time);

            scriptEnv = luaEnv.NewTable();

            // 为每个脚本设置一个独立的环境，可一定程度上防止脚本间全局变量、函数冲突
            LuaTable meta = luaEnv.NewTable();
            meta.Set("__index", luaEnv.Global);
            scriptEnv.SetMetaTable(meta);
            meta.Dispose();

            scriptEnv.Set("self", this);

            TextAsset textAsset = Instantiate(a.mainObject) as TextAsset;//a.Instantiate();
            luaEnv.DoString(textAsset.text, "ABS", scriptEnv);

            Action luaAwake = scriptEnv.Get<Action>("awake");
            scriptEnv.Get("start", out _luaStart);
            scriptEnv.Get("update", out _luaUpdate);
            scriptEnv.Get("ondestroy", out _luaOnDestroy);

            if (luaAwake != null)
            {
                luaAwake();
            }
        });
    }

    void Start()
    {
        Debug.Log("Start once");
        if (_luaStart != null)
            _luaStart();
    }

    void Update()
    {
        if (_luaUpdate != null)
        {
            _luaUpdate();
        }
        if (Time.time - BattleManger.lastGCTime > GCInterval)
        {
            luaEnv.Tick();
            BattleManger.lastGCTime = Time.time;
        }
    }

    void OnDestroy()
    {
        if (_luaOnDestroy != null)
        {
            _luaOnDestroy();
        }
        _luaOnDestroy = null;
        _luaUpdate = null;
        _luaStart = null;
        scriptEnv.Dispose();
    }
}
