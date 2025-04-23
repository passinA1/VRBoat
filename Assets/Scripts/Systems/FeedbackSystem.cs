using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class FeedbackSystem : MonoBehaviour
{
    [Header("文本反馈")]
    public TextMeshProUGUI feedbackText;
    public float feedbackDuration = 0.8f;
    public float moveDistance = 30f; // 文本上移距离
    public int maxQueuedFeedbacks = 3; // 最大排队反馈数量

    [Header("粒子效果")]
    public ParticleSystem paddleParticles;
    public ParticleSystem perfectParticles;
    public ParticleSystem fullPowerParticles;
    public ParticleSystem comboParticles;
    public float particleIntensityMultiplier = 1.0f; // 粒子强度倍数

    [Header("音效")]
    public AudioClip paddleSound;
    public AudioClip perfectSound;
    public AudioClip missSound;
    public AudioClip fullPowerSound;
    public AudioClip comboSound;
    public float volumeMultiplier = 1.0f; // 音量倍数

    [Header("鼓手动画")]
    public Animator drummerAnimator;
    public string drumHitTriggerName = "DrumHit";
    public string perfectHitTriggerName = "PerfectHit";

    [Header("调试设置")]
    public bool debugMode = false; // 调试模式
    public bool muteAudio = false; // 静音
    public bool disableParticles = false; // 禁用粒子
    public KeyCode testPerfectKey = KeyCode.Alpha1; // 测试完美反馈
    public KeyCode testGoodKey = KeyCode.Alpha2; // 测试良好反馈
    public KeyCode testMissKey = KeyCode.Alpha3; // 测试Miss反馈
    public KeyCode testComboKey = KeyCode.Alpha4; // 测试连击反馈
    public KeyCode testFullPowerKey = KeyCode.Alpha5; // 测试全力反馈

    private AudioSource audioSource;
    private Queue<FeedbackInfo> feedbackQueue = new Queue<FeedbackInfo>();
    private bool isFeedbackPlaying = false;
    private RectTransform feedbackRectTransform;

    // 反馈信息类
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
        // 获取或添加音频源
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.volume = volumeMultiplier;

        // 获取反馈文本的RectTransform
        if (feedbackText != null)
        {
            feedbackRectTransform = feedbackText.rectTransform;
            feedbackText.gameObject.SetActive(false);
        }

        Debug.Log("FeedbackSystem初始化完成");
    }

    void Update()
    {
        // 调试按键测试
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

        // 处理反馈队列
        ProcessFeedbackQueue();
    }

    // 处理反馈队列
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

        // 限制队列长度，防止过多反馈堆积
        if (feedbackQueue.Count >= maxQueuedFeedbacks)
        {
            if (debugMode)
                Debug.LogWarning("反馈队列已满，丢弃反馈: " + message);
            return;
        }

        // 添加到队列
        feedbackQueue.Enqueue(new FeedbackInfo(message, color));

        if (debugMode)
            Debug.Log("添加文本反馈: " + message);
    }

    IEnumerator TextFeedbackCoroutine(string message, Color color)
    {
        isFeedbackPlaying = true;

        // 重置位置到中心位置
        feedbackRectTransform.anchoredPosition = Vector2.zero;

        // 设置文本
        feedbackText.text = message;
        feedbackText.color = color;
        feedbackText.gameObject.SetActive(true);

        // 创建一个固定的上移动画
        Vector2 startPos = feedbackRectTransform.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0, moveDistance);

        // 动画效果持续时间
        float startTime = Time.time;

        // 执行动画
        while (Time.time - startTime < feedbackDuration)
        {
            float progress = (Time.time - startTime) / feedbackDuration;

            // 移动和淡出效果
            feedbackRectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, progress);

            // 使用淡出曲线，使文字在动画后期才开始淡出
            float alphaProgress = progress < 0.7f ? 1f : 1f - ((progress - 0.7f) / 0.3f);
            Color newColor = color;
            newColor.a = alphaProgress;
            feedbackText.color = newColor;

            yield return null;
        }

        // 完成后隐藏文本
        feedbackText.gameObject.SetActive(false);

        // 重置位置，准备下次使用
        feedbackRectTransform.anchoredPosition = startPos;

        // 标记反馈播放完成
        isFeedbackPlaying = false;
    }

    public void PlayDrummerAnimation(bool isPerfect = false)
    {
        if (drummerAnimator == null) return;

        // 播放对应的动画
        if (isPerfect && !string.IsNullOrEmpty(perfectHitTriggerName))
        {
            drummerAnimator.SetTrigger(perfectHitTriggerName);

            if (debugMode)
                Debug.Log("播放鼓手完美动画");
        }
        else if (!string.IsNullOrEmpty(drumHitTriggerName))
        {
            drummerAnimator.SetTrigger(drumHitTriggerName);

            if (debugMode)
                Debug.Log("播放鼓手普通动画");
        }
    }

    public void PlayPaddleEffect(Vector3 position, bool isPerfect)
    {
        // 播放鼓手动画
        PlayDrummerAnimation(isPerfect);

        // 播放粒子效果
        if (!disableParticles)
        {
            ParticleSystem particlesToPlay = isPerfect && perfectParticles != null ? perfectParticles : paddleParticles;

            if (particlesToPlay != null)
            {
                // 如果位置是零向量，使用当前位置
                if (position == Vector3.zero && particlesToPlay.transform != null)
                {
                    // 保持当前位置
                }
                else
                {
                    particlesToPlay.transform.position = position;
                }

                // 设置粒子强度
                var mainModule = particlesToPlay.main;
                mainModule.startSizeMultiplier *= particleIntensityMultiplier;

                // 播放粒子
                particlesToPlay.Play();

                // 恢复默认设置
                mainModule.startSizeMultiplier /= particleIntensityMultiplier;
            }
        }

        // 播放音效
        if (!muteAudio && audioSource != null)
        {
            AudioClip clipToPlay = isPerfect ? perfectSound : paddleSound;
            if (clipToPlay != null)
                audioSource.PlayOneShot(clipToPlay, volumeMultiplier);
        }

        // 显示文本反馈
        if (isPerfect)
            ShowTextFeedback("Perfect!", Color.green);
        else
            ShowTextFeedback("Good!", Color.yellow);
    }

    public void ShowMissFeedback()
    {
        // 显示Miss文本
        ShowTextFeedback("Miss!", Color.red);

        // 播放Miss音效
        if (!muteAudio && audioSource != null && missSound != null)
            audioSource.PlayOneShot(missSound, volumeMultiplier);
    }

    public void PlayFullPowerEffect()
    {
        // 播放全力状态特效
        if (!disableParticles && fullPowerParticles != null)
            fullPowerParticles.Play();

        // 播放全力状态音效
        if (!muteAudio && audioSource != null && fullPowerSound != null)
            audioSource.PlayOneShot(fullPowerSound, volumeMultiplier);

        // 显示全力状态文本
        ShowTextFeedback("FULL POWER!", Color.magenta);

        if (debugMode)
            Debug.Log("播放全力状态效果");
    }

    public void PlayComboEffect(int comboCount)
    {
        // 播放连击特效
        if (!disableParticles && comboParticles != null)
            comboParticles.Play();

        // 播放连击音效
        if (!muteAudio && audioSource != null && comboSound != null)
            audioSource.PlayOneShot(comboSound, volumeMultiplier);

        // 显示连击文本
        ShowTextFeedback(comboCount + " Combo!", Color.cyan);

        if (debugMode)
            Debug.Log($"播放{comboCount}连击效果");
    }

    // 实用方法 - 在世界空间中显示一个临时文本(如果需要)
    public void ShowWorldTextFeedback(string message, Color color, Vector3 worldPosition)
    {
        // 这是一个选项，用于在3D世界空间而不是UI上显示反馈
        // 需要根据项目情况实现
        if (debugMode)
            Debug.Log($"世界空间反馈: {message} 位置: {worldPosition}");
    }

    // 设置音量
    public void SetVolume(float volume)
    {
        volumeMultiplier = Mathf.Clamp01(volume); // 确保在0-1范围内

        if (audioSource != null)
            audioSource.volume = volumeMultiplier;

        if (debugMode)
            Debug.Log($"设置音量: {volumeMultiplier}");
    }

    // 启用/禁用反馈
    public void EnableFeedback(bool enable)
    {
        enabled = enable;

        if (!enable)
        {
            // 清空队列
            feedbackQueue.Clear();

            // 隐藏反馈文本
            if (feedbackText != null)
                feedbackText.gameObject.SetActive(false);

            // 停止所有协程
            StopAllCoroutines();

            // 重置状态
            isFeedbackPlaying = false;
        }

        if (debugMode)
            Debug.Log($"反馈系统: {(enable ? "启用" : "禁用")}");
    }

    // 启用/禁用音效
    public void EnableAudio(bool enable)
    {
        muteAudio = !enable;

        if (debugMode)
            Debug.Log($"音效: {(enable ? "启用" : "禁用")}");
    }

    // 启用/禁用粒子效果
    public void EnableParticles(bool enable)
    {
        disableParticles = !enable;

        if (debugMode)
            Debug.Log($"粒子效果: {(enable ? "启用" : "禁用")}");
    }

    // 播放自定义反馈
    public void PlayCustomFeedback(string message, Color color, Vector3 position, bool playSound = true, bool playParticles = true)
    {
        // 显示文本
        ShowTextFeedback(message, color);

        // 播放粒子
        if (playParticles && !disableParticles && paddleParticles != null)
        {
            if (position != Vector3.zero)
                paddleParticles.transform.position = position;

            paddleParticles.Play();
        }

        // 播放音效
        if (playSound && !muteAudio && audioSource != null && paddleSound != null)
            audioSource.PlayOneShot(paddleSound, volumeMultiplier);

        if (debugMode)
            Debug.Log($"播放自定义反馈: {message}");
    }
}