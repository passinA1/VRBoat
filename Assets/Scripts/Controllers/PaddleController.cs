using UnityEngine;
using System.Collections;
using UnityEngine.XR;
using System.Collections.Generic;

public class PaddleController : MonoBehaviour
{
    [Header("桨设置")]
    public Transform leftPaddle;
    public Transform rightPaddle;
    public float paddleForwardAmount = 0.5f; // 增加向前推动量
    public float paddleBackwardAmount = 0.7f; // 增加向后拉动量
    public float paddleAnimSpeed = 7f; // 划桨动画速度
    public float paddleRecoveryTime = 0.1f; // 桨恢复时间

    [Header("节奏判定设置")]
    public float perfectTiming = 0.1f; // 完美判定窗口(秒)
    public float goodTiming = 0.3f; // 良好判定窗口(秒)
    public Transform leftFlagMarker; // 左旗帜判定点
    public Transform rightFlagMarker; // 右旗帜判定点
    public Transform judgmentLine; // 判定线

    [Header("模拟测试设置")]
    public bool forceKeyboardMode = true; // 强制使用键盘模式（用于测试）
    public KeyCode leftPaddleKey = KeyCode.Q;
    public KeyCode rightPaddleKey = KeyCode.E;
    public KeyCode syncPaddleKey = KeyCode.Space;
    public KeyCode perfectPaddleKey = KeyCode.LeftShift; // 模拟完美划桨的按键

    [Header("调试选项")]
    public bool showDebugInfo = true; // 显示调试信息
    public bool randomizePaddleStrength = true; // 随机化划桨力度（模拟真实手感）
    public float paddleDistanceThreshold = 0.35f; // 划桨距离阈值，可在设置中调整（单位:米）

    // 划桨事件
    public delegate void PaddleActionEvent(bool isLeftPaddle, float strength);
    public static event PaddleActionEvent OnPaddleAction;

    // 双桨同步事件
    public delegate void SyncPaddleEvent(float strength);
    public static event SyncPaddleEvent OnSyncPaddle;

    private Vector3 leftPaddleRestPos;
    private Vector3 rightPaddleRestPos;
    private bool leftPaddleMoving = false;
    private bool rightPaddleMoving = false;
    private Coroutine leftPaddleCoroutine;
    private Coroutine rightPaddleCoroutine;
    private bool isVRMode = false;
    private float lastLeftPaddleTime = -1f;
    private float lastRightPaddleTime = -1f;

    // 划桨状态
    private enum PaddleState { Ready, Forward, Backward, Recovering }
    private PaddleState leftPaddleState = PaddleState.Ready;
    private PaddleState rightPaddleState = PaddleState.Ready;

    // 引用反馈系统
    private FeedbackSystem feedbackSystem;
    private ScoreSystem scoreSystem;

    void Start()
    {
        // 保存桨的初始位置
        if (leftPaddle) leftPaddleRestPos = leftPaddle.localPosition;
        if (rightPaddle) rightPaddleRestPos = rightPaddle.localPosition;

        // 获取反馈系统引用
        feedbackSystem = FindObjectOfType<FeedbackSystem>();
        scoreSystem = FindObjectOfType<ScoreSystem>();

        // 检测是否启用VR
        CheckVRAvailability();

        Debug.Log($"PaddleController初始化完成，当前模式: {(isVRMode && !forceKeyboardMode ? "VR" : "键盘")}");
    }

    void Update()
    {
        // 如果强制键盘模式或者未检测到VR设备，使用键盘输入
        if (forceKeyboardMode || !isVRMode)
        {
            HandleKeyboardInput();
        }
        else
        {
            HandleVRInput();
        }

        // 显示调试信息
        if (showDebugInfo)
        {
            Debug.DrawRay(leftPaddle.position, Vector3.forward * 0.5f, Color.blue);
            Debug.DrawRay(rightPaddle.position, Vector3.forward * 0.5f, Color.red);
        }
    }

