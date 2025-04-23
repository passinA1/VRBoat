using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 控制单个NPC的划桨行为，与全局同步控制器配合使用
/// 适用于需要多个NPC保持一致行为的场景
/// </summary>
public class NPCPaddleController : MonoBehaviour
{
    [Header("NPC基本设置")]
    public string npcName = "NPC船手";
    public Transform npcBoat; // NPC的船对象
    public bool isActiveNPC = true; // 是否激活此NPC

    [Header("NPC桨设置")]
    public Transform leftPaddle; // 左桨
    public Transform rightPaddle; // 右桨
    public float paddleForwardAmount = 0.5f; // 向前推动量
    public float paddleBackwardAmount = 0.7f; // 向后拉动量
    public float paddleAnimSpeed = 7f; // 划桨动画速度
    public float paddleRecoveryTime = 0.1f; // 桨恢复时间

    [Header("同步设置")]
    public bool useSynchronizedController = true; // 是否使用全局同步控制器
    public int syncGroupID = 0; // 同步组ID，相同ID的NPC保持同步

    [Header("调试选项")]
    public bool showDebugInfo = false; // 是否显示调试信息

    // 私有变量
    private Vector3 leftPaddleRestPos;
    private Vector3 rightPaddleRestPos;
    private Coroutine paddleCoroutine;
    private bool isCurrentlyPaddling = false;
    private bool lastPaddleWasLeft = false;

    // 全局同步器的引用
    private NPCPaddleManager paddleManager;
    private SynchronizedNPCPaddleController syncController;

    void Start()
    {
        // 保存桨的初始位置
        if (leftPaddle) leftPaddleRestPos = leftPaddle.localPosition;
        if (rightPaddle) rightPaddleRestPos = rightPaddle.localPosition;

        // 查找同步控制器与管理器
        paddleManager = FindObjectOfType<NPCPaddleManager>();
        syncController = FindObjectOfType<SynchronizedNPCPaddleController>();

        // 如果使用同步控制器，自身不启动划桨协程
        if (!useSynchronizedController && isActiveNPC)
        {
            StartIndividualPaddling();
        }
        else if (showDebugInfo)
        {
            Debug.Log($"NPC {npcName} 使用全局同步控制器，不启动自身划桨协程");
        }
    }

    void Update()
    {
        // 如果使用同步控制器，确保桨的位置与控制器同步
        if (useSynchronizedController && syncController != null)
        {
            SyncWithGlobalController();
        }
    }

    /// <summary>
    /// 与全局控制器同步桨的位置
    /// </summary>
    private void SyncWithGlobalController()
    {
        // 确保同步控制器存在且已添加桨
        if (syncController == null ||
            syncController.npcLeftPaddles.Count == 0 ||
            syncController.npcRightPaddles.Count == 0)
            return;

        // 检查自己的桨是否已添加到同步控制器
        if (!syncController.npcLeftPaddles.Contains(leftPaddle) ||
            !syncController.npcRightPaddles.Contains(rightPaddle))
        {
            if (showDebugInfo)
                Debug.LogWarning($"NPC {npcName} 的桨未添加到同步控制器中！");
            return;
        }

        // 当使用同步控制器时，不需要额外操作
        // 同步控制器会直接控制所有桨的位置
    }

    /// <summary>
    /// 启动独立划桨协程（当不使用同步控制器时）
    /// </summary>
    public void StartIndividualPaddling()
    {
        if (useSynchronizedController)
        {
            Debug.LogWarning($"NPC {npcName} 使用全局同步控制器，无法启动独立划桨协程");
            return;
        }

        if (paddleCoroutine != null)
        {
            StopCoroutine(paddleCoroutine);
        }

        paddleCoroutine = StartCoroutine(IndividualPaddlingRoutine());
    }

    /// <summary>
    /// 停止独立划桨
    /// </summary>
    public void StopIndividualPaddling()
    {
        if (paddleCoroutine != null)
        {
            StopCoroutine(paddleCoroutine);
            paddleCoroutine = null;
            isCurrentlyPaddling = false;
        }
    }

