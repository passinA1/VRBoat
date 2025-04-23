using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// 阶段管理器，专门负责管理游戏关卡中的不同阶段过渡与逻辑
/// 配合GameManager使用，减轻GameManager的负担
/// </summary>
public class StageManager : MonoBehaviour
{
    [System.Serializable]
    public class StageConfig
    {
        public string stageName;
        public string description;
        public GameObject instructionUI;
        public bool requireAlternating;
        public bool allowSyncPaddle;
        public float stageDuration = 60f;
        public int targetCombo = 0;
        public int targetScore = 0;
    }

    [Header("阶段配置")]
    public StageConfig[] stages;  // 阶段配置数组
    public int currentStage = 1;  // 当前阶段，从1开始
    public bool autoProgressStages = true; // 是否自动进入下一阶段

    [Header("过渡设置")]
    public GameObject transitionScreen;
    public float transitionDuration = 1.0f;
    public AudioClip transitionSound;
    public TextMeshProUGUI stageAnnouncementText;

    [Header("UI设置")]
    public TextMeshProUGUI currentStageText;
    public TextMeshProUGUI stageDescriptionText;
    public Slider stageProgressBar;

    [Header("调试设置")]
    public bool debugMode = false;

    // 私有变量
    private float stageStartTime;
    private bool isTransitioning = false;
    private Coroutine stageTimerCoroutine;
    private GameManager gameManager;
    private ScoreSystem scoreSystem;
    private AudioSource audioSource;
    private Animator transitionAnimator;
    private int stageObjective = 0; // 0=时间, 1=连击, 2=分数

    // 引用
    private PaddleController paddleController;
    private UIManager uiManager;

    void Start()
    {
        // 获取引用
        gameManager = FindObjectOfType<GameManager>();
        scoreSystem = FindObjectOfType<ScoreSystem>();
        audioSource = GetComponent<AudioSource>();
        paddleController = FindObjectOfType<PaddleController>();
        uiManager = FindObjectOfType<UIManager>();

        if (transitionScreen != null)
        {
            transitionAnimator = transitionScreen.GetComponent<Animator>();
            transitionScreen.SetActive(false);
        }

        // 确保阶段数组长度有效
        if (stages == null || stages.Length == 0)
        {
            Debug.LogError("StageManager: 阶段配置为空!");
            return;
        }

        // 确保currentStage在有效范围内
        currentStage = Mathf.Clamp(currentStage, 1, stages.Length);

        // 初始化第一个阶段
        SetupCurrentStage();

        // 如果启用自动进度，开始计时器
        if (autoProgressStages)
        {
            stageStartTime = Time.time;
            stageTimerCoroutine = StartCoroutine(StageProgressionTimer());
        }

        if (debugMode)
            Debug.Log($"StageManager初始化完成，当前阶段: {currentStage} - {GetCurrentStageName()}");
    }

    void Update()
    {
        // 更新UI
        UpdateUI();

        // 检查目标是否达成（如果不是基于时间）
        if (!isTransitioning && autoProgressStages && stageObjective != 0)
        {
            CheckObjectiveCompletion();
        }
    }