    // 检测VR是否可用
    private void CheckVRAvailability()
    {
        if (forceKeyboardMode)
        {
            isVRMode = false;
            return;
        }

        var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
        SubsystemManager.GetInstances(xrDisplaySubsystems);

        foreach (var xrDisplay in xrDisplaySubsystems)
        {
            if (xrDisplay.running)
            {
                isVRMode = true;
                Debug.Log("已检测到VR设备，启用VR输入模式");
                return;
            }
        }

        isVRMode = false;
        Debug.Log("未检测到VR设备，使用键盘模拟模式");
    }

    // 处理键盘输入（模拟VR）
    private void HandleKeyboardInput()
    {
        // 左桨划动
        if (Input.GetKeyDown(leftPaddleKey) && !leftPaddleMoving)
        {
            if (leftPaddleCoroutine != null)
                StopCoroutine(leftPaddleCoroutine);

            bool isPerfect = Input.GetKey(perfectPaddleKey);
            float strength = GetPaddleStrength(isPerfect);
            leftPaddleCoroutine = StartCoroutine(PaddleStroke(true, isPerfect, strength));
            lastLeftPaddleTime = Time.time;

            // 检查是否双桨同步 (0.2秒内)
            if (lastRightPaddleTime > 0 && Time.time - lastRightPaddleTime < 0.2f)
            {
                TriggerSyncPaddle(isPerfect);
            }
        }

        // 右桨划动
        if (Input.GetKeyDown(rightPaddleKey) && !rightPaddleMoving)
        {
            if (rightPaddleCoroutine != null)
                StopCoroutine(rightPaddleCoroutine);

            bool isPerfect = Input.GetKey(perfectPaddleKey);
            float strength = GetPaddleStrength(isPerfect);
            rightPaddleCoroutine = StartCoroutine(PaddleStroke(false, isPerfect, strength));
            lastRightPaddleTime = Time.time;

            // 检查是否双桨同步 (0.2秒内)
            if (lastLeftPaddleTime > 0 && Time.time - lastLeftPaddleTime < 0.2f)
            {
                TriggerSyncPaddle(isPerfect);
            }
        }

        // 双桨同步划动
        if (Input.GetKeyDown(syncPaddleKey) && !leftPaddleMoving && !rightPaddleMoving)
        {
            bool isPerfect = Input.GetKey(perfectPaddleKey);
            StartCoroutine(SyncPaddleStroke(isPerfect));
        }
    }

    // 处理VR输入
    private void HandleVRInput()
    {
        // 由于VR设备暂时不可用，这部分逻辑保留但不实现具体功能
        // 未来当VR设备可用时，可以扩展此方法
        if (showDebugInfo && !forceKeyboardMode)
            Debug.Log("VR输入处理功能待实现");
    }

    // 获取划桨力度
    private float GetPaddleStrength(bool isPerfect)
    {
        if (isPerfect)
            return 2.0f; // 完美划桨力度

        if (randomizePaddleStrength)
        {
            // 根据设置的划桨距离阈值调整随机范围
            float minStrength = 1.0f;
            float maxStrength = 1.0f + paddleDistanceThreshold;
            return Random.Range(minStrength, maxStrength);
        }

        return 1.5f; // 默认力度
    }

    // 触发双桨同步事件
    private void TriggerSyncPaddle(bool isPerfect = false)
    {
        //Debug.Log("触发双桨同步!");

        // 触发同步划桨事件，强度为2.5，提供更强的推动力
        float strength = isPerfect ? 3.0f : 2.5f;
        if (OnSyncPaddle != null)
            OnSyncPaddle(strength);

        // 播放特殊同步特效
        if (feedbackSystem != null)
        {
            feedbackSystem.PlayComboEffect(2);
        }
    }

