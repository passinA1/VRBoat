using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ���Ƶ���NPC�Ļ�����Ϊ����ȫ��ͬ�����������ʹ��
/// ��������Ҫ���NPC����һ����Ϊ�ĳ���
/// </summary>
public class NPCPaddleController : MonoBehaviour
{
    [Header("NPC��������")]
    public string npcName = "NPC����";
    public Transform npcBoat; // NPC�Ĵ�����
    public bool isActiveNPC = true; // �Ƿ񼤻��NPC

    [Header("NPC������")]
    public Transform leftPaddle; // ��
    public Transform rightPaddle; // �ҽ�
    public float paddleForwardAmount = 0.5f; // ��ǰ�ƶ���
    public float paddleBackwardAmount = 0.7f; // ���������
    public float paddleAnimSpeed = 7f; // ���������ٶ�
    public float paddleRecoveryTime = 0.1f; // ���ָ�ʱ��

    [Header("ͬ������")]
    public bool useSynchronizedController = true; // �Ƿ�ʹ��ȫ��ͬ��������
    public int syncGroupID = 0; // ͬ����ID����ͬID��NPC����ͬ��

    [Header("����ѡ��")]
    public bool showDebugInfo = false; // �Ƿ���ʾ������Ϣ

    // ˽�б���
    private Vector3 leftPaddleRestPos;
    private Vector3 rightPaddleRestPos;
    private Coroutine paddleCoroutine;
    private bool isCurrentlyPaddling = false;
    private bool lastPaddleWasLeft = false;

    // ȫ��ͬ����������
    private NPCPaddleManager paddleManager;
    private SynchronizedNPCPaddleController syncController;

    void Start()
    {
        // ���潰�ĳ�ʼλ��
        if (leftPaddle) leftPaddleRestPos = leftPaddle.localPosition;
        if (rightPaddle) rightPaddleRestPos = rightPaddle.localPosition;

        // ����ͬ���������������
        paddleManager = FindObjectOfType<NPCPaddleManager>();
        syncController = FindObjectOfType<SynchronizedNPCPaddleController>();

        // ���ʹ��ͬ����������������������Э��
        if (!useSynchronizedController && isActiveNPC)
        {
            StartIndividualPaddling();
        }
        else if (showDebugInfo)
        {
            Debug.Log($"NPC {npcName} ʹ��ȫ��ͬ����������������������Э��");
        }
    }

    void Update()
    {
        // ���ʹ��ͬ����������ȷ������λ���������ͬ��
        if (useSynchronizedController && syncController != null)
        {
            SyncWithGlobalController();
        }
    }

    /// <summary>
    /// ��ȫ�ֿ�����ͬ������λ��
    /// </summary>
    private void SyncWithGlobalController()
    {
        // ȷ��ͬ������������������ӽ�
        if (syncController == null ||
            syncController.npcLeftPaddles.Count == 0 ||
            syncController.npcRightPaddles.Count == 0)
            return;

        // ����Լ��Ľ��Ƿ�����ӵ�ͬ��������
        if (!syncController.npcLeftPaddles.Contains(leftPaddle) ||
            !syncController.npcRightPaddles.Contains(rightPaddle))
        {
            if (showDebugInfo)
                Debug.LogWarning($"NPC {npcName} �Ľ�δ��ӵ�ͬ���������У�");
            return;
        }

        // ��ʹ��ͬ��������ʱ������Ҫ�������
        // ͬ����������ֱ�ӿ������н���λ��
    }

    /// <summary>
    /// ������������Э�̣�����ʹ��ͬ��������ʱ��
    /// </summary>
    public void StartIndividualPaddling()
    {
        if (useSynchronizedController)
        {
            Debug.LogWarning($"NPC {npcName} ʹ��ȫ��ͬ�����������޷�������������Э��");
            return;
        }

        if (paddleCoroutine != null)
        {
            StopCoroutine(paddleCoroutine);
        }

        paddleCoroutine = StartCoroutine(IndividualPaddlingRoutine());
    }

    /// <summary>
    /// ֹͣ��������
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
    /// ��������Э�̣��ڲ�ʹ��ͬ��������ʱ��
    /// </summary>
    private IEnumerator IndividualPaddlingRoutine()
    {
        isCurrentlyPaddling = true;
        float paddleInterval = 0.7f; // Ĭ�ϻ������

        // ����й��������ӹ�������ȡ���
        if (paddleManager != null)
        {
            paddleInterval = paddleManager.basePaddleInterval;
        }

        while (isCurrentlyPaddling)
        {
            // ����ʹ�����ҽ�
            bool useLeftPaddle = !lastPaddleWasLeft;

            // ִ�л�������
            yield return StartCoroutine(PerformPaddleStroke(useLeftPaddle));

            // �������ʹ�õĽ�
            lastPaddleWasLeft = useLeftPaddle;

            // �ȴ���һ�λ���
            yield return new WaitForSeconds(paddleInterval);
        }
    }

    /// <summary>
    /// ִ�е�����������
    /// </summary>
    private IEnumerator PerformPaddleStroke(bool isLeft)
    {
        Transform paddle = isLeft ? leftPaddle : rightPaddle;
        Vector3 restPos = isLeft ? leftPaddleRestPos : rightPaddleRestPos;

        if (paddle == null)
        {
            Debug.LogWarning($"NPC {npcName} ��{(isLeft ? "��" : "��")}��Ϊ�գ�");
            yield break;
        }

        // ���㶯��ʱ��
        float strokeTime = 1f / paddleAnimSpeed;

        // 1. ��ǰ�ƶ���
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

        // ȷ������ǰ��λ��
        paddle.localPosition = forwardPos;

        // 2. ���������
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

        // ȷ���������λ��
        paddle.localPosition = backwardPos;

        // 3. ���ٻָ�����ʼλ��
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

        // ȷ���ص���ȷ����Ϣλ��
        paddle.localPosition = restPos;
    }

    /// <summary>
    /// ����NPC�Ƿ񼤻�
    /// </summary>
    public void SetNPCActive(bool active)
    {
        isActiveNPC = active;

        // ֻ���ڶ���ģʽ�²ſ���Э��
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
    /// �������ý�����ʼλ��
    /// </summary>
    public void ResetPaddles()
    {
        if (leftPaddle != null)
            leftPaddle.localPosition = leftPaddleRestPos;

        if (rightPaddle != null)
            rightPaddle.localPosition = rightPaddleRestPos;
    }

    /// <summary>
    /// �����Ƿ�ʹ��ȫ��ͬ��������
    /// </summary>
    public void SetUseSynchronizedController(bool useSync)
    {
        // ���״̬�ı�
        if (useSynchronizedController != useSync)
        {
            useSynchronizedController = useSync;

            // ����л�������ģʽ��������������
            if (!useSync && isActiveNPC)
            {
                StopIndividualPaddling(); // ȷ��ֹ֮ͣǰ��Э��
                StartIndividualPaddling();
            }
            // ����л���ͬ��ģʽ��ֹͣ��������
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