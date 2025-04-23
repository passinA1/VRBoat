using UnityEngine;
using TMPro;
using System.Collections;

public class ScoreSystem : MonoBehaviour
{
    [Header("得分规则")]
    public int pointsPerPaddle = 10; // 每次划桨基础得分
    public int comboBonus = 2; // 每级连击奖励
    public int missPenalty = -5; // miss惩罚分数
    public int syncPaddleBonus = 25; // 双桨同步奖励分数

    [Header("节奏判定")]
    public float rhythmWindowTime = 0.5f; // 判定窗口(秒)
    public float perfectTiming = 0.1f; // 完美判定窗口(秒)
    public float goodTiming = 0.2f; // 良好判定窗口(秒)
                                    // 超过goodTiming则判定为miss

    [Header("连击设置")]
    public int comboThreshold = 10; // 触发"全力"状态的连击数
    public float comboMultiplier = 1.5f; // "全力"状态得分倍数
    public float fullPowerDuration = 5f; // "全力"状态持续时间

    [Header("左右交替设置")]
    public bool requireAlternating = false; // 是否要求左右交替划桨
    private bool lastPaddleWasLeft = false; // 上一次是否为左桨

    [Header("测试模式设置")]
    public bool debugMode = false; // 调试模式
    public KeyCode perfectHitKey = KeyCode.F; // 强制完美判定的按键

