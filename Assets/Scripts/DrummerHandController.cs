using UnityEngine;
using System.Collections;

public class DrummerHandController : MonoBehaviour
{
    private Animator animator;
    private bool isPlaying = false;
    private Coroutine currentIdleRoutine;

    [Header("动画状态设置")]
    [SerializeField] private string idle1TriggerName = "DrumerHand_Idle1";  // 慢动作小幅度待机
    [SerializeField] private string idle2TriggerName = "DrumerHand_Idle2";  // 节奏稍快待机
    [SerializeField] private string play1TriggerName = "DrumerHand_Play1";  // 慢拍子(1s)
    [SerializeField] private string play2TriggerName = "DrumerHand_Play2";  // 快拍子(0.5s)

    [Header("镜像设置")]
    [SerializeField] private bool isLeftHand = false;  // 是否为左手

    private void Start()
    {
        animator = GetComponent<Animator>();
        
        // 如果是左手，将X轴缩放设为-1来实现镜像
        if (isLeftHand)
        {
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }

        // 默认播放Idle1动画（慢动作小幅度待机）
        SetPlayingState(false);
    }

    // 准备动作处理
    public void PrepareLeft()
    {
        if (isLeftHand) PrepareToPlay();
    }

    public void PrepareRight()
    {
        if (!isLeftHand) PrepareToPlay();
    }

    public void PrepareBoth()
    {
        PrepareToPlay();
    }

    private void PrepareToPlay()
    {
        // 停止当前的待机协程
        if (currentIdleRoutine != null)
        {
            StopCoroutine(currentIdleRoutine);
            currentIdleRoutine = null;
        }
        
        isPlaying = true;
        animator.SetTrigger(idle2TriggerName);  // 切换到节奏稍快的待机动画
    }

    public void PlayBeatAnimation(RhythmGenerator.BeatInfo beatInfo)
    {
        // 检查是否应该播放动画（根据手的类型）
        bool shouldPlay = beatInfo.hand == RhythmGenerator.HandType.Both ||
                         (beatInfo.hand == RhythmGenerator.HandType.Left && isLeftHand) ||
                         (beatInfo.hand == RhythmGenerator.HandType.Right && !isLeftHand);

        if (!shouldPlay) return;

        // 根据节拍类型选择动画
        string triggerName = beatInfo.type == RhythmGenerator.BeatType.Single 
            ? play1TriggerName  // 慢拍子，1s完成
            : play2TriggerName; // 快拍子，0.5s一次，共两次
            
        animator.SetTrigger(triggerName);

        // 在动画播放后返回到合适的待机状态
        float waitTime = beatInfo.type == RhythmGenerator.BeatType.Single ? 1f : 1f;
        if (currentIdleRoutine != null)
        {
            StopCoroutine(currentIdleRoutine);
        }
        currentIdleRoutine = StartCoroutine(ReturnToIdle(waitTime));
    }

    private IEnumerator ReturnToIdle(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (!isPlaying)
        {
            // 如果不在演奏状态，返回到慢动作待机
            animator.SetTrigger(idle1TriggerName);
        }
        else
        {
            // 如果在演奏状态，返回到节奏稍快的待机
            animator.SetTrigger(idle2TriggerName);
        }
    }

    public void SetPlayingState(bool playing)
    {
        isPlaying = playing;
        // 根据演奏状态切换待机动画
        animator.SetTrigger(playing ? idle2TriggerName : idle1TriggerName);
    }
}