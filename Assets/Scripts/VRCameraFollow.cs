using UnityEngine;

public class VRCameraFollow : MonoBehaviour
{
    public Transform target; // 跟随的目标 (DragonBoat)
    public float smoothSpeed = 0.125f; // 平滑跟随速度
    public Vector3 offset = new Vector3(0, 2, -2); // 相对目标的偏移量
    public bool lockYRotation = true; // 锁定Y轴旋转，避免晕眩

    [Header("摇动设置")]
    public bool transferBoatRocking = true; // 是否传递船只摇动
    public float rockingIntensity = 0.2f; // 摇动强度，避免过强导致晕眩

    [Header("VR/非VR设置")]
    public bool forceNonVRMode = true; // 强制使用非VR模式（用于测试）
    public Transform xrOrigin; // XR Origin的Transform
    public Transform standardCamera; // 非VR模式下的标准相机
    public Vector3 cameraOffset = new Vector3(0, 1.6f, 0); // 摄像机相对于XR Origin的偏移

    [Header("调试设置")]
    public bool debugMode = false; // 调试模式
    public KeyCode resetKey = KeyCode.R; // 重置相机位置键
    public bool showTargetGizmo = true; // 显示目标连线

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private DragonBoatMovement boatMovement;
    private bool isVRMode = false;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        // 检测VR模式
        isVRMode = !forceNonVRMode && CheckVRAvailability();

        if (isVRMode)
        {
            SetupVRMode();
        }
        else
        {
            SetupNonVRMode();
        }