    // 设置当前阶段
    public void SetupCurrentStage()
    {
        int index = currentStage - 1;
        if (index < 0 || index >= stages.Length) return;

        StageConfig stage = stages[index];

        // 隐藏所有阶段说明
        foreach (StageConfig s in stages)
        {
            if (s.instructionUI != null)
                s.instructionUI.SetActive(false);
        }

        // 显示当前阶段说明
        if (stage.instructionUI != null)
            stage.instructionUI.SetActive(true);

        // 配置得分系统
        if (scoreSystem != null)
        {
            scoreSystem.SetRequireAlternating(stage.requireAlternating);
        }

        // 根据第一关文档设置每个阶段的特性
        if (currentStage == 1)
        {
            // 第一阶段: 基本操作
            // 5个左手桨, 5个右手桨
            if (scoreSystem != null)
            {
                scoreSystem.SetRequireAlternating(false);
            }
        }
        else if (currentStage == 2)
        {
            // 第二阶段: 合作
            // 同时使用双手划桨5次
            if (paddleController != null)
            {
                // 可以在这里添加特殊设置
            }
        }
        else if (currentStage == 3)
        {
            // 第三阶段: 特殊机制
            // 连续10次完美命中，激活火焰特效
            if (scoreSystem != null)
            {
                scoreSystem.SetRequireAlternating(true); // 要求左右交替
            }

            // 显示旗帜判定点
            if (uiManager != null)
            {
                uiManager.SetFlagMarkersVisible(true);
            }
        }
        else if (currentStage == 4)
        {
            // 第四阶段: 正式尝试
            // 随机切换鼓点
            if (scoreSystem != null)
            {
                scoreSystem.SetRequireAlternating(true);
            }
        }

        // 更新UI
        if (currentStageText != null)
            currentStageText.text = $"阶段 {currentStage}: {stage.stageName}";

        if (stageDescriptionText != null)
            stageDescriptionText.text = stage.description;

        // 确定阶段目标类型
        if (stage.targetCombo > 0)
            stageObjective = 1; // 连击
        else if (stage.targetScore > 0)
            stageObjective = 2; // 分数
        else
            stageObjective = 0; // 时间

        // 重置阶段开始时间
        stageStartTime = Time.time;

        // 通知GameManager
        if (gameManager != null)
        {
            gameManager.currentStage = currentStage;
        }

        if (debugMode)
            Debug.Log($"设置阶段 {currentStage}: {stage.stageName}");
    }

    // 更新UI
    private void UpdateUI()
    {
        if (stageProgressBar != null && !isTransitioning)
        {
            int index = currentStage - 1;
            if (index < 0 || index >= stages.Length) return;

            StageConfig stage = stages[index];

            switch (stageObjective)
            {
                case 0: // 时间
                    float elapsed = Time.time - stageStartTime;
                    float progress = elapsed / stage.stageDuration;
                    stageProgressBar.value = Mathf.Clamp01(progress);
                    break;

                case 1: // 连击
                    if (scoreSystem != null)
                    {
                        float comboProgress = (float)scoreSystem.GetHighestCombo() / stage.targetCombo;
                        stageProgressBar.value = Mathf.Clamp01(comboProgress);
                    }
                    break;

                case 2: // 分数
                    if (scoreSystem != null)
                    {
                        float scoreProgress = (float)scoreSystem.GetScore() / stage.targetScore;
                        stageProgressBar.value = Mathf.Clamp01(scoreProgress);
                    }
                    break;
            }
        }
    }

    // 检查目标是否完成
    private void CheckObjectiveCompletion()
    {
        int index = currentStage - 1;
        if (index < 0 || index >= stages.Length) return;

        StageConfig stage = stages[index];

        bool objectiveCompleted = false;

        switch (stageObjective)
        {
            case 1: // 连击
                if (scoreSystem != null && scoreSystem.GetHighestCombo() >= stage.targetCombo)
                    objectiveCompleted = true;
                break;

            case 2: // 分数
                if (scoreSystem != null && scoreSystem.GetScore() >= stage.targetScore)
                    objectiveCompleted = true;
                break;
        }

        if (objectiveCompleted)
        {
            AdvanceToNextStage();
        }
    }

    // 阶段进度计时器
    private IEnumerator StageProgressionTimer()
    {
        while (currentStage <= stages.Length && !isTransitioning)
        {
            // 只有当阶段目标是基于时间时才检查时间
            if (stageObjective == 0)
            {
                int index = currentStage - 1;
                if (index >= 0 && index < stages.Length)
                {
                    // 检查是否应该进入下一阶段
                    if (Time.time - stageStartTime >= stages[index].stageDuration)
                    {
                        AdvanceToNextStage();
                        yield break; // 结束当前协程，新阶段会启动新的协程
                    }
                }
            }

            yield return null;
        }
    }