    [Header("UI引用")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI speedText;
    public GameObject fullPowerEffect;

    // 私有变量
    private int score = 0;
    public int comboCount { get; private set; } = 0; // 属性化以提供外部读取
    private float lastPaddleTime = 0f;
    private bool isFullPower = false;
    private Coroutine fullPowerCoroutine;
    private DragonBoatMovement boatMovement;
    private int highestCombo = 0; // 最高连击记录

    // 判定枚举
    public enum JudgmentResult { Perfect, Good, Miss }

    // 引用
    private FeedbackSystem feedbackSystem;
    private UIManager uiManager;

    void Start()
    {
        // 获取引用
        boatMovement = FindObjectOfType<DragonBoatMovement>();
        feedbackSystem = FindObjectOfType<FeedbackSystem>();
        uiManager = FindObjectOfType<UIManager>();

        // 初始化UI
        UpdateUI();

        // 订阅划桨事件
        PaddleController.OnPaddleAction += OnPaddleAction;

        // 订阅双桨同步事件
        PaddleController.OnSyncPaddle += OnSyncPaddle;

        // 初始化全力特效
        if (fullPowerEffect != null)
            fullPowerEffect.SetActive(false);

        if (uiManager != null && uiManager.fullPowerIndicator != null)
            uiManager.fullPowerIndicator.gameObject.SetActive(false);

        if (debugMode)
            Debug.Log("ScoreSystem已启动调试模式");
    }

    void OnDestroy()
    {
        // 取消订阅
        PaddleController.OnPaddleAction -= OnPaddleAction;
        PaddleController.OnSyncPaddle -= OnSyncPaddle;
    }

    void Update()
    {
        // 更新速度显示
        if (speedText != null && boatMovement != null)
        {
            speedText.text = "Speed: " + boatMovement.GetCurrentSpeed().ToString("F1") + " m/s";
        }

        // 调试信息
        if (debugMode && Input.GetKeyDown(KeyCode.R))
        {
            ResetCombo();
            Debug.Log("手动重置连击");
        }
    }

    // 响应双桨同步事件
    void OnSyncPaddle(float strength)
    {
        // 双桨同步得分
        int syncScore = syncPaddleBonus;

        // 应用连击奖励
        syncScore += comboCount * comboBonus * 2; // 双倍连击奖励

        // 如果在"全力"状态，得分加成
        if (isFullPower)
        {
            syncScore = Mathf.RoundToInt(syncScore * comboMultiplier);
        }

        // 增加连击
        comboCount += 2; // 双桨同步增加两级连击
        if (comboCount > highestCombo)
            highestCombo = comboCount;

        // 添加得分
        score += syncScore;

        // 显示反馈
        if (feedbackSystem != null)
        {
            feedbackSystem.ShowTextFeedback("Row in unison! +" + syncScore, Color.cyan);
            feedbackSystem.PlayComboEffect(comboCount);
        }

        // 如果龙舟移动组件存在，增加速度
        if (boatMovement != null)
            boatMovement.AddSpeed(strength * 1.5f);

        // 检查是否触发"全力"状态
        if (comboCount >= comboThreshold && !isFullPower)
        {
            ActivateFullPower();
        }

        // 更新UI
        UpdateUI();

        if (debugMode)
            Debug.Log($"同步划桨! 得分+{syncScore}, 连击:{comboCount}, 力度:{strength}");
    }

    void OnPaddleAction(bool isLeftPaddle, float strength)
    {
        // 如果调试模式下按住特定键，强制完美判定
        bool forcePerfect = debugMode && Input.GetKey(perfectHitKey);

        // 计算节奏得分奖励
        float currentTime = Time.time;
        float timeSinceLastPaddle = currentTime - lastPaddleTime;

        JudgmentResult judgment = forcePerfect ? JudgmentResult.Perfect : JudgeRhythm(timeSinceLastPaddle);

        // 如果要求左右交替且连击大于0，检查是否符合交替要求
        if (requireAlternating && comboCount > 0 && isLeftPaddle == lastPaddleWasLeft && !forcePerfect)
        {
            judgment = JudgmentResult.Miss;

            // 显示反馈
            if (feedbackSystem != null)
            {
                feedbackSystem.ShowTextFeedback("Alternating left-and-right rowing!", Color.red);
            }

            if (debugMode)
                Debug.Log("交替失败! 应为" + (lastPaddleWasLeft ? "右" : "左") + "桨");
        }

        ProcessJudgment(judgment, isLeftPaddle, strength);

        // 更新左右手记录
        lastPaddleWasLeft = isLeftPaddle;

        // 更新最后划桨时间
        lastPaddleTime = currentTime;

        // 更新UI
        UpdateUI();
    }

    JudgmentResult JudgeRhythm(float timeSinceLastPaddle)
    {
        // 防止同一侧快速连击
        if (timeSinceLastPaddle < 0.1f)
            return JudgmentResult.Miss;

        // 检查是否在节奏窗口内
        if (timeSinceLastPaddle > rhythmWindowTime)
            return JudgmentResult.Miss;

        // 检查是否是完美时机
        if (Mathf.Abs(timeSinceLastPaddle - (rhythmWindowTime / 2)) < perfectTiming)
            return JudgmentResult.Perfect;

        // 检查是否是良好时机
        if (Mathf.Abs(timeSinceLastPaddle - (rhythmWindowTime / 2)) < goodTiming)
            return JudgmentResult.Good;

        // 都不是则是Miss
        return JudgmentResult.Miss;
    }

    void ProcessJudgment(JudgmentResult judgment, bool isLeftPaddle, float strength)
    {
        int paddleScore = 0;

        switch (judgment)
        {
            case JudgmentResult.Perfect:
                // 增加连击
                comboCount++;
                if (comboCount > highestCombo)
                    highestCombo = comboCount;

                // 计算得分
                paddleScore = pointsPerPaddle * 2; // 完美判定双倍分数

                // 应用连击奖励
                paddleScore += comboCount * comboBonus;

                // 显示反馈
                if (feedbackSystem != null)
                    feedbackSystem.PlayPaddleEffect(isLeftPaddle ? new Vector3(-0.5f, 0, 0) : new Vector3(0.5f, 0, 0), true);

                // 如果龙舟移动组件存在，增加速度
                if (boatMovement != null)
                    boatMovement.AddSpeed(strength * 1.5f);

                if (debugMode)
                    Debug.Log($"{(isLeftPaddle ? "左" : "右")}桨完美判定! +{paddleScore}分, 连击:{comboCount}");
                break;

            case JudgmentResult.Good:
                // 增加连击
                comboCount++;
                if (comboCount > highestCombo)
                    highestCombo = comboCount;

                // 计算得分
                paddleScore = pointsPerPaddle;

                // 应用连击奖励
                paddleScore += comboCount * comboBonus;

                // 显示反馈
                if (feedbackSystem != null)
                    feedbackSystem.PlayPaddleEffect(isLeftPaddle ? new Vector3(-0.5f, 0, 0) : new Vector3(0.5f, 0, 0), false);

                // 如果龙舟移动组件存在，增加速度
                if (boatMovement != null)
                    boatMovement.AddSpeed(strength);

                if (debugMode)
                    Debug.Log($"{(isLeftPaddle ? "左" : "右")}桨良好判定! +{paddleScore}分, 连击:{comboCount}");
                break;

            case JudgmentResult.Miss:
                // 重置连击
                ResetCombo();

                // 计算得分
                paddleScore = missPenalty;

                // 显示反馈
                if (feedbackSystem != null)
                    feedbackSystem.ShowMissFeedback();

                // 如果龙舟移动组件存在，减少速度
                if (boatMovement != null)
                    boatMovement.ReduceSpeed(0.5f);

                if (debugMode)
                    Debug.Log($"{(isLeftPaddle ? "左" : "右")}桨未命中! {paddleScore}分, 连击重置");
                break;
        }

        // 如果在"全力"状态，得分加成
        if (isFullPower && paddleScore > 0)
        {
            paddleScore = Mathf.RoundToInt(paddleScore * comboMultiplier);
        }

        // 添加得分
        score += paddleScore;
        if (score < 0) score = 0; // 防止负分

        // 检查是否触发"全力"状态
        if (comboCount >= comboThreshold && !isFullPower)
        {
            ActivateFullPower();
        }
    }

    // 重置连击
    private void ResetCombo()
    {
        comboCount = 0;

        // 如果在全力模式，取消全力模式
        if (isFullPower)
        {
            isFullPower = false;

            // 关闭特效
            if (fullPowerEffect != null)
                fullPowerEffect.SetActive(false);

            // 更新UI的全力指示器
            if (uiManager != null && uiManager.fullPowerIndicator != null)
                uiManager.fullPowerIndicator.gameObject.SetActive(false);

            // 如果有计时协程，停止
            if (fullPowerCoroutine != null)
                StopCoroutine(fullPowerCoroutine);

            if (debugMode)
                Debug.Log("全力状态被中断!");
        }
    }

    void ActivateFullPower()
    {
        isFullPower = true;

        // 激活特效
        if (fullPowerEffect != null)
            fullPowerEffect.SetActive(true);

        // 更新UI的全力指示器
        if (uiManager != null && uiManager.fullPowerIndicator != null)
            uiManager.fullPowerIndicator.gameObject.SetActive(true);

        // 显示反馈
        if (feedbackSystem != null)
        {
            feedbackSystem.ShowTextFeedback("FULL POWER!", Color.magenta);
            feedbackSystem.PlayFullPowerEffect();
        }

        if (debugMode)
            Debug.Log("全力状态激活！持续" + fullPowerDuration + "秒");

        // 如果已有计时协程，先停止
        if (fullPowerCoroutine != null)
            StopCoroutine(fullPowerCoroutine);

        // 启动新的计时协程
        fullPowerCoroutine = StartCoroutine(FullPowerTimer());
    }

    IEnumerator FullPowerTimer()
    {
        yield return new WaitForSeconds(fullPowerDuration);

        // 结束全力状态
        isFullPower = false;

        // 关闭特效
        if (fullPowerEffect != null)
            fullPowerEffect.SetActive(false);

        // 更新UI的全力指示器
        if (uiManager != null && uiManager.fullPowerIndicator != null)
            uiManager.fullPowerIndicator.gameObject.SetActive(false);

        if (debugMode)
            Debug.Log("全力状态结束");
    }

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;

        if (comboText != null)
            comboText.text = "Combo: " + comboCount;
    }

    // 获取当前得分
    public int GetScore()
    {
        return score;
    }

    // 获取最高连击数
    public int GetHighestCombo()
    {
        return highestCombo;
    }

    // 设置是否需要左右交替
    public void SetRequireAlternating(bool require)
    {
        requireAlternating = require;

        if (debugMode)
            Debug.Log("左右交替要求: " + (require ? "启用" : "禁用"));
    }

    // 测试用方法 - 手动添加得分
    public void AddScore(int amount)
    {
        score += amount;
        UpdateUI();

        if (debugMode)
            Debug.Log("手动添加分数: " + amount);
    }

    // 测试用方法 - 手动设置连击
    public void SetCombo(int amount)
    {
        comboCount = amount;
        if (comboCount > highestCombo)
            highestCombo = comboCount;

        UpdateUI();

        // 检查是否触发"全力"状态
        if (comboCount >= comboThreshold && !isFullPower)
        {
            ActivateFullPower();
        }

        if (debugMode)
            Debug.Log("手动设置连击: " + amount);
    }

    // 测试用方法 - 手动触发全力状态
    public void ActivateFullPowerManually()
    {
        if (!isFullPower)
        {
            ActivateFullPower();
        }
    }
}