        // 记录初始位置和旋转
        if (isVRMode && xrOrigin != null)
        {
            initialPosition = xrOrigin.position;
            initialRotation = xrOrigin.rotation;
        }
        else if (standardCamera != null)
        {
            initialPosition = standardCamera.position;
            initialRotation = standardCamera.rotation;
        }
        else
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        if (debugMode)
            Debug.Log($"VRCameraFollow初始化完成，模式: {(isVRMode ? "VR" : "非VR")}");
    }

    // 检测VR是否可用
    private bool CheckVRAvailability()
    {
        // 这里简单检查XR设备是否存在
        if (UnityEngine.XR.XRSettings.isDeviceActive)
        {
            if (debugMode)
                Debug.Log("检测到VR设备，使用VR模式");
            return true;
        }

        if (debugMode)
            Debug.Log("未检测到VR设备，使用非VR模式");
        return false;
    }

    // 设置VR模式
    private void SetupVRMode()
    {
        // 如果没有指定XR Origin，尝试自动查找
        if (xrOrigin == null)
        {
            GameObject xrOriginObj = GameObject.FindGameObjectWithTag("XROrigin");
            if (xrOriginObj == null)
            {
                xrOriginObj = GameObject.Find("XR Origin");
            }

            if (xrOriginObj != null)
            {
                xrOrigin = xrOriginObj.transform;
                Debug.Log("自动找到XR Origin: " + xrOrigin.name);
            }
            else
            {
                Debug.LogWarning("未找到XR Origin，使用标准相机模式");
                isVRMode = false;
                SetupNonVRMode();
                return;
            }
        }

        // 如果有标准相机，禁用它
        if (standardCamera != null)
            standardCamera.gameObject.SetActive(false);
    }

    // 设置非VR模式
    private void SetupNonVRMode()
    {
        // 如果没有指定标准相机，使用主相机
        if (standardCamera == null)
        {
            // 先尝试查找带有"StandardCamera"标签的对象
            GameObject standardCamObj = GameObject.FindGameObjectWithTag("StandardCamera");

            // 如果没找到，使用主相机
            if (standardCamObj == null && mainCamera != null)
            {
                standardCamera = mainCamera.transform;
                Debug.Log("使用主相机作为标准相机");
            }
            else if (standardCamObj != null)
            {
                standardCamera = standardCamObj.transform;
                Debug.Log("自动找到标准相机: " + standardCamera.name);
            }
            else
            {
                Debug.LogWarning("未找到可用的相机，相机跟随可能无法正常工作");
            }
        }

        // 确保标准相机启用
        if (standardCamera != null)
            standardCamera.gameObject.SetActive(true);
    }

    void Update()
    {
        // 处理调试输入
        if (debugMode && Input.GetKeyDown(resetKey))
        {
            ResetToInitialPosition();
            Debug.Log("已重置相机位置");
        }
    }

    void LateUpdate()
    {
        if (target == null)
        {
            FindTargetIfMissing();
            if (target == null) return;
        }

        if (isVRMode)
        {
            UpdateVRCamera();
        }
        else
        {
            UpdateStandardCamera();
        }

        // 显示调试连线
        if (debugMode && showTargetGizmo && target != null)
        {
            Debug.DrawLine(
                isVRMode ? xrOrigin.position : (standardCamera != null ? standardCamera.position : transform.position),
                target.position,
                Color.yellow
            );
        }
    }

    // 查找目标如果缺失
    private void FindTargetIfMissing()
    {
        // 尝试找到DragonBoat对象
        DragonBoatMovement boatMovement = FindObjectOfType<DragonBoatMovement>();
        if (boatMovement != null)
        {
            target = boatMovement.transform;
            this.boatMovement = boatMovement;

            if (debugMode)
                Debug.Log("自动找到跟随目标: " + target.name);
        }
        else
        {
            if (debugMode)
                Debug.LogWarning("未找到跟随目标，请手动设置!");
        }
    }

    // 更新VR相机位置
    private void UpdateVRCamera()
    {
        if (xrOrigin == null) return;

        // 计算XR Origin的目标位置
        Vector3 desiredPosition = target.position + offset;

        // 平滑移动XR Origin到目标位置
        Vector3 smoothedPosition = Vector3.Lerp(xrOrigin.position, desiredPosition, smoothSpeed);
        xrOrigin.position = smoothedPosition;

        // 如果不锁定Y轴旋转，可以让XR Origin朝向目标
        if (!lockYRotation)
        {
            Vector3 lookDirection = target.position - xrOrigin.position;
            lookDirection.y = 0; // 保持水平方向

            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                xrOrigin.rotation = Quaternion.Slerp(xrOrigin.rotation, targetRotation, smoothSpeed);
            }
        }

        // 传递船只摇动
        if (transferBoatRocking && boatMovement != null && boatMovement.enableRocking)
        {
            // 获取船只当前的横向倾斜
            float boatRoll = target.rotation.eulerAngles.z;
            if (boatRoll > 180) boatRoll -= 360; // 标准化到-180到180范围

            // 计算缓和的摇动效果
            Vector3 currentRotation = xrOrigin.rotation.eulerAngles;
            // 保持当前的x和y旋转，只调整z轴（横向）旋转
            currentRotation.z = Mathf.Lerp(currentRotation.z, boatRoll * rockingIntensity, smoothSpeed * 0.5f);

            // 应用摇动
            xrOrigin.rotation = Quaternion.Euler(currentRotation);
        }
    }

    // 更新标准相机位置
    private void UpdateStandardCamera()
    {
        Transform cameraToMove = standardCamera != null ? standardCamera : transform;

        // 计算相机的目标位置
        Vector3 desiredPosition = target.position + offset;

        // 平滑移动相机到目标位置
        Vector3 smoothedPosition = Vector3.Lerp(cameraToMove.position, desiredPosition, smoothSpeed);
        cameraToMove.position = smoothedPosition;

        // 相机始终朝向目标
        if (target != null)
        {
            Vector3 lookDirection = target.position - cameraToMove.position;

            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

                // 如果锁定Y轴旋转，只旋转Y轴
                if (lockYRotation)
                {
                    targetRotation = Quaternion.Euler(
                        cameraToMove.rotation.eulerAngles.x,
                        targetRotation.eulerAngles.y,
                        cameraToMove.rotation.eulerAngles.z
                    );
                }

                cameraToMove.rotation = Quaternion.Slerp(cameraToMove.rotation, targetRotation, smoothSpeed);
            }
        }

        // 传递船只摇动
        if (transferBoatRocking && boatMovement != null && boatMovement.enableRocking)
        {
            // 获取船只当前的横向倾斜
            float boatRoll = target.rotation.eulerAngles.z;
            if (boatRoll > 180) boatRoll -= 360; // 标准化到-180到180范围

            // 计算缓和的摇动效果
            Vector3 currentRotation = cameraToMove.rotation.eulerAngles;
            // 保持当前的x和y旋转，只调整z轴（横向）旋转
            currentRotation.z = Mathf.Lerp(currentRotation.z, boatRoll * rockingIntensity, smoothSpeed * 0.5f);

            // 应用摇动
            cameraToMove.rotation = Quaternion.Euler(currentRotation);
        }
    }

    // 重置相机到初始位置
    public void ResetToInitialPosition()
    {
        if (isVRMode && xrOrigin != null)
        {
            xrOrigin.position = initialPosition;
            xrOrigin.rotation = initialRotation;
        }
        else if (standardCamera != null)
        {
            standardCamera.position = initialPosition;
            standardCamera.rotation = initialRotation;
        }
        else
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }
    }

    // 切换VR/非VR模式（用于测试）
    public void ToggleVRMode()
    {
        isVRMode = !isVRMode;

        if (isVRMode)
        {
            SetupVRMode();
        }
        else
        {
            SetupNonVRMode();
        }

        // 重置位置
        ResetToInitialPosition();

        if (debugMode)
            Debug.Log($"切换到{(isVRMode ? "VR" : "非VR")}模式");
    }
}