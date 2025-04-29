using UnityEngine;
using System.Collections;

public class DrummerHandController : MonoBehaviour
{
    private Animator animator;
    private bool isPlaying = false;
    private Coroutine currentIdleRoutine;

    [Header("动画状态设置")]
    [Tooltip("慢动作小幅度待机动画名称")]
    [SerializeField] private string idle1StateName = "DrumerHand_Idle1";
    [Tooltip("节奏稍快待机动画名称")]
    [SerializeField] private string idle2StateName = "DrumerHand_Idle2";
    [Tooltip("慢拍子动画名称(1s)")]
    [SerializeField] private string play1StateName = "DrumerHand_Play1";
    [Tooltip("快拍子动画名称(0.5s)")]
    [SerializeField] private string play2StateName = "DrumerHand_Play2";

    [Header("手部设置")]
    [Tooltip("勾选表示这是左手控制器")]
    [SerializeField] public bool isLeftHand = false;  // 修改为public，使其可以从外部访问

    private void Start()
    {
        animator = GetComponent<Animator>();
        
        // 如果是左手，将X轴缩放设为负值（不是乘以-1）
        if (isLeftHand)
        {
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x);  // 确保X轴缩放为负值
            transform.localScale = scale;
        }
        else
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x);   // 确保X轴缩放为正值
            transform.localScale = scale;
        }

        // 默认播放Idle1动画（慢动作小幅度待机）
        SetPlayingState(false);
    }

    // 准备动作处理
    public void PrepareLeft()
    {
        if (isLeftHand) 
        {
            // 添加调试输出
            Debug.Log($"左手准备动作被触发 - isLeftHand: {isLeftHand}");
            
            if (currentIdleRoutine != null)
            {
                StopCoroutine(currentIdleRoutine);
                currentIdleRoutine = null;
            }
            isPlaying = true;
            animator.Play(play1StateName);
        }
    }

    public void PrepareRight()
    {
        if (!isLeftHand)
        {
            if (currentIdleRoutine != null)
            {
                StopCoroutine(currentIdleRoutine);
                currentIdleRoutine = null;
            }
            isPlaying = true;
            animator.Play(play1StateName);
        }
    }

    public void PrepareBoth()
    {
        if (currentIdleRoutine != null)
        {
            StopCoroutine(currentIdleRoutine);
            currentIdleRoutine = null;
        }
        isPlaying = true;
        animator.Play(play1StateName);
    }

    public void PlayBeatAnimation(RhythmGenerator.BeatInfo beatInfo)
    {
        // 检查是否应该播放动画（根据手的类型）
        bool shouldPlay = beatInfo.hand == RhythmGenerator.HandType.Both ||
                         (beatInfo.hand == RhythmGenerator.HandType.Left && isLeftHand) ||
                         (beatInfo.hand == RhythmGenerator.HandType.Right && !isLeftHand);

        if (!shouldPlay) return;

        // 根据节拍类型选择动画
        string stateName = beatInfo.type == RhythmGenerator.BeatType.Single 
            ? play1StateName  // 慢拍子，1s完成
            : play2StateName; // 快拍子，0.5s一次，共两次
            
        animator.Play(stateName);

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
            animator.Play(idle1StateName);
        }
        else
        {
            // 如果在演奏状态，返回到节奏稍快的待机
            animator.Play(idle2StateName);
        }
    }

    public void SetPlayingState(bool playing)
    {
        isPlaying = playing;
        // 根据演奏状态切换待机动画
        animator.Play(playing ? idle2StateName : idle1StateName);
    }
}