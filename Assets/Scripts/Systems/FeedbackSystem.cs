using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class FeedbackSystem : MonoBehaviour
{
    [Header("�ı�����")]
    public TextMeshProUGUI feedbackText;
    public float feedbackDuration = 0.8f;
    public float moveDistance = 30f; // �ı����ƾ���
    public int maxQueuedFeedbacks = 3; // ����Ŷӷ�������

    [Header("����Ч��")]
    public ParticleSystem paddleParticles;
    public ParticleSystem perfectParticles;
    public ParticleSystem fullPowerParticles;
    public ParticleSystem comboParticles;
    public float particleIntensityMultiplier = 1.0f; // ����ǿ�ȱ���

    [Header("��Ч")]
    public AudioClip paddleSound;
    public AudioClip perfectSound;
    public AudioClip missSound;
    public AudioClip fullPowerSound;
    public AudioClip comboSound;
    public float volumeMultiplier = 1.0f; // ��������

    [Header("���ֶ���")]
    public Animator drummerAnimator;
    public string drumHitTriggerName = "DrumHit";
    public string perfectHitTriggerName = "PerfectHit";

    [Header("��������")]
    public bool debugMode = false; // ����ģʽ
    public bool muteAudio = false; // ����
    public bool disableParticles = false; // ��������
    public KeyCode testPerfectKey = KeyCode.Alpha1; // ������������
    public KeyCode testGoodKey = KeyCode.Alpha2; // �������÷���
    public KeyCode testMissKey = KeyCode.Alpha3; // ����Miss����
    public KeyCode testComboKey = KeyCode.Alpha4; // ������������
    public KeyCode testFullPowerKey = KeyCode.Alpha5; // ����ȫ������

    private AudioSource audioSource;
    private Queue<FeedbackInfo> feedbackQueue = new Queue<FeedbackInfo>();
    private bool isFeedbackPlaying = false;
    private RectTransform feedbackRectTransform;

    // ������Ϣ��
    private class FeedbackInfo
    {
        public string message;
        public Color color;

        public FeedbackInfo(string message, Color color)
        {
            this.message = message;
            this.color = color;
        }
    }

    void Start()
    {
        // ��ȡ�������ƵԴ
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.volume = volumeMultiplier;

        // ��ȡ�����ı���RectTransform
        if (feedbackText != null)
        {
            feedbackRectTransform = feedbackText.rectTransform;
            feedbackText.gameObject.SetActive(false);
        }

        Debug.Log("FeedbackSystem��ʼ�����");
    }

    void Update()
    {
        // ���԰�������
        if (debugMode)
        {
            if (Input.GetKeyDown(testPerfectKey))
            {
                PlayPaddleEffect(Vector3.zero, true);
            }

            if (Input.GetKeyDown(testGoodKey))
            {
                PlayPaddleEffect(Vector3.zero, false);
            }

            if (Input.GetKeyDown(testMissKey))
            {
                ShowMissFeedback();
            }

            if (Input.GetKeyDown(testComboKey))
            {
                PlayComboEffect(Random.Range(5, 20));
            }

            if (Input.GetKeyDown(testFullPowerKey))
            {
                PlayFullPowerEffect();
            }
        }

        // ����������
        ProcessFeedbackQueue();
    }

    // ����������
    private void ProcessFeedbackQueue()
    {
        if (!isFeedbackPlaying && feedbackQueue.Count > 0 && feedbackText != null)
        {
            FeedbackInfo nextFeedback = feedbackQueue.Dequeue();
            StartCoroutine(TextFeedbackCoroutine(nextFeedback.message, nextFeedback.color));
        }
    }

    public void ShowTextFeedback(string message, Color color)
    {
        if (feedbackText == null) return;

        // ���ƶ��г��ȣ���ֹ���෴���ѻ�
        if (feedbackQueue.Count >= maxQueuedFeedbacks)
        {
            if (debugMode)
                Debug.LogWarning("����������������������: " + message);
            return;
        }

        // ��ӵ�����
        feedbackQueue.Enqueue(new FeedbackInfo(message, color));

        if (debugMode)
            Debug.Log("����ı�����: " + message);
    }

    IEnumerator TextFeedbackCoroutine(string message, Color color)
    {
        isFeedbackPlaying = true;

        // ����λ�õ�����λ��
        feedbackRectTransform.anchoredPosition = Vector2.zero;

        // �����ı�
        feedbackText.text = message;
        feedbackText.color = color;
        feedbackText.gameObject.SetActive(true);

        // ����һ���̶������ƶ���
        Vector2 startPos = feedbackRectTransform.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0, moveDistance);

        // ����Ч������ʱ��
        float startTime = Time.time;

        // ִ�ж���
        while (Time.time - startTime < feedbackDuration)
        {
            float progress = (Time.time - startTime) / feedbackDuration;

            // �ƶ��͵���Ч��
            feedbackRectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, progress);

            // ʹ�õ������ߣ�ʹ�����ڶ������ڲſ�ʼ����
            float alphaProgress = progress < 0.7f ? 1f : 1f - ((progress - 0.7f) / 0.3f);
            Color newColor = color;
            newColor.a = alphaProgress;
            feedbackText.color = newColor;

            yield return null;
        }

        // ��ɺ������ı�
        feedbackText.gameObject.SetActive(false);

        // ����λ�ã�׼���´�ʹ��
        feedbackRectTransform.anchoredPosition = startPos;

        // ��Ƿ����������
        isFeedbackPlaying = false;
    }

    public void PlayDrummerAnimation(bool isPerfect = false)
    {
        if (drummerAnimator == null) return;

        // ���Ŷ�Ӧ�Ķ���
        if (isPerfect && !string.IsNullOrEmpty(perfectHitTriggerName))
        {
            drummerAnimator.SetTrigger(perfectHitTriggerName);

            if (debugMode)
                Debug.Log("���Ź�����������");
        }
        else if (!string.IsNullOrEmpty(drumHitTriggerName))
        {
            drummerAnimator.SetTrigger(drumHitTriggerName);

            if (debugMode)
                Debug.Log("���Ź�����ͨ����");
        }
    }

    public void PlayPaddleEffect(Vector3 position, bool isPerfect)
    {
        // ���Ź��ֶ���
        PlayDrummerAnimation(isPerfect);

        // ��������Ч��
        if (!disableParticles)
        {
            ParticleSystem particlesToPlay = isPerfect && perfectParticles != null ? perfectParticles : paddleParticles;

            if (particlesToPlay != null)
            {
                // ���λ������������ʹ�õ�ǰλ��
                if (position == Vector3.zero && particlesToPlay.transform != null)
                {
                    // ���ֵ�ǰλ��
                }
                else
                {
                    particlesToPlay.transform.position = position;
                }

                // ��������ǿ��
                var mainModule = particlesToPlay.main;
                mainModule.startSizeMultiplier *= particleIntensityMultiplier;

                // ��������
                particlesToPlay.Play();

                // �ָ�Ĭ������
                mainModule.startSizeMultiplier /= particleIntensityMultiplier;
            }
        }

        // ������Ч
        if (!muteAudio && audioSource != null)
        {
            AudioClip clipToPlay = isPerfect ? perfectSound : paddleSound;
            if (clipToPlay != null)
                audioSource.PlayOneShot(clipToPlay, volumeMultiplier);
        }

        // ��ʾ�ı�����
        if (isPerfect)
            ShowTextFeedback("Perfect!", Color.green);
        else
            ShowTextFeedback("Good!", Color.yellow);
    }

    public void ShowMissFeedback()
    {
        // ��ʾMiss�ı�
        ShowTextFeedback("Miss!", Color.red);

        // ����Miss��Ч
        if (!muteAudio && audioSource != null && missSound != null)
            audioSource.PlayOneShot(missSound, volumeMultiplier);
    }

    public void PlayFullPowerEffect()
    {
        // ����ȫ��״̬��Ч
        if (!disableParticles && fullPowerParticles != null)
            fullPowerParticles.Play();

        // ����ȫ��״̬��Ч
        if (!muteAudio && audioSource != null && fullPowerSound != null)
            audioSource.PlayOneShot(fullPowerSound, volumeMultiplier);

        // ��ʾȫ��״̬�ı�
        ShowTextFeedback("FULL POWER!", Color.magenta);

        if (debugMode)
            Debug.Log("����ȫ��״̬Ч��");
    }

    public void PlayComboEffect(int comboCount)
    {
        // ����������Ч
        if (!disableParticles && comboParticles != null)
            comboParticles.Play();

        // ����������Ч
        if (!muteAudio && audioSource != null && comboSound != null)
            audioSource.PlayOneShot(comboSound, volumeMultiplier);

        // ��ʾ�����ı�
        ShowTextFeedback(comboCount + " Combo!", Color.cyan);

        if (debugMode)
            Debug.Log($"����{comboCount}����Ч��");
    }

    // ʵ�÷��� - ������ռ�����ʾһ����ʱ�ı�(�����Ҫ)
    public void ShowWorldTextFeedback(string message, Color color, Vector3 worldPosition)
    {
        // ����һ��ѡ�������3D����ռ������UI����ʾ����
        // ��Ҫ������Ŀ���ʵ��
        if (debugMode)
            Debug.Log($"����ռ䷴��: {message} λ��: {worldPosition}");
    }

    // ��������
    public void SetVolume(float volume)
    {
        volumeMultiplier = Mathf.Clamp01(volume); // ȷ����0-1��Χ��

        if (audioSource != null)
            audioSource.volume = volumeMultiplier;

        if (debugMode)
            Debug.Log($"��������: {volumeMultiplier}");
    }

    // ����/���÷���
    public void EnableFeedback(bool enable)
    {
        enabled = enable;

        if (!enable)
        {
            // ��ն���
            feedbackQueue.Clear();

            // ���ط����ı�
            if (feedbackText != null)
                feedbackText.gameObject.SetActive(false);

            // ֹͣ����Э��
            StopAllCoroutines();

            // ����״̬
            isFeedbackPlaying = false;
        }

        if (debugMode)
            Debug.Log($"����ϵͳ: {(enable ? "����" : "����")}");
    }

    // ����/������Ч
    public void EnableAudio(bool enable)
    {
        muteAudio = !enable;

        if (debugMode)
            Debug.Log($"��Ч: {(enable ? "����" : "����")}");
    }

    // ����/��������Ч��
    public void EnableParticles(bool enable)
    {
        disableParticles = !enable;

        if (debugMode)
            Debug.Log($"����Ч��: {(enable ? "����" : "����")}");
    }

    // �����Զ��巴��
    public void PlayCustomFeedback(string message, Color color, Vector3 position, bool playSound = true, bool playParticles = true)
    {
        // ��ʾ�ı�
        ShowTextFeedback(message, color);

        // ��������
        if (playParticles && !disableParticles && paddleParticles != null)
        {
            if (position != Vector3.zero)
                paddleParticles.transform.position = position;

            paddleParticles.Play();
        }

        // ������Ч
        if (playSound && !muteAudio && audioSource != null && paddleSound != null)
            audioSource.PlayOneShot(paddleSound, volumeMultiplier);

        if (debugMode)
            Debug.Log($"�����Զ��巴��: {message}");
    }
}