using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.XR;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(PlayerInput))]
public class PaddleController : MonoBehaviour
{
    [Header("������")]
    public Transform leftPaddle;
    public Transform rightPaddle;
    public float paddleForwardAmount = 0.5f; // ������ǰ�ƶ���
    public float paddleBackwardAmount = 0.7f; // �������������
    public float paddleAnimSpeed = 7f; // ���������ٶ�
    public float paddleRecoveryTime = 0.1f; // ���ָ�ʱ��

    [Header("�ֱ���������")]
    public float velocityThreshold = 1.5f; // ������������С�ٶ�(m/s)
    public float cooldownTime = 0.3f; // ������ȴʱ��
    // ����ϵͳ���
    private PlayerInput playerInput;
    private InputAction leftPaddleAction;
    private InputAction rightPaddleAction;
    private InputAction syncPaddleAction;

    // �ֱ��ٶȼ��
    private Vector3 leftControllerLastPos;
    private Vector3 rightControllerLastPos;
    private Vector3 leftControllerVelocity;
    private Vector3 rightControllerVelocity;

    // �ֱ�����
    private ActionBasedController leftController;
    private ActionBasedController rightController;


    [Header("�����ж�����")]
    public float perfectTiming = 0.1f; // �����ж�����(��)
    public float goodTiming = 0.3f; // �����ж�����(��)
    public Transform leftFlagMarker; // �������ж���
    public Transform rightFlagMarker; // �������ж���
    public Transform judgmentLine; // �ж���

    [Header("ģ���������")]
    public bool forceKeyboardMode = true; // ǿ��ʹ�ü���ģʽ�����ڲ��ԣ�
    /*public KeyCode leftPaddleKey = KeyCode.Q;
    public KeyCode rightPaddleKey = KeyCode.E;
    public KeyCode syncPaddleKey = KeyCode.Space;
    public KeyCode perfectPaddleKey = KeyCode.LeftShift; // ģ�����������İ���*/

    [Header("����ѡ��")]
    public bool showDebugInfo = true; // ��ʾ������Ϣ
    public bool randomizePaddleStrength = true; // ������������ȣ�ģ����ʵ�ָУ�
    public float paddleDistanceThreshold = 0.35f; // ����������ֵ�����������е�������λ:�ף�

    // �����¼�
    public delegate void PaddleActionEvent(bool isLeftPaddle, float strength);
    public static event PaddleActionEvent OnPaddleAction;

    // ˫��ͬ���¼�
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


    // ����״̬
    private enum PaddleState { Ready, Forward, Backward, Recovering }
    private PaddleState leftPaddleState = PaddleState.Ready;
    private PaddleState rightPaddleState = PaddleState.Ready;

    // ���÷���ϵͳ
    private FeedbackSystem feedbackSystem;
    private ScoreSystem scoreSystem;
    
   
    void Start()
    {
        // ���潰�ĳ�ʼλ��
        if (leftPaddle) leftPaddleRestPos = leftPaddle.localPosition;
        if (rightPaddle) rightPaddleRestPos = rightPaddle.localPosition;

        // ��ȡ����ϵͳ����
        feedbackSystem = FindObjectOfType<FeedbackSystem>();
        scoreSystem = FindObjectOfType<ScoreSystem>();

        // ����Ƿ�����VR
        CheckVRAvailability();

        Debug.Log($"PaddleController��ʼ����ɣ���ǰģʽ: {(isVRMode && !forceKeyboardMode ? "VR" : "����")}");

        // ��ʼ������ϵͳ
        InitializeInputSystem();
        FindXRControllers();

    }

    void Update()
    {

        // ��ʾ������Ϣ
        if (showDebugInfo)
        {
            Debug.DrawRay(leftPaddle.position, Vector3.forward * 0.5f, Color.blue);
            Debug.DrawRay(rightPaddle.position, Vector3.forward * 0.5f, Color.red);
        }

        CalculateControllerVelocity();
        HandleAutomaticPaddleInput();
    }


