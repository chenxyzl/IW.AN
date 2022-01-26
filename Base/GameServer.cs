﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.Client;
using Akka.Configuration;
using Base.Helper;
using Common;
using Message;

namespace Base;

[Server]
public abstract class GameServer
{
    //
    public static GameServer Instance;

    //日志
    public readonly ILog Logger;

    //退出标记
    private bool _quitFlag;

    //配置
    protected Akka.Configuration.Config _systemConfig;
    private long lastTime;

    //
    public GameServer(RoleType r)
    {
        role = r;
        Logger = new NLogAdapter(role.ToString());
    }

    //根系统
    public ActorSystem system { get; protected set; }

    //角色类型
    public RoleType role { get; }

    //退出标记监听
    protected virtual void WatchQuit()
    {
        Console.CancelKeyPress += (sender, e) =>
        {
            if (_quitFlag) return;
            _quitFlag = true;
            e.Cancel = true;
        };
    }

    protected virtual void LoadConfig()
    {
        var baseConfig = File.ReadAllText("../Conf/Base.conf");
        var config = File.ReadAllText($"../Conf/{role}.conf");
        // var o = ConfigurationFactory.Default();
        var a = ConfigurationFactory.ParseString(baseConfig);
        var b = ConfigurationFactory.ParseString(config);
        _systemConfig = b.WithFallback(a);
        //_systemConfig  = File.ReadAllText($"../Conf/{role}.conf");
    }

    protected virtual async Task BeforCreate()
    {
        //拦截退出
        WatchQuit();
        //加载配置
        LoadConfig();
        //注册组建
        RegisterGlobalComponent();
        //全局触发load
        foreach (var x in _componentsList) await x.Load();
    }

    protected virtual async Task AfterCreate()
    {
        //全局触发AfterLoad
        foreach (var x in _componentsList) await x.Start();
        //触发挤时间
        Instance.lastTime = TimeHelper.Now();
    }


    protected virtual async Task Tick()
    {
        //全局触发PreStop
        foreach (var x in _componentsList) await x.Tick();
    }

    protected virtual async Task PreStop()
    {
        //全局触发PreStop
        foreach (var x in _componentsList) await x.PreStop();
    }

    protected virtual async Task Stop()
    {
        //全局触发PreStop
        foreach (var x in _componentsList) await x.Stop();
    }

    protected virtual async Task StartSystem(string typeName, Props p, HashCodeMessageExtractor extractor)
    {
        await BeforCreate();
        system = ActorSystem.Create(GlobalParam.SystemName, _systemConfig);
        var sharding = ClusterSharding.Get(system);
        var shardRegion = await sharding.StartAsync(
            typeName,
            p,
            ClusterShardingSettings.Create(system),
            extractor
        );
        ClusterClientReceptionist.Get(system).RegisterService(shardRegion);
        await AfterCreate();
    }

    protected virtual async Task StartSystem()
    {
        await BeforCreate();
        system = ActorSystem.Create(GlobalParam.SystemName, _systemConfig);
        await AfterCreate();
    }

    protected virtual async Task StopSystem()
    {
        GlobalLog.Warning($"---{role}停止中,请勿强关---");
        await PreStop();
        await system.Terminate();
        // ClusterClientReceptionist.Get(system).UnregisterService(Self);
        await Stop();
        GlobalLog.Warning($"---{role}停止完成---");
    }

    public IActorRef GetChild(string path)
    {
        var a = system.ActorSelection(path);
        if (a == null) A.Abort(Code.Error, $"local system get child path:{path} not found");

        return a.Anchor;
    }

    //加载程序集合
    protected virtual void Reload()
    {
        GlobalLog.Warning($"---{role}加载中---");
        HotfixManager.Instance.Reload();
        GlobalLog.Warning($"---{role}加载完成---");
    }

    protected virtual void Loop()
    {
        GlobalLog.Warning($"---{role}开启loop---");
        while (!_quitFlag)
        {
            Thread.Sleep(1);
            var now = TimeHelper.Now();
            //1000毫秒tick一次
            if (now - lastTime < 1000) continue;
            lastTime += 1000;
            _ = Tick();
        }

        GlobalLog.Warning($"---{role}退出loop---");
    }

    private static void BeforeRun()
    {
        //支持gbk2132
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    //有actor的启动
    public static async Task Run(Type gsType, string typeName, Props p, HashCodeMessageExtractor extractor)
    {
        //before
        BeforeRun();
        //创建
        Instance = Activator.CreateInstance(gsType) as GameServer;
        //准备
        Instance.Reload();
        //开始游戏
        await Instance.StartSystem(typeName, p, extractor);
        //开启无限循环
        Instance.Loop();
        //结束游戏
        await Instance.StopSystem();
    }

    //无actor的启动
    public static async Task Run(Type gsType)
    {
        //before；
        BeforeRun();
        //创建
        Instance = Activator.CreateInstance(gsType) as GameServer;
        //准备
        Instance.Reload();
        //开始游戏
        await Instance.StartSystem();
        //开启无限循环
        Instance.Loop();
        //结束游戏
        await Instance.StopSystem();
    }


    /// <summary>
    ///     注册全局组件
    /// </summary>
    public abstract void RegisterGlobalComponent();

    #region 全局组件

    //所有model
    protected Dictionary<Type, IGlobalComponent> _components = new();

    protected List<IGlobalComponent> _componentsList = new();

    //获取model
    public K GetComponent<K>() where K : class, IGlobalComponent
    {
        if (!_components.TryGetValue(typeof(K), out var component))
        {
            A.Abort(Code.Error, $"game component:{typeof(K).Name} not found");
            ;
        }

        return (K) component;
    }

    protected void AddComponent<K>(params object[] args) where K : class, IGlobalComponent
    {
        var t = typeof(K);
        if (_components.TryGetValue(t, out var _)) A.Abort(Code.Error, $"game component:{t.Name} repeated");

        var obj = Activator.CreateInstance(t, args) as K;
        _components.Add(t, obj);
        _componentsList.Add(obj);
    }

    #endregion


    #region 开启各种proxy

    protected virtual void StartPlayerShardProxy()
    {
        ClusterSharding.Get(system).StartProxy(GameSharedRole.Player.ToString(), role.ToString(),
            MessageExtractor.PlayerMessageExtractor);
    }

    protected virtual void StartWorldShardProxy()
    {
        ClusterSharding.Get(system).StartProxy(GameSharedRole.World.ToString(), role.ToString(),
            MessageExtractor.WorldMessageExtractor);
    }

    #endregion
}