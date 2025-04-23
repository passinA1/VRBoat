using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// NPC划桨管理器，负责管理和整合不同同步机制，确保NPC之间的完美同步
/// 作为同步控制器和单独NPC控制器之间的桥梁
/// </summary>
public class NPCPaddleManager : MonoBehaviour
{
    [System.Serializable]
    public class NPCInfo
    {
        public string npcName = "NPC";
        public NPCPaddleController controller;
        public int syncGroupID = 0; // 同步组ID
    }

    [Header("NPC管理")]
    public List<NPCInfo> npcControllers = new List<NPCInfo>();

    [Header("全局同步控制")]
    public SynchronizedNPCPaddleController syncController; // 同步控制器引用
    public bool initializeOnStart = true; // 是否在开始时初始化
    public bool autoRegisterNPCs = true; // 是否自动注册所有NPC到同步控制器

    [Header("节奏设置")]
    public float basePaddleInterval = 0.7f; // 基础划桨间隔
    public bool startWithLeftPaddle = true; // 从左桨开始

    [Header("游戏整合")]
    public bool enableStageBehaviors = true; // 是否启用阶段行为变化

    // 私有变量
    private GameManager gameManager;
    private StageManager stageManager;
    private ScoreSystem scoreSystem;

    void Awake()
    {
        // 如果没有设置同步控制器，尝试查找
        if (syncController == null)
        {
            syncController = FindObjectOfType<SynchronizedNPCPaddleController>();
        }
    }

    void Start()
    {
        // 获取引用
        gameManager = FindObjectOfType<GameManager>();
        stageManager = FindObjectOfType<StageManager>();
        scoreSystem = FindObjectOfType<ScoreSystem>();

        // 初始化同步控制
        if (initializeOnStart)
        {
            InitializeSynchronization();
        }
    }

    void Update()
    {
        // 根据游戏阶段调整NPC行为
        if (enableStageBehaviors && (gameManager != null || stageManager != null))
        {
            UpdateNPCsForCurrentStage();
        }
    }

    /// <summary>
    /// 初始化同步系统，设置所有NPC和同步控制器
    /// </summary>
    public void InitializeSynchronization()
    {
        // 确保有同步控制器
        if (syncController == null)
        {
            Debug.LogError("未找到同步控制器！请确保场景中有SynchronizedNPCPaddleController组件");
            return;
        }

        // 如果需要自动注册，将所有NPC的桨添加到同步控制器
        if (autoRegisterNPCs)
        {
            RegisterAllNPCsToSyncController();
        }

        // 设置同步控制器的参数
        syncController.paddleInterval = basePaddleInterval;
        syncController.startWithLeftPaddle = startWithLeftPaddle;

        // 确保所有NPC控制器使用全局同步
        foreach (var npcInfo in npcControllers)
        {
            if (npcInfo.controller != null)
            {
                npcInfo.controller.SetUseSynchronizedController(true);
                npcInfo.controller.syncGroupID = npcInfo.syncGroupID;
            }
        }

        // 启动同步控制器
        syncController.StartPaddling();
    }

    /// <summary>
    /// 将所有NPC的桨注册到同步控制器
    /// </summary>
    private void RegisterAllNPCsToSyncController()
    {
        if (syncController == null)
            return;

        // 清空同步控制器中的桨列表
        syncController.npcLeftPaddles.Clear();
        syncController.npcRightPaddles.Clear();

        // 添加所有NPC的桨
        foreach (var npcInfo in npcControllers)
        {
            if (npcInfo.controller != null)
            {
                // 添加左桨
                if (npcInfo.controller.leftPaddle != null)
                {
                    syncController.npcLeftPaddles.Add(npcInfo.controller.leftPaddle);
                }

                // 添加右桨
                if (npcInfo.controller.rightPaddle != null)
                {
                    syncController.npcRightPaddles.Add(npcInfo.controller.rightPaddle);
                }
            }
        }

        // 确认添加结果
        Debug.Log($"已将 {syncController.npcLeftPaddles.Count} 个左桨和 {syncController.npcRightPaddles.Count} 个右桨注册到同步控制器");
    }