    // 同步双桨划动协程
    IEnumerator SyncPaddleStroke(bool isPerfect = false)
    {
        // 停止任何正在进行的划桨动画
        if (leftPaddleCoroutine != null)
            StopCoroutine(leftPaddleCoroutine);

        if (rightPaddleCoroutine != null)
            StopCoroutine(rightPaddleCoroutine);

        leftPaddleMoving = true;
        rightPaddleMoving = true;

        // 计算动作时间
        float strokeTime = 1f / paddleAnimSpeed;

        // 1. 向前推动作
        Vector3 leftForwardPos = leftPaddleRestPos + new Vector3(0, 0, paddleForwardAmount);
        Vector3 rightForwardPos = rightPaddleRestPos + new Vector3(0, 0, paddleForwardAmount);
        float forwardTime = strokeTime / 4;
        float t = 0;

        while (t < forwardTime)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / forwardTime);
            leftPaddle.localPosition = Vector3.Lerp(leftPaddleRestPos, leftForwardPos, progress);
            rightPaddle.localPosition = Vector3.Lerp(rightPaddleRestPos, rightForwardPos, progress);
            yield return null;
        }

        // 2. 向后拉动作
        Vector3 leftBackwardPos = leftPaddleRestPos - new Vector3(0, 0, paddleBackwardAmount);
        Vector3 rightBackwardPos = rightPaddleRestPos - new Vector3(0, 0, paddleBackwardAmount);
        float backwardTime = strokeTime / 2;
        t = 0;

        bool eventTriggered = false;

        while (t < backwardTime)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / backwardTime);
            leftPaddle.localPosition = Vector3.Lerp(leftForwardPos, leftBackwardPos, progress);
            rightPaddle.localPosition = Vector3.Lerp(rightForwardPos, rightBackwardPos, progress);

            // 在划桨动作早期就触发前进事件，使反应更迅速
            if (progress >= 0.3f && !eventTriggered)
            {
                eventTriggered = true;

                // 触发同步划桨事件
                float strength = isPerfect ? 3.0f : 2.5f;
                if (OnSyncPaddle != null)
                    OnSyncPaddle(strength);

                // 播放划桨特效和音效
                if (feedbackSystem != null)
                {
                    feedbackSystem.PlayPaddleEffect(leftPaddle.position, isPerfect);
                    feedbackSystem.PlayPaddleEffect(rightPaddle.position, isPerfect);
                }
            }

            yield return null;
        }

        // 3. 快速恢复到初始位置
        t = 0;
        while (t < paddleRecoveryTime)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / paddleRecoveryTime);
            leftPaddle.localPosition = Vector3.Lerp(leftBackwardPos, leftPaddleRestPos, progress);
            rightPaddle.localPosition = Vector3.Lerp(rightBackwardPos, rightPaddleRestPos, progress);
            yield return null;
        }

        // 确保回到精确的休息位置
        leftPaddle.localPosition = leftPaddleRestPos;
        rightPaddle.localPosition = rightPaddleRestPos;

        // 结束划桨状态
        leftPaddleMoving = false;
        rightPaddleMoving = false;
    }

    IEnumerator PaddleStroke(bool isLeft, bool isPerfect = false, float strength = 1.5f)
    {
        Transform paddle = isLeft ? leftPaddle : rightPaddle;
        Vector3 restPos = isLeft ? leftPaddleRestPos : rightPaddleRestPos;

        if (paddle == null)
        {
            Debug.LogError(isLeft ? "左桨对象为空！" : "右桨对象为空！");
            yield break;
        }

        // 设置正在划桨状态
        if (isLeft)
        {
            leftPaddleMoving = true;
            leftPaddleState = PaddleState.Forward;
        }
        else
        {
            rightPaddleMoving = true;
            rightPaddleState = PaddleState.Forward;
        }

        // 计算动作时间
        float strokeTime = 1f / paddleAnimSpeed;

        // 1. 向前推动作
        Vector3 forwardPos = restPos + new Vector3(0, 0, paddleForwardAmount);
        float forwardTime = strokeTime / 4;
        float t = 0;

        while (t < forwardTime)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / forwardTime);
            paddle.localPosition = Vector3.Lerp(restPos, forwardPos, progress);
            yield return null;
        }

        // 更新状态
        if (isLeft)
            leftPaddleState = PaddleState.Backward;
        else
            rightPaddleState = PaddleState.Backward;

        // 2. 向后拉动作
        Vector3 backwardPos = restPos - new Vector3(0, 0, paddleBackwardAmount);
        float backwardTime = strokeTime / 2;
        t = 0;

        bool eventTriggered = false;

        while (t < backwardTime)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / backwardTime);
            paddle.localPosition = Vector3.Lerp(forwardPos, backwardPos, progress);

            // 在划桨动作早期就触发前进事件，使反应更迅速
            if (progress >= 0.3f && !eventTriggered)
            {
                eventTriggered = true;

                // 触发划桨事件
                if (OnPaddleAction != null)
                    OnPaddleAction(isLeft, strength);

                // 播放划桨特效和音效
                if (feedbackSystem != null)
                {
                    feedbackSystem.PlayPaddleEffect(paddle.position, isPerfect);
                }
            }

            yield return null;
        }

        // 更新状态
        if (isLeft)
            leftPaddleState = PaddleState.Recovering;
        else
            rightPaddleState = PaddleState.Recovering;

        // 3. 快速恢复到初始位置
        t = 0;
        while (t < paddleRecoveryTime)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / paddleRecoveryTime);
            paddle.localPosition = Vector3.Lerp(backwardPos, restPos, progress);
            yield return null;
        }

        // 确保回到精确的休息位置
        paddle.localPosition = restPos;

        // 结束划桨状态
        if (isLeft)
        {
            leftPaddleMoving = false;
            leftPaddleState = PaddleState.Ready;
        }
        else
        {
            rightPaddleMoving = false;
            rightPaddleState = PaddleState.Ready;
        }
    }

    // 设置划桨距离阈值
    public void SetPaddleDistanceThreshold(float distance)
    {
        // 限制在合理范围内 (10-50cm)
        paddleDistanceThreshold = Mathf.Clamp(distance, 0.1f, 0.5f);

        if (showDebugInfo)
            Debug.Log($"划桨距离阈值设置为: {paddleDistanceThreshold * 100}cm");
    }

    // 模拟测试用方法 - 通过代码触发划桨
    public void SimulatePaddle(bool isLeft, bool isPerfect = false)
    {
        if ((isLeft && !leftPaddleMoving) || (!isLeft && !rightPaddleMoving))
        {
            if (isLeft)
            {
                if (leftPaddleCoroutine != null)
                    StopCoroutine(leftPaddleCoroutine);

                float strength = GetPaddleStrength(isPerfect);
                leftPaddleCoroutine = StartCoroutine(PaddleStroke(true, isPerfect, strength));
                lastLeftPaddleTime = Time.time;
            }
            else
            {
                if (rightPaddleCoroutine != null)
                    StopCoroutine(rightPaddleCoroutine);

                float strength = GetPaddleStrength(isPerfect);
                rightPaddleCoroutine = StartCoroutine(PaddleStroke(false, isPerfect, strength));
                lastRightPaddleTime = Time.time;
            }

            // 检查是否双桨同步 (0.2秒内)
            float lastOtherTime = isLeft ? lastRightPaddleTime : lastLeftPaddleTime;
            if (lastOtherTime > 0 && Time.time - lastOtherTime < 0.2f)
            {
                TriggerSyncPaddle(isPerfect);
            }
        }
    }

    // 模拟测试用方法 - 通过代码触发同步划桨
    public void SimulateSyncPaddle(bool isPerfect = false)
    {
        if (!leftPaddleMoving && !rightPaddleMoving)
        {
            StartCoroutine(SyncPaddleStroke(isPerfect));
        }
    }
}