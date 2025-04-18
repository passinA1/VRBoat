using UnityEngine;
using UnityEngine.Events;

public class RhythmJudgement : MonoBehaviour
{
    [System.Serializable]
    public class JudgementEvent : UnityEvent<JudgementResult> { }

    public enum Judgement { Perfect, Great, Miss }
    
    public struct JudgementResult
    {
        public Judgement judgement;
        public RhythmGenerator.HandType handType;
        public RhythmGenerator.BeatType beatType;
        public float timeDeviation;
    }

    [Header("判定设置")]
    public float perfectThreshold = 0.05f;  // 完美判定阈值(秒)
    public float greatThreshold = 0.15f;    // 优秀判定阈值(秒)
    public float inputBufferTime = 0.2f;    // 输入缓冲时间(秒)

    [Header("事件")]
    public JudgementEvent OnJudgementMade;  // 判定结果事件

    private RhythmGenerator.BeatInfo? pendingBeat; // 当前等待判定的鼓点
    private float lastInputTime;

    public void RegisterInput(RhythmGenerator.HandType inputHand, RhythmGenerator.BeatType inputType)
    {
        lastInputTime = Time.time;
        
        if (pendingBeat.HasValue)
        {
            var beat = pendingBeat.Value;
            float timeDeviation = Mathf.Abs(Time.time - beat.hitTime);
            
            // 检查手型和类型是否匹配
            if (beat.hand == inputHand && beat.type == inputType)
            {
                Judgement judgement = CalculateJudgement(timeDeviation);
                SendJudgement(judgement, beat, timeDeviation);
                pendingBeat = null;
            }
        }
    }

    public void RegisterBeat(RhythmGenerator.BeatInfo beat)
    {
        // 设置当前等待判定的鼓点
        pendingBeat = beat;
        
        // 设置超时检查
        Invoke("CheckMiss", greatThreshold);
    }

    private void CheckMiss()
    {
        if (pendingBeat.HasValue)
        {
            var beat = pendingBeat.Value;
            SendJudgement(Judgement.Miss, beat, greatThreshold);
            pendingBeat = null;
        }
    }

    private Judgement CalculateJudgement(float timeDeviation)
    {
        if (timeDeviation <= perfectThreshold)
            return Judgement.Perfect;
        if (timeDeviation <= greatThreshold)
            return Judgement.Great;
        return Judgement.Miss;
    }

    private void SendJudgement(Judgement judgement, RhythmGenerator.BeatInfo beat, float timeDeviation)
    {
        CancelInvoke("CheckMiss"); // 取消超时检查
        
        var result = new JudgementResult
        {
            judgement = judgement,
            handType = beat.hand,
            beatType = beat.type,
            timeDeviation = timeDeviation
        };

        OnJudgementMade.Invoke(result);
        
        // 调试输出
        string handText = beat.hand switch
        {
            RhythmGenerator.HandType.Left => "左手",
            RhythmGenerator.HandType.Both => "双手",
            _ => "右手"
        };
        Debug.Log($"{handText} {(beat.type == RhythmGenerator.BeatType.Single ? "单" : "双")}击: {judgement} (偏差: {timeDeviation:F3}s)");
    }
}