using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoryScreen : MonoBehaviour
{
    // Start is called before the first frame update
    private ScoreSystem scoreSystem;
    public GameManager gameManager;
    public TextMeshProUGUI scoreText;
    
    void Start()
    {
        scoreSystem = FindObjectOfType<ScoreSystem>();
        gameManager = FindObjectOfType<GameManager>();

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void DisplayScore()
    {
        // 更新分数
        if (scoreText != null && scoreSystem != null)
        {
            scoreText.text = "Your Score: " + scoreSystem.GetScore().ToString();
        }
        else Debug.Log("score system is null");
    }
    
    public void OnReturnButtonClicked()
    {   
        gameManager.ReturnToMainMenu();
    }
}
