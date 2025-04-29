using UnityEngine;
using System.Collections;

public class DragonBoatMovement : MonoBehaviour
{
    [Header("移动设置")]
    public float initialSpeed = 10.0f; // 初始速度设为10m/s
    public float minSpeed = 5.0f;     // 最低速度设为5m/s
    public float maxSpeed = 20.0f;    // 最高速度设为20m/s
    public float baseForwardForce = 3.0f; // 增加基础前进力，使划桨效果更明显
    public float dragForce = 3.0f;    // 调整阻力系数
    public float boatSmoothTime = 0.05f; // 保持平滑移动时间较短
    public float moveTimeAfterPaddle = 0.3f; // 划桨后继续移动的时间

    [Header("摇动设置")]
    public bool enableRocking = true; // 是否启用摇动效果
    public float rockingAmount = 3.0f; // 摇动幅度
    public float rockingSpeed = 0.5f; // 摇动速度
    public float paddleRockImpact = 2.0f; // 划桨对摇动的影响

    [Header("调试设置")]
    public bool debugMode = false; // 调试模式
    public KeyCode speedUpKey = KeyCode.UpArrow; // 加速键
    public KeyCode speedDownKey = KeyCode.DownArrow; // 减速键
    public KeyCode resetBoatKey = KeyCode.Backspace; // 重置船位置键
    public bool showTrajectory = true; // 显示轨迹
    public int trajectorySteps = 10; // 轨迹点数量
    public Color trajectoryColor = Color.cyan; // 轨迹颜色

    [Header("引用")]
    public Transform trackEnd;
    public GameObject victoryScreen;

    // 私有变量
    private float currentSpeed;
    private float distanceTraveled = 0f;
    private float totalDistance;
    private bool raceCompleted = false;
    private Vector3 velocity = Vector3.zero; // 用于SmoothDamp
    private float lastPaddleTime = 0f; // 上次划桨时间
    private float currentRockAngle = 0f; // 当前摇动角度
    private float targetRockAngle = 0f; // 目标摇动角度
    private Vector3 initialPosition; // 初始位置
    private Quaternion initialRotation; // 初始旋转

    // 事件系统引用
    private GameManager gameManager;

    private UIManager uiManager;
    private ScoreSystem scoreSystem;

    void Start()
    {
        // 获取引用
        gameManager = FindObjectOfType<GameManager>();
        if(gameManager == null)
        {
            Debug.Log("gameManager is null");
        }
        uiManager = FindObjectOfType<UIManager>();
        if(uiManager == null)
        {
            Debug.Log("ui manager is null");
        }

        scoreSystem = FindObjectOfType<ScoreSystem>();
        if(scoreSystem == null)
        {
            Debug.Log("score system is null");
        }

        victoryScreen = uiManager.gameOverMenu;

        // 保存初始位置和旋转
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        // 初始化速度为设定值
        currentSpeed = initialSpeed;

        // 计算总距离
        if (trackEnd != null)
            totalDistance = trackEnd.position.z - transform.position.z;
        else
            totalDistance = 1000f; // 默认距离

        // 隐藏胜利画面
        if (victoryScreen != null)
            victoryScreen.SetActive(false);

        // 订阅划桨事件
        PaddleController.OnPaddleAction += OnPaddleAction;
        PaddleController.OnSyncPaddle += OnSyncPaddle;

        Debug.Log($"DragonBoatMovement初始化完成，总距离: {totalDistance}，初始速度: {currentSpeed}m/s");
    }

    void OnDestroy()
    {
        // 取消订阅
        PaddleController.OnPaddleAction -= OnPaddleAction;
        PaddleController.OnSyncPaddle -= OnSyncPaddle;
    }

