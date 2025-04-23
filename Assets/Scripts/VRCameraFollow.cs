using UnityEngine;

public class VRCameraFollow : MonoBehaviour
{
    public Transform target; // �����Ŀ�� (DragonBoat)
    public float smoothSpeed = 0.125f; // ƽ�������ٶ�
    public Vector3 offset = new Vector3(0, 2, -2); // ���Ŀ���ƫ����
    public bool lockYRotation = true; // ����Y����ת��������ѣ

    [Header("ҡ������")]
    public bool transferBoatRocking = true; // �Ƿ񴫵ݴ�ֻҡ��
    public float rockingIntensity = 0.2f; // ҡ��ǿ�ȣ������ǿ������ѣ

    [Header("VR/��VR����")]
    public bool forceNonVRMode = true; // ǿ��ʹ�÷�VRģʽ�����ڲ��ԣ�
    public Transform xrOrigin; // XR Origin��Transform
    public Transform standardCamera; // ��VRģʽ�µı�׼���
    public Vector3 cameraOffset = new Vector3(0, 1.6f, 0); // ����������XR Origin��ƫ��

    [Header("��������")]
    public bool debugMode = false; // ����ģʽ
    public KeyCode resetKey = KeyCode.R; // �������λ�ü�
    public bool showTargetGizmo = true; // ��ʾĿ������

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private DragonBoatMovement boatMovement;
    private bool isVRMode = false;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        // ���VRģʽ
        isVRMode = !forceNonVRMode && CheckVRAvailability();

        if (isVRMode)
        {
            SetupVRMode();
        }
        else
        {
            SetupNonVRMode();
        }