    /// <summary>
    /// 独立划桨协程（在不使用同步控制器时）
    /// </summary>
    private IEnumerator IndividualPaddlingRoutine()
    {
        isCurrentlyPaddling = true;
        float paddleInterval = 0.7f; // 默认划桨间隔

        // 如果有管理器，从管理器获取间隔
        if (paddleManager != null)
        {
            paddleInterval = paddleManager.basePaddleInterval;
        }

        while (isCurrentlyPaddling)
        {
            // 交替使用左右桨
            bool useLeftPaddle = !lastPaddleWasLeft;

            // 执行划桨动作
            yield return StartCoroutine(PerformPaddleStroke(useLeftPaddle));

            // 更新最后使用的桨
            lastPaddleWasLeft = useLeftPaddle;

            // 等待下一次划桨
            yield return new WaitForSeconds(paddleInterval);
        }
    }

    /// <summary>
    /// 执行单个划桨动作
    /// </summary>
    private IEnumerator PerformPaddleStroke(bool isLeft)
    {
        Transform paddle = isLeft ? leftPaddle : rightPaddle;
        Vector3 restPos = isLeft ? leftPaddleRestPos : rightPaddleRestPos;

        if (paddle == null)
        {
            Debug.LogWarning($"NPC {npcName} 的{(isLeft ? "左" : "右")}桨为空！");
            yield break;
        }

        // 计算动作时间
        float strokeTime = 1f / paddleAnimSpeed;

        // 1. 向前推动作
        Vector3 forwardPos = restPos + new Vector3(0, 0, paddleForwardAmount);
        float forwardTime = strokeTime / 4;
        float startTime = Time.time;
        float elapsedTime = 0f;

        while (elapsedTime < forwardTime)
        {
            elapsedTime = Time.time - startTime;
            float progress = Mathf.Clamp01(elapsedTime / forwardTime);
            paddle.localPosition = Vector3.Lerp(restPos, forwardPos, progress);
            yield return null;
        }

        // 确保到达前进位置
        paddle.localPosition = forwardPos;

        // 2. 向后拉动作
        Vector3 backwardPos = restPos - new Vector3(0, 0, paddleBackwardAmount);
        float backwardTime = strokeTime / 2;
        startTime = Time.time;
        elapsedTime = 0f;

        while (elapsedTime < backwardTime)
        {
            elapsedTime = Time.time - startTime;
            float progress = Mathf.Clamp01(elapsedTime / backwardTime);
            paddle.localPosition = Vector3.Lerp(forwardPos, backwardPos, progress);
            yield return null;
        }

        // 确保到达后退位置
        paddle.localPosition = backwardPos;

        // 3. 快速恢复到初始位置
        float recoveryTime = paddleRecoveryTime;
        startTime = Time.time;
        elapsedTime = 0f;

        while (elapsedTime < recoveryTime)
        {
            elapsedTime = Time.time - startTime;
            float progress = Mathf.Clamp01(elapsedTime / recoveryTime);
            paddle.localPosition = Vector3.Lerp(backwardPos, restPos, progress);
            yield return null;
        }

        // 确保回到精确的休息位置
        paddle.localPosition = restPos;
    }

    /// <summary>
    /// 设置NPC是否激活
    /// </summary>
    public void SetNPCActive(bool active)
    {
        isActiveNPC = active;

        // 只有在独立模式下才控制协程
        if (!useSynchronizedController)
        {
            if (active && paddleCoroutine == null)
            {
                StartIndividualPaddling();
            }
            else if (!active && paddleCoroutine != null)
            {
                StopIndividualPaddling();
            }
        }
    }

    /// <summary>
    /// 立即重置桨到初始位置
    /// </summary>
    public void ResetPaddles()
    {
        if (leftPaddle != null)
            leftPaddle.localPosition = leftPaddleRestPos;

        if (rightPaddle != null)
            rightPaddle.localPosition = rightPaddleRestPos;
    }

    /// <summary>
    /// 设置是否使用全局同步控制器
    /// </summary>
    public void SetUseSynchronizedController(bool useSync)
    {
        // 如果状态改变
        if (useSynchronizedController != useSync)
        {
            useSynchronizedController = useSync;

            // 如果切换到独立模式，启动独立划桨
            if (!useSync && isActiveNPC)
            {
                StopIndividualPaddling(); // 确保停止之前的协程
                StartIndividualPaddling();
            }
            // 如果切换到同步模式，停止独立划桨
            else if (useSync && paddleCoroutine != null)
            {
                StopIndividualPaddling();
            }
        }
    }

    void OnDisable()
    {
        StopIndividualPaddling();
    }

    void OnDestroy()
    {
        StopIndividualPaddling();
    }
}