    #region ����ϵͳ��ʼ��
    private void InitializeInputSystem()
    {
        playerInput = GetComponent<PlayerInput>();

        // ��������Action����
        leftPaddleAction = playerInput.actions["LeftPaddle"];
        rightPaddleAction = playerInput.actions["RightPaddle"];
        syncPaddleAction = playerInput.actions["SyncPaddle"];

        // �������¼�
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

    #region �ֱ��ٶȼ���
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

            // ������ǰ�Ӷ����ٶȷ�����������ǰ�ǿ�������Z�᷽��
            float forwardSpeed = Vector3.Dot(velocity, isLeft ?
                leftController.transform.forward :
                rightController.transform.forward);

            return Mathf.Abs(forwardSpeed) > velocityThreshold;
        }
        return false;
    }
    #endregion


    #region ���봦��
    private void HandleAutomaticPaddleInput()
    {
        if (isVRMode)
        {
            // �Զ������������Ӷ�
            if (CheckSwingVelocity(true))
            {
                Debug.Log("�Ӷ����ֱ�");
                OnPaddleInput(true);
            }

            // �Զ�����ҿ������Ӷ�
            if (CheckSwingVelocity(false))
            {
                Debug.Log("�Ӷ����ֱ�");
                OnPaddleInput(false);
            }
        }
    }

    private void OnPaddleInput(bool isLeft)
    {
        if ((isLeft && leftPaddleMoving) || (!isLeft && rightPaddleMoving))
            return;

        bool isPerfect = CheckPerfectTiming(isLeft); ;

        // VRģʽ�¼������ʱ��
        if (isVRMode)
        {
            isPerfect = CheckPerfectTiming(isLeft);
        }
        else // ����ģʽʹ��shift��
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
        // ��ȡ��һ֧����ʱ��
        float otherPaddleTime = isLeft ? lastRightPaddleTime : lastLeftPaddleTime;
        float currentTime = Time.time;

        // ����Ƿ���ͬ��ʱ�䴰���ڣ�0.2�룩
        if (otherPaddleTime > 0 && Mathf.Abs(currentTime - otherPaddleTime) < 0.2f)
        {
            // ����ͬ������
            bool isPerfect = isVRMode ?
                CheckBothPerfectTiming() :
                Keyboard.current.leftShiftKey.isPressed;

            TriggerSyncPaddle(isPerfect);

            // ����ʱ���¼��ֹ�ظ�����
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

    #region �����ж���ǿ
    private bool CheckPerfectTiming(bool isLeft)
    {
        if (!isVRMode) return false;

        // ʹ���ֱ����ٶ��ж�����ʱ��
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


    // ���VR�Ƿ����
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
                Debug.Log("�Ѽ�⵽VR�豸������VR����ģʽ");
                return;
            }
        }

        isVRMode = false;
        Debug.Log("δ��⵽VR�豸��ʹ�ü���ģ��ģʽ");
    }




    // ��ȡ��������
    private float GetPaddleStrength(bool isPerfect)
    {
        if (isPerfect)
            return 2.0f; // ������������

        if (randomizePaddleStrength)
        {
            // �������õĻ���������ֵ���������Χ
            float minStrength = 1.0f;
            float maxStrength = 1.0f + paddleDistanceThreshold;
            return Random.Range(minStrength, maxStrength);
        }

        return 1.5f; // Ĭ������
    }

    // ����˫��ͬ���¼�
    private void TriggerSyncPaddle(bool isPerfect)
    {
        //Debug.Log("����˫��ͬ��!");

        // ����ͬ�������¼���ǿ��Ϊ2.5���ṩ��ǿ���ƶ���
        float strength = isPerfect ? 3.0f : 2.5f;
        if (OnSyncPaddle != null)
            OnSyncPaddle(strength);

        // ��������ͬ����Ч
        if (feedbackSystem != null)
        {
            feedbackSystem.PlayComboEffect(2);
        }
    }

    // ͬ��˫������Э��
    IEnumerator SyncPaddleStroke(bool isPerfect = false)
    {
        // ֹͣ�κ����ڽ��еĻ�������
        if (leftPaddleCoroutine != null)
            StopCoroutine(leftPaddleCoroutine);

        if (rightPaddleCoroutine != null)
            StopCoroutine(rightPaddleCoroutine);

        leftPaddleMoving = true;
        rightPaddleMoving = true;

        // ���㶯��ʱ��
        float strokeTime = 1f / paddleAnimSpeed;

        // 1. ��ǰ�ƶ���
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

        // 2. ���������
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

            // �ڻ����������ھʹ���ǰ���¼���ʹ��Ӧ��Ѹ��
            if (progress >= 0.3f && !eventTriggered)
            {
                eventTriggered = true;

                // ����ͬ�������¼�
                float strength = isPerfect ? 3.0f : 2.5f;
                if (OnSyncPaddle != null)
                    OnSyncPaddle(strength);

                // ���Ż�����Ч����Ч
                if (feedbackSystem != null)
                {
                    feedbackSystem.PlayPaddleEffect(leftPaddle.position, isPerfect);
                    feedbackSystem.PlayPaddleEffect(rightPaddle.position, isPerfect);
                }
            }

            yield return null;
        }

        // 3. ���ٻָ�����ʼλ��
        t = 0;
        while (t < paddleRecoveryTime)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / paddleRecoveryTime);
            leftPaddle.localPosition = Vector3.Lerp(leftBackwardPos, leftPaddleRestPos, progress);
            rightPaddle.localPosition = Vector3.Lerp(rightBackwardPos, rightPaddleRestPos, progress);
            yield return null;
        }

        // ȷ���ص���ȷ����Ϣλ��
        leftPaddle.localPosition = leftPaddleRestPos;
        rightPaddle.localPosition = rightPaddleRestPos;

        // ��������״̬
        leftPaddleMoving = false;
        rightPaddleMoving = false;
    }

    IEnumerator PaddleStroke(bool isLeft, bool isPerfect = false, float strength = 1.5f)
    {
        Transform paddle = isLeft ? leftPaddle : rightPaddle;
        Vector3 restPos = isLeft ? leftPaddleRestPos : rightPaddleRestPos;

        if (paddle == null)
        {
            Debug.LogError(isLeft ? "�󽰶���Ϊ�գ�" : "�ҽ�����Ϊ�գ�");
            yield break;
        }

        // �������ڻ���״̬
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

        // ���㶯��ʱ��
        float strokeTime = 1f / paddleAnimSpeed;

        // 1. ��ǰ�ƶ���
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

        // ����״̬
        if (isLeft)
            leftPaddleState = PaddleState.Backward;
        else
            rightPaddleState = PaddleState.Backward;

        // 2. ���������
        Vector3 backwardPos = restPos - new Vector3(0, 0, paddleBackwardAmount);
        float backwardTime = strokeTime / 2;
        t = 0;

        bool eventTriggered = false;

        while (t < backwardTime)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / backwardTime);
            paddle.localPosition = Vector3.Lerp(forwardPos, backwardPos, progress);

            // �ڻ����������ھʹ���ǰ���¼���ʹ��Ӧ��Ѹ��
            if (progress >= 0.3f && !eventTriggered)
            {
                eventTriggered = true;

                // ���������¼�
                if (OnPaddleAction != null)
                    OnPaddleAction(isLeft, strength);

                // ���Ż�����Ч����Ч
                if (feedbackSystem != null)
                {
                    feedbackSystem.PlayPaddleEffect(paddle.position, isPerfect);
                }
            }

            yield return null;
        }

        // ����״̬
        if (isLeft)
            leftPaddleState = PaddleState.Recovering;
        else
            rightPaddleState = PaddleState.Recovering;

        // 3. ���ٻָ�����ʼλ��
        t = 0;
        while (t < paddleRecoveryTime)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / paddleRecoveryTime);
            paddle.localPosition = Vector3.Lerp(backwardPos, restPos, progress);
            yield return null;
        }

        // ȷ���ص���ȷ����Ϣλ��
        paddle.localPosition = restPos;

        // ��������״̬
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

    // ���û���������ֵ
    public void SetPaddleDistanceThreshold(float distance)
    {
        // �����ں���Χ�� (10-50cm)
        paddleDistanceThreshold = Mathf.Clamp(distance, 0.1f, 0.5f);

        if (showDebugInfo)
            Debug.Log($"����������ֵ����Ϊ: {paddleDistanceThreshold * 100}cm");
    }

    // ģ������÷��� - ͨ�����봥������
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

            // ����Ƿ�˫��ͬ�� (0.2����)
            float lastOtherTime = isLeft ? lastRightPaddleTime : lastLeftPaddleTime;
            if (lastOtherTime > 0 && Time.time - lastOtherTime < 0.2f)
            {
                TriggerSyncPaddle(isPerfect);
            }
        }
    }

    // ģ������÷��� - ͨ�����봥��ͬ������
    public void SimulateSyncPaddle(bool isPerfect = false)
    {
        if (!leftPaddleMoving && !rightPaddleMoving)
        {
            StartCoroutine(SyncPaddleStroke(isPerfect));
        }
    }

    void OnDestroy()
    {
        // ��������
        if (leftPaddleAction != null)
            leftPaddleAction.started -= ctx => OnPaddleInput(true);

        if (rightPaddleAction != null)
            rightPaddleAction.started -= ctx => OnPaddleInput(false);

        if (syncPaddleAction != null)
            syncPaddleAction.started -= ctx => OnSyncPaddleInput();
    }
}