        // ��¼��ʼλ�ú���ת
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
            Debug.Log($"VRCameraFollow��ʼ����ɣ�ģʽ: {(isVRMode ? "VR" : "��VR")}");
    }

    // ���VR�Ƿ����
    private bool CheckVRAvailability()
    {
        // ����򵥼��XR�豸�Ƿ����
        if (UnityEngine.XR.XRSettings.isDeviceActive)
        {
            if (debugMode)
                Debug.Log("��⵽VR�豸��ʹ��VRģʽ");
            return true;
        }

        if (debugMode)
            Debug.Log("δ��⵽VR�豸��ʹ�÷�VRģʽ");
        return false;
    }

    // ����VRģʽ
    private void SetupVRMode()
    {
        // ���û��ָ��XR Origin�������Զ�����
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
                Debug.Log("�Զ��ҵ�XR Origin: " + xrOrigin.name);
            }
            else
            {
                Debug.LogWarning("δ�ҵ�XR Origin��ʹ�ñ�׼���ģʽ");
                isVRMode = false;
                SetupNonVRMode();
                return;
            }
        }

        // ����б�׼�����������
        if (standardCamera != null)
            standardCamera.gameObject.SetActive(false);
    }

    // ���÷�VRģʽ
    private void SetupNonVRMode()
    {
        // ���û��ָ����׼�����ʹ�������
        if (standardCamera == null)
        {
            // �ȳ��Բ��Ҵ���"StandardCamera"��ǩ�Ķ���
            GameObject standardCamObj = GameObject.FindGameObjectWithTag("StandardCamera");

            // ���û�ҵ���ʹ�������
            if (standardCamObj == null && mainCamera != null)
            {
                standardCamera = mainCamera.transform;
                Debug.Log("ʹ���������Ϊ��׼���");
            }
            else if (standardCamObj != null)
            {
                standardCamera = standardCamObj.transform;
                Debug.Log("�Զ��ҵ���׼���: " + standardCamera.name);
            }
            else
            {
                Debug.LogWarning("δ�ҵ����õ�����������������޷���������");
            }
        }

        // ȷ����׼�������
        if (standardCamera != null)
            standardCamera.gameObject.SetActive(true);
    }

    void Update()
    {
        // �����������
        if (debugMode && Input.GetKeyDown(resetKey))
        {
            ResetToInitialPosition();
            Debug.Log("���������λ��");
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

        // ��ʾ��������
        if (debugMode && showTargetGizmo && target != null)
        {
            Debug.DrawLine(
                isVRMode ? xrOrigin.position : (standardCamera != null ? standardCamera.position : transform.position),
                target.position,
                Color.yellow
            );
        }
    }

    // ����Ŀ�����ȱʧ
    private void FindTargetIfMissing()
    {
        // �����ҵ�DragonBoat����
        DragonBoatMovement boatMovement = FindObjectOfType<DragonBoatMovement>();
        if (boatMovement != null)
        {
            target = boatMovement.transform;
            this.boatMovement = boatMovement;

            if (debugMode)
                Debug.Log("�Զ��ҵ�����Ŀ��: " + target.name);
        }
        else
        {
            if (debugMode)
                Debug.LogWarning("δ�ҵ�����Ŀ�꣬���ֶ�����!");
        }
    }

    // ����VR���λ��
    private void UpdateVRCamera()
    {
        if (xrOrigin == null) return;

        // ����XR Origin��Ŀ��λ��
        Vector3 desiredPosition = target.position + offset;

        // ƽ���ƶ�XR Origin��Ŀ��λ��
        Vector3 smoothedPosition = Vector3.Lerp(xrOrigin.position, desiredPosition, smoothSpeed);
        xrOrigin.position = smoothedPosition;

        // ���������Y����ת��������XR Origin����Ŀ��
        if (!lockYRotation)
        {
            Vector3 lookDirection = target.position - xrOrigin.position;
            lookDirection.y = 0; // ����ˮƽ����

            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                xrOrigin.rotation = Quaternion.Slerp(xrOrigin.rotation, targetRotation, smoothSpeed);
            }
        }

        // ���ݴ�ֻҡ��
        if (transferBoatRocking && boatMovement != null && boatMovement.enableRocking)
        {
            // ��ȡ��ֻ��ǰ�ĺ�����б
            float boatRoll = target.rotation.eulerAngles.z;
            if (boatRoll > 180) boatRoll -= 360; // ��׼����-180��180��Χ

            // ���㻺�͵�ҡ��Ч��
            Vector3 currentRotation = xrOrigin.rotation.eulerAngles;
            // ���ֵ�ǰ��x��y��ת��ֻ����z�ᣨ������ת
            currentRotation.z = Mathf.Lerp(currentRotation.z, boatRoll * rockingIntensity, smoothSpeed * 0.5f);

            // Ӧ��ҡ��
            xrOrigin.rotation = Quaternion.Euler(currentRotation);
        }
    }

    // ���±�׼���λ��
    private void UpdateStandardCamera()
    {
        Transform cameraToMove = standardCamera != null ? standardCamera : transform;

        // ���������Ŀ��λ��
        Vector3 desiredPosition = target.position + offset;

        // ƽ���ƶ������Ŀ��λ��
        Vector3 smoothedPosition = Vector3.Lerp(cameraToMove.position, desiredPosition, smoothSpeed);
        cameraToMove.position = smoothedPosition;

        // ���ʼ�ճ���Ŀ��
        if (target != null)
        {
            Vector3 lookDirection = target.position - cameraToMove.position;

            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

                // �������Y����ת��ֻ��תY��
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

        // ���ݴ�ֻҡ��
        if (transferBoatRocking && boatMovement != null && boatMovement.enableRocking)
        {
            // ��ȡ��ֻ��ǰ�ĺ�����б
            float boatRoll = target.rotation.eulerAngles.z;
            if (boatRoll > 180) boatRoll -= 360; // ��׼����-180��180��Χ

            // ���㻺�͵�ҡ��Ч��
            Vector3 currentRotation = cameraToMove.rotation.eulerAngles;
            // ���ֵ�ǰ��x��y��ת��ֻ����z�ᣨ������ת
            currentRotation.z = Mathf.Lerp(currentRotation.z, boatRoll * rockingIntensity, smoothSpeed * 0.5f);

            // Ӧ��ҡ��
            cameraToMove.rotation = Quaternion.Euler(currentRotation);
        }
    }

    // �����������ʼλ��
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

    // �л�VR/��VRģʽ�����ڲ��ԣ�
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

        // ����λ��
        ResetToInitialPosition();

        if (debugMode)
            Debug.Log($"�л���{(isVRMode ? "VR" : "��VR")}ģʽ");
    }
}