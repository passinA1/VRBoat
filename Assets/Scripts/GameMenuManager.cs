using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameMenuManager : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private Button startButton;

    private void Awake()
    {
        
    }
    void Start()
    {
        
    }
    public void OnEnable()
    {
        // 订阅场景加载完成事件
        Debug.Log("GameMenuManager enabled");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void OnDisable()
    {
        // 取消订阅场景加载完成事件
        Debug.Log("GameMenuManager disabled");
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "0_MainMenu_Scene")
        {
            // 如果使用动态查找，确保使用正确的查找路径
            if (startButton == null)
            {
                startButton = GameObject.Find("Canvas/Panel_Select/buttonRoot/startButton")?.GetComponent<Button>();
            }

            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(OnStartButtonClicked);
                Debug.Log("StartButton event rebound");
            }
            else
            {
                Debug.LogError("StartButton not found!");
            }
        }
    }


    public void OnStartButtonClicked()
    {
        SceneManager.LoadScene("1_Level1_Scene");
    }

    public void OnQuitButtonClicked()
    {
        Application.Quit();
    }
     public void OnLevelRecordButtonClicked()
    {

    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
