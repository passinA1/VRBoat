using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.XR;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(PlayerInput))]
public class PaddleController : MonoBehaviour
{
    [Header("桨设置")]
    public Transform leftPaddle;
    public Transform rightPaddle;
    public float paddleForwardAmount = 0.5f; // 增加向前推动量
    public float paddleBackwardAmount = 0.7f; // 增加向后拉动量
    public float paddleAnimSpeed = 7f; // 划桨动画速度
    public float paddleRecoveryTime = 0.1f; // 桨恢复时间

    [Header("手柄输入设置")]
    public float velocityThreshold = 1.5f; // 触发划桨的最小速度(m/s)
    public float cooldownTime = 0.3f; // 划桨冷却时间
    // 输入系统相关
    private PlayerInput playerInput;
    private InputAction leftPaddleAction;
    private InputAction rightPaddleAction;
    private InputAction syncPaddleAction;

    // 手柄速度检测
    private Vector3 leftControllerLastPos;
    private Vector3 rightControllerLastPos;
    private Vector3 leftControllerVelocity;
    private Vector3 rightControllerVelocity;

    // 手柄引用
    private ActionBasedController leftController;
    private ActionBasedController rightController;


    [Header("节奏判定设置")]
    public float perfectTiming = 0.1f; // 完美判定窗口(秒)
    public float goodTiming = 0.3f; // 良好判定窗口(秒)
    public Transform leftFlagMarker; // 左旗帜判定点
    public Transform rightFlagMarker; // 右旗帜判定点
    public Transform judgmentLine; // 判定线

    [Header("模拟测试设置")]
    public bool forceKeyboardMode = true; // 强制使用键盘模式（用于测试）
    /*public KeyCode leftPaddleKey = KeyCode.Q;
    public KeyCode rightPaddleKey = KeyCode.E;
    public KeyCode syncPaddleKey = KeyCode.Space;
    public KeyCode perfectPaddleKey = KeyCode.LeftShift; // 模拟完美划桨的按键*/

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