    void Update()
    {
        if (raceCompleted)
            return;

        // 调试功能
        if (debugMode)
        {
            HandleDebugInput();
        }

        // 检查是否应该减速
        float timeSinceLastPaddle = Time.time - lastPaddleTime;
        if (timeSinceLastPaddle > moveTimeAfterPaddle)
        {
            // 超过移动时间后减速，但不低于最低速度
            currentSpeed = Mathf.Lerp(currentSpeed, minSpeed, dragForce * Time.deltaTime);
        }
        else
        {
            // 在移动时间内轻微减速
            currentSpeed = Mathf.Max(minSpeed, currentSpeed - (dragForce * 0.5f * Time.deltaTime));
        }

        // 计算船体摇动
        if (enableRocking)
        {
            UpdateBoatRocking();
        }

        // 移动龙舟(使用SmoothDamp实现平滑移动)
        Vector3 targetPosition = transform.position + Vector3.forward * (currentSpeed * Time.deltaTime);
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, boatSmoothTime);

        // 更新已行进距离
        float delta = Vector3.Distance(transform.position, transform.position - velocity * Time.deltaTime);
        distanceTraveled += delta;

        // 检查是否完成比赛
        if (distanceTraveled >= totalDistance && !raceCompleted)
        {
            raceCompleted = true;
            CompleteRace();
        }

