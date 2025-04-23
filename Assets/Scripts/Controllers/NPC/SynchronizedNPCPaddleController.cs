using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 实现同步的NPC划桨控制器，确保多个NPC保持完全一致的划桨节奏和模式
/// 特别适用于需要NPC保持左右交替且统一划桨动作的情况
/// </summary>
public class SynchronizedNPCPaddleController : MonoBehaviour
{
    [Header("NPC设置")]
    public List<Transform> npcLeftPaddles = new List<Transform>(); // 所有NPC的左桨
    public List<Transform> npcRightPaddles = new List<Transform>(); // 所有NPC的右桨

    [Header("划桨设置")]
    public float paddleForwardAmount = 0.5f; // 向前推动量
    public float paddleBackwardAmount = 0.7f; // 向后拉动量
    public float paddleAnimSpeed = 7f; // 划桨动画速度
    public float paddleRecoveryTime = 0.1f; // 桨恢复时间

    [Header("节奏设置")]
    public float paddleInterval = 0.7f; // 划桨间隔
    public bool startWithLeftPaddle = true; // 从左桨开始

    [Header("游戏整合")]
    public bool adaptToGameStage = true; // 根据游戏阶段调整
    public bool autoStart = true; // 自动开始划桨

    // 私有变量
    private List<Vector3> leftPaddleRestPositions = new List<Vector3>();
    private List<Vector3> rightPaddleRestPositions = new List<Vector3>();
    private bool isPaddling = false;
    private Coroutine paddlingCoroutine;
    private bool isLeftPaddleTurn = true; // 当前是否轮到左桨

    // 静态变量用于全局同步
    public static SynchronizedNPCPaddleController instance;

    // 引用
    private GameManager gameManager;
    private StageManager stageManager;

    void Awake()
    {
        // 单例模式确保只有一个控制器实例
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // 获取引用
        gameManager = FindObjectOfType<GameManager>();
        stageManager = FindObjectOfType<StageManager>();

        // 初始化并保存所有桨的初始位置
        if (npcLeftPaddles.Count != npcRightPaddles.Count)
        {
            Debug.LogError("左桨和右桨数量不匹配！请确保每个NPC都有对应的左右桨。");
            return;
        }

        // 保存所有桨的初始位置
        foreach (Transform paddle in npcLeftPaddles)
        {
            if (paddle != null)
                leftPaddleRestPositions.Add(paddle.localPosition);
            else
                leftPaddleRestPositions.Add(Vector3.zero); // 占位
        }

        foreach (Transform paddle in npcRightPaddles)
        {
            if (paddle != null)
                rightPaddleRestPositions.Add(paddle.localPosition);
            else
                rightPaddleRestPositions.Add(Vector3.zero); // 占位
        }

        // 设置初始划桨方向
        isLeftPaddleTurn = startWithLeftPaddle;

        // 自动开始划桨
        if (autoStart)
        {
            StartPaddling();
        }
    }

    void Update()
    {
        // 根据游戏阶段调整行为
        if (adaptToGameStage)
        {
            UpdateBehaviorForCurrentStage();
        }
    }

    /// <summary>
    /// 启动同步划桨协程
    /// </summary>
    public void StartPaddling()
    {
        if (!isPaddling)
        {
            if (paddlingCoroutine != null)
                StopCoroutine(paddlingCoroutine);

            paddlingCoroutine = StartCoroutine(SynchronizedPaddlingRoutine());
        }
    }

    /// <summary>
    /// 停止划桨
    /// </summary>
    public void StopPaddling()
    {
        if (isPaddling && paddlingCoroutine != null)
        {
            StopCoroutine(paddlingCoroutine);
            paddlingCoroutine = null;
            isPaddling = false;
        }
    }

    /// <summary>
    /// 同步划桨协程，确保所有NPC保持一致的划桨节奏和模式
    /// </summary>
    private IEnumerator SynchronizedPaddlingRoutine()
    {
        isPaddling = true;
        float nextPaddleTime = Time.time;

        while (isPaddling)
        {
            // 等待到达下一次划桨时间，确保精确的时间控制
            while (Time.time < nextPaddleTime)
            {
                yield return null;
            }

            // 执行当前paddle的划桨动作
            if (isLeftPaddleTurn)
            {
                yield return StartCoroutine(AnimateAllPaddles(true));
            }
            else
            {
                yield return StartCoroutine(AnimateAllPaddles(false));
            }

            // 切换到另一侧
            isLeftPaddleTurn = !isLeftPaddleTurn;

            // 精确计算下一次划桨时间
            nextPaddleTime = Time.time + paddleInterval;
        }
    }