        // 初始化输入系统
        InitializeInputSystem();
        FindXRControllers();

    }

    void Update()
    {

        // 显示调试信息
        if (showDebugInfo)
        {
            Debug.DrawRay(leftPaddle.position, Vector3.forward * 0.5f, Color.blue);
            Debug.DrawRay(rightPaddle.position, Vector3.forward * 0.5f, Color.red);
        }

        CalculateControllerVelocity();
        HandleAutomaticPaddleInput();
    }


    #region 输入系统初始化
    private void InitializeInputSystem()
    {
        playerInput = GetComponent<PlayerInput>();

        // 创建输入Action引用
        leftPaddleAction = playerInput.actions["LeftPaddle"];
        rightPaddleAction = playerInput.actions["RightPaddle"];
        syncPaddleAction = playerInput.actions["SyncPaddle"];

        // 绑定输入事件
        leftPaddleAction.started += ctx => OnPaddleInput(true);
        rightPaddleAction.started += ctx => OnPaddleInput(false);
        syncPaddleAction.started += ctx => OnSyncPaddleInput();
    }

    private void FindXRControllers()
    {
        var controllers = FindObjectsOfType<ActionBasedController>();
        foreach (var controller in controllers)
        {
            if (controller.name.Contains("Left"))
                leftController = controller;
            else if (controller.name.Contains("Right"))
                rightController = controller;
        }
    }
    #endregion

    #region 手柄速度计算
    private void CalculateControllerVelocity()
    {
        if (leftController)
        {
            Vector3 currentPos = leftController.transform.position;
            leftControllerVelocity = (currentPos - leftControllerLastPos) / Time.deltaTime;
            leftControllerLastPos = currentPos;
        }

        if (rightController)
        {
            Vector3 currentPos = rightController.transform.position;
            rightControllerVelocity = (currentPos - rightControllerLastPos) / Time.deltaTime;
            rightControllerLastPos = currentPos;
        }
    }

    private bool CheckSwingVelocity(bool isLeft)
    {
        if (isVRMode)
        {
            Vector3 velocity = isLeft ? leftControllerVelocity : rightControllerVelocity;

            // 计算向前挥动的速度分量（假设向前是控制器的Z轴方向）
            float forwardSpeed = Vector3.Dot(velocity, isLeft ?
                leftController.transform.forward :
                rightController.transform.forward);

            return Mathf.Abs(forwardSpeed) > velocityThreshold;
        }
        return false;
    }
    #endregion


    #region 输入处理
    private void HandleAutomaticPaddleInput()
    {
        if (isVRMode)
        {
            // 自动检测左控制器挥动
            if (CheckSwingVelocity(true))
            {
                Debug.Log("挥动左手柄");
                OnPaddleInput(true);
            }

            // 自动检测右控制器挥动
            if (CheckSwingVelocity(false))
            {
                Debug.Log("挥动右手柄");
                OnPaddleInput(false);
            }
        }
    }

    private void OnPaddleInput(bool isLeft)
    {
        if ((isLeft && leftPaddleMoving) || (!isLeft && rightPaddleMoving))
            return;

        bool isPerfect = CheckPerfectTiming(isLeft); ;

        // VR模式下检测完美时机
        if (isVRMode)
        {
            isPerfect = CheckPerfectTiming(isLeft);
        }
        else // 键盘模式使用shift键
        {
            isPerfect = Keyboard.current.leftShiftKey.isPressed;
        }

        StartCoroutine(PaddleStroke(isLeft, isPerfect, GetPaddleStrength(isPerfect)));
        UpdateLastPaddleTime(isLeft);
        CheckSyncPaddle(isLeft);
    }

    private void UpdateLastPaddleTime(bool isLeft)
    {
        if (isLeft)
        {
            lastLeftPaddleTime = Time.time;
        }
        else
        {
            lastRightPaddleTime = Time.time;
        }
    }

    private void CheckSyncPaddle(bool isLeft)
    {
        // 获取另一支桨的时间
        float otherPaddleTime = isLeft ? lastRightPaddleTime : lastLeftPaddleTime;
        float currentTime = Time.time;

        // 检查是否在同步时间窗口内（0.2秒）
        if (otherPaddleTime > 0 && Mathf.Abs(currentTime - otherPaddleTime) < 0.2f)
        {
            // 触发同步划桨
            bool isPerfect = isVRMode ?
                CheckBothPerfectTiming() :
                Keyboard.current.leftShiftKey.isPressed;

            TriggerSyncPaddle(isPerfect);

            // 重置时间记录防止重复触发
            lastLeftPaddleTime = -1f;
            lastRightPaddleTime = -1f;
        }
    }

    private void OnSyncPaddleInput()
    {
        if (!leftPaddleMoving && !rightPaddleMoving)
        {
            bool isPerfect = isVRMode ?
                CheckBothPerfectTiming() :
                Keyboard.current.leftShiftKey.isPressed;

            StartCoroutine(SyncPaddleStroke(isPerfect));
        }
    }
    #endregion

    #region 节奏判定增强
    private bool CheckPerfectTiming(bool isLeft)
    {
        if (!isVRMode) return false;

        // 使用手柄加速度判断完美时机
        Vector3 acceleration = isLeft ?
            leftControllerVelocity / Time.deltaTime :
            rightControllerVelocity / Time.deltaTime;

        return acceleration.magnitude > velocityThreshold * 2f;
    }

    private bool CheckBothPerfectTiming()
    {
        return CheckPerfectTiming(true) && CheckPerfectTiming(false);
    }
    #endregion


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
    private void TriggerSyncPaddle(bool isPerfect)
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

    void OnDestroy()
    {
        // 解除输入绑定
        if (leftPaddleAction != null)
            leftPaddleAction.started -= ctx => OnPaddleInput(true);

        if (rightPaddleAction != null)
            rightPaddleAction.started -= ctx => OnPaddleInput(false);

        if (syncPaddleAction != null)
            syncPaddleAction.started -= ctx => OnSyncPaddleInput();
    }
}