        // 显示调试轨迹
        if (debugMode && showTrajectory)
        {
            DrawTrajectory();
        }
    }

    // 处理调试输入
    private void HandleDebugInput()
    {
        // 手动加速
        if (Input.GetKey(speedUpKey))
        {
            currentSpeed = Mathf.Min(currentSpeed + (baseForwardForce * Time.deltaTime * 2), maxSpeed);
            if (debugMode)
            {
                //Debug.Log($"手动加速，当前速度: {currentSpeed:F2}m/s");
            }
        }

        // 手动减速
        if (Input.GetKey(speedDownKey))
        {
            currentSpeed = Mathf.Max(currentSpeed - (baseForwardForce * Time.deltaTime * 2), minSpeed);
            if (debugMode)
            { 
            //Debug.Log($"手动减速，当前速度: {currentSpeed:F2}m/s");
            }
        }

        // 重置船位置
        if (Input.GetKeyDown(resetBoatKey))
        {
            ResetBoat();
        }
    }

    // 重置船
    public void ResetBoat()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        currentSpeed = initialSpeed;
        distanceTraveled = 0f;
        raceCompleted = false;

        // 隐藏胜利画面
        if (victoryScreen != null)
            victoryScreen.SetActive(false);

        if (debugMode)
            Debug.Log("已重置船位置和状态");
    }

    // 绘制预测轨迹
    private void DrawTrajectory()
    {
        Vector3 currentPos = transform.position;
        Vector3 currentVel = velocity;

        for (int i = 0; i < trajectorySteps; i++)
        {
            Vector3 nextPos = currentPos + Vector3.forward * (currentSpeed * Time.deltaTime);

            // 绘制轨迹点
            Debug.DrawLine(currentPos, nextPos, trajectoryColor);

            // 更新位置
            currentPos = nextPos;
        }
    }

    // 更新船体摇动
    private void UpdateBoatRocking()
    {
        // 计算自然摇动
        float naturalRocking = Mathf.Sin(Time.time * rockingSpeed) * rockingAmount;

        // 平滑过渡到目标摇动角度
        currentRockAngle = Mathf.Lerp(currentRockAngle, naturalRocking + targetRockAngle, Time.deltaTime * 2f);

        // 应用摇动 - 只在z轴上摇动（左右摆动）
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, currentRockAngle);

        // 逐渐减少目标摇动角度（从划桨影响恢复）
        targetRockAngle = Mathf.Lerp(targetRockAngle, 0, Time.deltaTime * 3f);
    }

    // 响应划桨事件
    void OnPaddleAction(bool isLeftPaddle, float strength)
    {
        // 更新最后划桨时间
        lastPaddleTime = Time.time;

        // 每次划桨增加速度
        float addedSpeed = baseForwardForce * strength;
        currentSpeed = Mathf.Min(currentSpeed + addedSpeed, maxSpeed);

        // 添加一个小的即时位移，使每次划桨都有明显的前进感
        transform.position += Vector3.forward * 0.1f;
        distanceTraveled += 0.1f;

        // 影响船体摇动
        if (enableRocking)
        {
            // 左右桨影响不同方向的摇动
            float rockImpact = isLeftPaddle ? -paddleRockImpact : paddleRockImpact;
            targetRockAngle += rockImpact;
        }

        if (debugMode)
            Debug.Log($"收到{(isLeftPaddle ? "左" : "右")}桨划动，速度增加到: {currentSpeed:F2}m/s，强度: {strength:F2}");
    }

    // 响应同步划桨事件
    void OnSyncPaddle(float strength)
    {
        // 更新最后划桨时间
        lastPaddleTime = Time.time;

        // 同步划桨提供更多加速
        float addedSpeed = baseForwardForce * strength * 1.5f;
        currentSpeed = Mathf.Min(currentSpeed + addedSpeed, maxSpeed);

        // 同步划桨提供更大的即时位移
        transform.position += Vector3.forward * 0.3f;
        distanceTraveled += 0.3f;

        // 同步划桨平衡船体，减少摇动
        if (enableRocking)
        {
            targetRockAngle = 0;
            currentRockAngle *= 0.5f; // 快速减少当前摇动
        }

        if (debugMode)
            Debug.Log($"同步划桨！速度增加到: {currentSpeed:F2}m/s，强度: {strength:F2}");
    }

    // 增加速度的方法
    public void AddSpeed(float strength)
    {
        // 更新最后划桨时间
        lastPaddleTime = Time.time;

        float addedSpeed = baseForwardForce * strength;
        currentSpeed = Mathf.Min(currentSpeed + addedSpeed, maxSpeed);

        // 添加一个小的即时位移
        transform.position += Vector3.forward * 0.2f;
        distanceTraveled += 0.2f;

        if (debugMode)
            Debug.Log($"速度增加到: {currentSpeed:F2}m/s，强度: {strength:F2}");
    }

    // 减少速度的方法，但不会低于最低速度
    public void ReduceSpeed(float factor)
    {
        currentSpeed *= (1f - factor);

        // 确保速度不低于最低速度
        if (currentSpeed < minSpeed)
            currentSpeed = minSpeed;

        if (debugMode)
            Debug.Log($"速度减少到: {currentSpeed:F2}m/s，因子: {factor:F2}");
    }

    void CompleteRace()
    {
        //uiManager.HideAllMenus();
        // 显示胜利画面
        if (victoryScreen != null)
        {
            victoryScreen.SetActive(true);
            Debug.Log("victory screen set true");
        }
            

        Debug.Log("比赛完成！行程: " + distanceTraveled.ToString("F1") + " / " + totalDistance.ToString("F1"));

        // 通知游戏管理器
        if (gameManager != null)
        {
            gameManager.TogglePause();
            gameManager.OnLevelCompleted(scoreSystem.GetScore());
            Debug.Log("game manager set pause");
        }
    }

    // 获取完成百分比，用于UI显示
    public float GetCompletionPercentage()
    {
        return distanceTraveled / totalDistance;
    }

    // 获取当前速度，用于UI和其他系统
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    // 设置当前速度（用于调试）
    public void SetSpeed(float speed)
    {
        currentSpeed = Mathf.Clamp(speed, minSpeed, maxSpeed);

        if (debugMode)
            Debug.Log($"手动设置速度为: {currentSpeed:F2}m/s");
    }

    // 模拟完成比赛（用于调试）
    public void SimulateRaceCompletion()
    {
        if (!raceCompleted)
        {
            distanceTraveled = totalDistance;
            raceCompleted = true;
            CompleteRace();

            if (debugMode)
                Debug.Log("模拟完成比赛");
        }
    }
}