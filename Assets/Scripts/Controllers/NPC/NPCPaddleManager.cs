using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// NPC�����������������������ϲ�ͬͬ�����ƣ�ȷ��NPC֮�������ͬ��
/// ��Ϊͬ���������͵���NPC������֮�������
/// </summary>
public class NPCPaddleManager : MonoBehaviour
{
    [System.Serializable]
    public class NPCInfo
    {
        public string npcName = "NPC";
        public NPCPaddleController controller;
        public int syncGroupID = 0; // ͬ����ID
    }

    [Header("NPC����")]
    public List<NPCInfo> npcControllers = new List<NPCInfo>();

    [Header("ȫ��ͬ������")]
    public SynchronizedNPCPaddleController syncController; // ͬ������������
    public bool initializeOnStart = true; // �Ƿ��ڿ�ʼʱ��ʼ��
    public bool autoRegisterNPCs = true; // �Ƿ��Զ�ע������NPC��ͬ��������

    [Header("��������")]
    public float basePaddleInterval = 0.7f; // �����������
    public bool startWithLeftPaddle = true; // ���󽰿�ʼ

    [Header("��Ϸ����")]
    public bool enableStageBehaviors = true; // �Ƿ����ý׶���Ϊ�仯

    // ˽�б���
    private GameManager gameManager;
    private StageManager stageManager;
    private ScoreSystem scoreSystem;

    void Awake()
    {
        // ���û������ͬ�������������Բ���
        if (syncController == null)
        {
            syncController = FindObjectOfType<SynchronizedNPCPaddleController>();
        }
    }

    void Start()
    {
        // ��ȡ����
        gameManager = FindObjectOfType<GameManager>();
        stageManager = FindObjectOfType<StageManager>();
        scoreSystem = FindObjectOfType<ScoreSystem>();

        // ��ʼ��ͬ������
        if (initializeOnStart)
        {
            InitializeSynchronization();
        }
    }

    void Update()
    {
        // ������Ϸ�׶ε���NPC��Ϊ
        if (enableStageBehaviors && (gameManager != null || stageManager != null))
        {
            UpdateNPCsForCurrentStage();
        }
    }

    /// <summary>
    /// ��ʼ��ͬ��ϵͳ����������NPC��ͬ��������
    /// </summary>
    public void InitializeSynchronization()
    {
        // ȷ����ͬ��������
        if (syncController == null)
        {
            Debug.LogError("δ�ҵ�ͬ������������ȷ����������SynchronizedNPCPaddleController���");
            return;
        }

        // �����Ҫ�Զ�ע�ᣬ������NPC�Ľ���ӵ�ͬ��������
        if (autoRegisterNPCs)
        {
            RegisterAllNPCsToSyncController();
        }

        // ����ͬ���������Ĳ���
        syncController.paddleInterval = basePaddleInterval;
        syncController.startWithLeftPaddle = startWithLeftPaddle;

        // ȷ������NPC������ʹ��ȫ��ͬ��
        foreach (var npcInfo in npcControllers)
        {
            if (npcInfo.controller != null)
            {
                npcInfo.controller.SetUseSynchronizedController(true);
                npcInfo.controller.syncGroupID = npcInfo.syncGroupID;
            }
        }

        // ����ͬ��������
        syncController.StartPaddling();
    }

    /// <summary>
    /// ������NPC�Ľ�ע�ᵽͬ��������
    /// </summary>
    private void RegisterAllNPCsToSyncController()
    {
        if (syncController == null)
            return;

        // ���ͬ���������еĽ��б�
        syncController.npcLeftPaddles.Clear();
        syncController.npcRightPaddles.Clear();

        // �������NPC�Ľ�
        foreach (var npcInfo in npcControllers)
        {
            if (npcInfo.controller != null)
            {
                // �����
                if (npcInfo.controller.leftPaddle != null)
                {
                    syncController.npcLeftPaddles.Add(npcInfo.controller.leftPaddle);
                }

                // ����ҽ�
                if (npcInfo.controller.rightPaddle != null)
                {
                    syncController.npcRightPaddles.Add(npcInfo.controller.rightPaddle);
                }
            }
        }

        // ȷ����ӽ��
        Debug.Log($"�ѽ� {syncController.npcLeftPaddles.Count} ���󽰺� {syncController.npcRightPaddles.Count} ���ҽ�ע�ᵽͬ��������");
    }

    /// <summary>
    /// ���ݵ�ǰ��Ϸ�׶θ���NPC��Ϊ
    /// </summary>
    private void UpdateNPCsForCurrentStage()
    {
        int currentStage = 1;

        // ��ȡ��ǰ�׶�
        if (gameManager != null)
            currentStage = gameManager.currentStage;
        else if (stageManager != null)
            currentStage = stageManager.currentStage;

        // ���ݽ׶ε����������
        float newInterval = basePaddleInterval;

        switch (currentStage)
        {
            case 1: // ��������
                newInterval = 0.8f; // ��������
                break;

            case 2: // ����
                newInterval = 0.7f; // �еȽ���
                break;

            case 3: // �������
                newInterval = 0.6f; // �Ͽ����
                break;

            case 4: // ��ʽ����
                newInterval = 0.5f; // ���ٽ���
                break;
        }

        // �������б仯������ͬ��������
        if (syncController != null && Mathf.Abs(syncController.paddleInterval - newInterval) > 0.01f)
        {
            syncController.SetPaddleInterval(newInterval);
            basePaddleInterval = newInterval;
        }
    }

    /// <summary>
    /// ��������NPC�Ƿ񼤻�
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
    /// ���û����������
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
    /// �л���ʼ��
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
    /// ��������NPC�Ľ�λ��
    /// </summary>
    public void ResetAllPaddles()
    {
        // ͨ��ͬ������������
        if (syncController != null)
        {
            syncController.ResetAllPaddles();
        }
        // ��ֱ������ÿ��NPC
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
    /// ������������
    /// </summary>
    public void RestartPaddling()
    {
        // ֹͣ���л���
        if (syncController != null)
        {
            syncController.StopPaddling();
        }

        // �������н�λ��
        ResetAllPaddles();

        // ���³�ʼ��ͬ��
        InitializeSynchronization();
    }

    /// <summary>
    /// ���һ��NPC��������
    /// </summary>
    public void AddNPC(NPCPaddleController controller, string name = "", int syncGroupID = 0)
    {
        if (controller == null)
            return;

        // �����µ�NPC��Ϣ
        NPCInfo newInfo = new NPCInfo();
        newInfo.npcName = string.IsNullOrEmpty(name) ? "NPC" + npcControllers.Count : name;
        newInfo.controller = controller;
        newInfo.syncGroupID = syncGroupID;

        // ��ӵ��б�
        npcControllers.Add(newInfo);

        // ����Ѿ���ʼ���ˣ�����ע��
        if (syncController != null && autoRegisterNPCs)
        {
            // ������NPC�Ľ���ͬ��������
            if (controller.leftPaddle != null)
                syncController.npcLeftPaddles.Add(controller.leftPaddle);

            if (controller.rightPaddle != null)
                syncController.npcRightPaddles.Add(controller.rightPaddle);

            // ����ʹ��ͬ��������
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