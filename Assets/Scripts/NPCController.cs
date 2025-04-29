using UnityEngine;
using System.Collections;

public class NPCController : MonoBehaviour
{
    private Animator animator;
    private bool isPlaying = false;
    private Coroutine currentIdleRoutine;

    [Header("动画状态设置")]
    [Tooltip("慢动作小幅度待机动画名称")]
    [SerializeField] private string idle1StateName = "RowerHand_Idle1";
    [Tooltip("节奏稍快待机动画名称")]
    [SerializeField] private string idle2StateName = "RowerHand_Idle2";
    [Tooltip("慢拍子划船动画名称(1s)")]
    [SerializeField] private string row1StateName = "RowerHand_Row1";
    [Tooltip("快拍子划船动画名称(0.5s)")]
    [SerializeField] private string row2StateName = "RowerHand_Row2";
    [Tooltip("慢拍子首次划船动画")]
    [SerializeField] private string firstRow1StateName = "RowerHand_FirstRow1";
    [Tooltip("快拍子首次划船动画")]
    [SerializeField] private string firstRow2StateName = "RowerHand_FirstRow2";
    [Tooltip("结束划船回到待机动画")]
    [SerializeField] private string rowToIdleStateName = "RowerHand_RowToIdle";

    [Header("手部设置")]
    [Tooltip("勾选表示这是左手控制器")]
    [SerializeField] public bool isLeftHand = false;

    private void Start()
    {
        animator = GetComponent<Animator>();
        
        // 如果是左手，将X轴缩放设为负值
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
            Debug.Log($"左手准备动作被触发 - isLeftHand: {isLeftHand}");
            
            if (currentIdleRoutine != null)
            {
                StopCoroutine(currentIdleRoutine);
                currentIdleRoutine = null;
            }
            isPlaying = true;
            animator.Play(firstRow1StateName);
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
            animator.Play(firstRow1StateName);
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
        animator.Play(firstRow1StateName);
    }

    public void PlayRowAnimation(RhythmGenerator.BeatInfo beatInfo)
    {
        // 检查是否应该播放动画（根据手的类型）
        bool shouldPlay = beatInfo.hand == RhythmGenerator.HandType.Both ||
                         (beatInfo.hand == RhythmGenerator.HandType.Left && isLeftHand) ||
                         (beatInfo.hand == RhythmGenerator.HandType.Right && !isLeftHand);

        if (!shouldPlay) return;

        // 根据节拍类型选择动画
        string stateName;
        if (isPlaying)
        {
            // 如果已经在划船，使用普通划船动画
            stateName = beatInfo.type == RhythmGenerator.BeatType.Single 
                ? row1StateName  // 慢拍子，1s完成
                : row2StateName; // 快拍子，0.5s一次，共两次
        }
        else
        {
            // 如果是开始划船，使用首次划船动画
            stateName = beatInfo.type == RhythmGenerator.BeatType.Single 
                ? firstRow1StateName  // 慢拍子首次划船
                : firstRow2StateName; // 快拍子首次划船
        }
            
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
        
        // 播放过渡到待机的动画
        animator.Play(rowToIdleStateName);
        
        // 等待过渡动画完成
        yield return new WaitForSeconds(0.5f);
        
        if (!isPlaying)
        {
            // 如果不在划船状态，返回到慢动作待机
            animator.Play(idle1StateName);
        }
        else
        {
            // 如果在划船状态，返回到节奏稍快的待机
            animator.Play(idle2StateName);
        }
    }

    public void SetPlayingState(bool playing)
    {
        isPlaying = playing;
        // 根据划船状态切换待机动画
        animator.Play(playing ? idle2StateName : idle1StateName);
    }
}