    // 进入下一阶段
    public void AdvanceToNextStage()
    {
        if (currentStage < stages.Length && !isTransitioning)
        {
            StartCoroutine(StageTransition(currentStage, currentStage + 1));
        }
        else if (currentStage >= stages.Length && !isTransitioning)
        {
            // 所有阶段完成，通知GameManager结束关卡
            if (gameManager != null && scoreSystem != null)
            {
                gameManager.OnLevelCompleted(scoreSystem.GetScore());
            }
        }
    }

    // 跳转到特定阶段
    public void JumpToStage(int stageNumber)
    {
        if (stageNumber < 1 || stageNumber > stages.Length || isTransitioning)
            return;

        if (stageNumber != currentStage)
        {
            StartCoroutine(StageTransition(currentStage, stageNumber));
        }
    }

    // 阶段过渡协程
    private IEnumerator StageTransition(int fromStage, int toStage)
    {
        isTransitioning = true;

        // 停止当前阶段计时器
        if (stageTimerCoroutine != null)
        {
            StopCoroutine(stageTimerCoroutine);
            stageTimerCoroutine = null;
        }

        // 显示过渡屏幕
        if (transitionScreen != null)
            transitionScreen.SetActive(true);

        // 显示阶段文本
        if (stageAnnouncementText != null)
        {
            int index = toStage - 1;
            if (index >= 0 && index < stages.Length)
            {
                stageAnnouncementText.text = $"阶段 {toStage}: {stages[index].stageName}";
            }
        }

        // 播放过渡动画
        if (transitionAnimator != null)
        {
            transitionAnimator.SetTrigger("StartTransition");
        }

        // 播放过渡音效
        if (audioSource != null && transitionSound != null)
        {
            audioSource.PlayOneShot(transitionSound);
        }

        // 等待过渡时间的一半
        yield return new WaitForSeconds(transitionDuration / 2);

        // 更新当前阶段
        currentStage = toStage;

        // 设置新阶段
        SetupCurrentStage();

        // 等待剩余过渡时间
        yield return new WaitForSeconds(transitionDuration / 2);

        // 隐藏过渡屏幕
        if (transitionScreen != null)
            transitionScreen.SetActive(false);

        // 如果启用自动进度，开始新的计时器
        if (autoProgressStages)
        {
            stageTimerCoroutine = StartCoroutine(StageProgressionTimer());
        }

        isTransitioning = false;

        if (debugMode)
            Debug.Log($"已切换到阶段 {currentStage}: {GetCurrentStageName()}");
    }

    // 获取当前阶段名称
    public string GetCurrentStageName()
    {
        int index = currentStage - 1;
        return (index >= 0 && index < stages.Length) ? stages[index].stageName : "未知";
    }

    // 获取当前阶段描述
    public string GetCurrentStageDescription()
    {
        int index = currentStage - 1;
        return (index >= 0 && index < stages.Length) ? stages[index].description : "未知阶段";
    }

    // 重置当前阶段
    public void RestartCurrentStage()
    {
        if (isTransitioning) return;

        // 停止当前阶段计时器
        if (stageTimerCoroutine != null)
        {
            StopCoroutine(stageTimerCoroutine);
            stageTimerCoroutine = null;
        }

        // 重新设置当前阶段
        SetupCurrentStage();

        // 如果启用自动进度，重新开始计时器
        if (autoProgressStages)
        {
            stageTimerCoroutine = StartCoroutine(StageProgressionTimer());
        }

        if (debugMode)
            Debug.Log($"重新启动阶段 {currentStage}: {GetCurrentStageName()}");
    }

    // 获取总阶段数
    public int GetTotalStages()
    {
        return stages.Length;
    }
}