    /// <summary>
    /// 根据当前游戏阶段更新NPC行为
    /// </summary>
    private void UpdateNPCsForCurrentStage()
    {
        int currentStage = 1;

        // 获取当前阶段
        if (gameManager != null)
            currentStage = gameManager.currentStage;
        else if (stageManager != null)
            currentStage = stageManager.currentStage;

        // 根据阶段调整划桨间隔
        float newInterval = basePaddleInterval;

        switch (currentStage)
        {
            case 1: // 基本操作
                newInterval = 0.8f; // 较慢节奏
                break;

            case 2: // 合作
                newInterval = 0.7f; // 中等节奏
                break;

            case 3: // 特殊机制
                newInterval = 0.6f; // 较快节奏
                break;

            case 4: // 正式尝试
                newInterval = 0.5f; // 快速节奏
                break;
        }

        // 如果间隔有变化，更新同步控制器
        if (syncController != null && Mathf.Abs(syncController.paddleInterval - newInterval) > 0.01f)
        {
            syncController.SetPaddleInterval(newInterval);
            basePaddleInterval = newInterval;
        }
    }

    /// <summary>
    /// 设置所有NPC是否激活
    /// </summary>
    public void SetAllNPCsActive(bool active)
    {
        foreach (var npcInfo in npcControllers)
        {
            if (npcInfo.controller != null)
            {
                npcInfo.controller.SetNPCActive(active);
            }
        }
    }

    /// <summary>
    /// 设置基础划桨间隔
    /// </summary>
    public void SetBasePaddleInterval(float interval)
    {
        basePaddleInterval = Mathf.Max(0.2f, interval);

        if (syncController != null)
        {
            syncController.SetPaddleInterval(basePaddleInterval);
        }
    }

    /// <summary>
    /// 切换起始桨
    /// </summary>
    public void SetStartWithLeftPaddle(bool startLeft)
    {
        startWithLeftPaddle = startLeft;

        if (syncController != null)
        {
            syncController.SetStartWithLeftPaddle(startLeft);
        }
    }

    /// <summary>
    /// 重置所有NPC的桨位置
    /// </summary>
    public void ResetAllPaddles()
    {
        // 通过同步控制器重置
        if (syncController != null)
        {
            syncController.ResetAllPaddles();
        }
        // 或直接重置每个NPC
        else
        {
            foreach (var npcInfo in npcControllers)
            {
                if (npcInfo.controller != null)
                {
                    npcInfo.controller.ResetPaddles();
                }
            }
        }
    }

    /// <summary>
    /// 重新启动划桨
    /// </summary>
    public void RestartPaddling()
    {
        // 停止现有划桨
        if (syncController != null)
        {
            syncController.StopPaddling();
        }

        // 重置所有桨位置
        ResetAllPaddles();

        // 重新初始化同步
        InitializeSynchronization();
    }

    /// <summary>
    /// 添加一个NPC到管理器
    /// </summary>
    public void AddNPC(NPCPaddleController controller, string name = "", int syncGroupID = 0)
    {
        if (controller == null)
            return;

        // 创建新的NPC信息
        NPCInfo newInfo = new NPCInfo();
        newInfo.npcName = string.IsNullOrEmpty(name) ? "NPC" + npcControllers.Count : name;
        newInfo.controller = controller;
        newInfo.syncGroupID = syncGroupID;

        // 添加到列表
        npcControllers.Add(newInfo);

        // 如果已经初始化了，立即注册
        if (syncController != null && autoRegisterNPCs)
        {
            // 添加这个NPC的桨到同步控制器
            if (controller.leftPaddle != null)
                syncController.npcLeftPaddles.Add(controller.leftPaddle);

            if (controller.rightPaddle != null)
                syncController.npcRightPaddles.Add(controller.rightPaddle);

            // 设置使用同步控制器
            controller.SetUseSynchronizedController(true);
            controller.syncGroupID = syncGroupID;
        }
    }

    void OnDisable()
    {
        if (syncController != null)
        {
            syncController.StopPaddling();
        }
    }

    void OnDestroy()
    {
        if (syncController != null)
        {
            syncController.StopPaddling();
        }
    }
}