    /// <summary>
    /// 同时为所有NPC的同一侧桨执行划桨动画
    /// </summary>
    private IEnumerator AnimateAllPaddles(bool isLeft)
    {
        List<Transform> paddlesToAnimate = isLeft ? npcLeftPaddles : npcRightPaddles;
        List<Vector3> restPositions = isLeft ? leftPaddleRestPositions : rightPaddleRestPositions;

        if (paddlesToAnimate.Count == 0)
            yield break;

        // 计算动作时间
        float strokeTime = 1f / paddleAnimSpeed;

        // 1. 向前推动作
        float forwardTime = strokeTime / 4;
        float startTime = Time.time;
        float elapsedTime = 0f;

        while (elapsedTime < forwardTime)
        {
            elapsedTime = Time.time - startTime;
            float progress = Mathf.Clamp01(elapsedTime / forwardTime);

            // 同时更新所有桨的位置
            for (int i = 0; i < paddlesToAnimate.Count; i++)
            {
                if (paddlesToAnimate[i] != null && i < restPositions.Count)
                {
                    Vector3 restPos = restPositions[i];
                    Vector3 forwardPos = restPos + new Vector3(0, 0, paddleForwardAmount);
                    paddlesToAnimate[i].localPosition = Vector3.Lerp(restPos, forwardPos, progress);
                }
            }

            yield return null;
        }

        // 确保所有桨都到达前进位置
        for (int i = 0; i < paddlesToAnimate.Count; i++)
        {
            if (paddlesToAnimate[i] != null && i < restPositions.Count)
            {
                Vector3 restPos = restPositions[i];
                Vector3 forwardPos = restPos + new Vector3(0, 0, paddleForwardAmount);
                paddlesToAnimate[i].localPosition = forwardPos;
            }
        }

        // 2. 向后拉动作
        float backwardTime = strokeTime / 2;
        startTime = Time.time;
        elapsedTime = 0f;

        while (elapsedTime < backwardTime)
        {
            elapsedTime = Time.time - startTime;
            float progress = Mathf.Clamp01(elapsedTime / backwardTime);

            // 同时更新所有桨的位置
            for (int i = 0; i < paddlesToAnimate.Count; i++)
            {
                if (paddlesToAnimate[i] != null && i < restPositions.Count)
                {
                    Vector3 restPos = restPositions[i];
                    Vector3 forwardPos = restPos + new Vector3(0, 0, paddleForwardAmount);
                    Vector3 backwardPos = restPos - new Vector3(0, 0, paddleBackwardAmount);
                    paddlesToAnimate[i].localPosition = Vector3.Lerp(forwardPos, backwardPos, progress);
                }
            }

            yield return null;
        }

        // 确保所有桨都到达后退位置
        for (int i = 0; i < paddlesToAnimate.Count; i++)
        {
            if (paddlesToAnimate[i] != null && i < restPositions.Count)
            {
                Vector3 restPos = restPositions[i];
                Vector3 backwardPos = restPos - new Vector3(0, 0, paddleBackwardAmount);
                paddlesToAnimate[i].localPosition = backwardPos;
            }
        }

        // 3. 恢复到初始位置
        float recoveryTime = paddleRecoveryTime;
        startTime = Time.time;
        elapsedTime = 0f;

        while (elapsedTime < recoveryTime)
        {
            elapsedTime = Time.time - startTime;
            float progress = Mathf.Clamp01(elapsedTime / recoveryTime);

            // 同时更新所有桨的位置
            for (int i = 0; i < paddlesToAnimate.Count; i++)
            {
                if (paddlesToAnimate[i] != null && i < restPositions.Count)
                {
                    Vector3 restPos = restPositions[i];
                    Vector3 backwardPos = restPos - new Vector3(0, 0, paddleBackwardAmount);
                    paddlesToAnimate[i].localPosition = Vector3.Lerp(backwardPos, restPos, progress);
                }
            }

            yield return null;
        }

        // 确保所有桨都回到精确的初始位置
        for (int i = 0; i < paddlesToAnimate.Count; i++)
        {
            if (paddlesToAnimate[i] != null && i < restPositions.Count)
            {
                paddlesToAnimate[i].localPosition = restPositions[i];
            }
        }
    }

    /// <summary>
    /// 根据当前游戏阶段更新NPC行为
    /// </summary>
    private void UpdateBehaviorForCurrentStage()
    {
        int currentStage = 1;

        // 获取当前阶段
        if (gameManager != null)
            currentStage = gameManager.currentStage;
        else if (stageManager != null)
            currentStage = stageManager.currentStage;

        // 根据阶段调整划桨间隔
        switch (currentStage)
        {
            case 1: // 基本操作
                paddleInterval = 0.8f; // 较慢，便于玩家学习
                break;

            case 2: // 合作
                paddleInterval = 0.7f; // 稍快
                break;

            case 3: // 特殊机制
                paddleInterval = 0.6f; // 再快一些
                break;

            case 4: // 正式尝试
                paddleInterval = 0.5f; // 快速节奏
                break;
        }
    }

    /// <summary>
    /// 设置所有NPC划桨的节奏间隔
    /// </summary>
    public void SetPaddleInterval(float interval)
    {
        paddleInterval = Mathf.Max(0.2f, interval); // 防止间隔过小
    }

    /// <summary>
    /// 重置所有桨的位置到初始状态
    /// </summary>
    public void ResetAllPaddles()
    {
        for (int i = 0; i < npcLeftPaddles.Count; i++)
        {
            if (npcLeftPaddles[i] != null && i < leftPaddleRestPositions.Count)
            {
                npcLeftPaddles[i].localPosition = leftPaddleRestPositions[i];
            }
        }

        for (int i = 0; i < npcRightPaddles.Count; i++)
        {
            if (npcRightPaddles[i] != null && i < rightPaddleRestPositions.Count)
            {
                npcRightPaddles[i].localPosition = rightPaddleRestPositions[i];
            }
        }
    }

    /// <summary>
    /// 切换起始桨
    /// </summary>
    public void SetStartWithLeftPaddle(bool startLeft)
    {
        // 只有在非划桨状态下，才能改变起始桨
        if (!isPaddling)
        {
            startWithLeftPaddle = startLeft;
            isLeftPaddleTurn = startLeft;
        }
    }

    /// <summary>
    /// 获取当前正在使用的桨（左或右）
    /// </summary>
    public bool IsLeftPaddleTurn()
    {
        return isLeftPaddleTurn;
    }

    void OnDisable()
    {
        StopPaddling();
    }

    void OnDestroy()
    {
        StopPaddling();

        // 清除单例引用
        if (instance == this)
        {
            instance = null;
        }
    }
}