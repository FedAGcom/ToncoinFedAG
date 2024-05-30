using System;
using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    private bool _running = true;

    //private static GameManager _instance;

    /*public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                }
            }

            return _instance;
        }
    }*/

    /*private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }*/

    public void Initialize()
    {
        StartCoroutine(TestCounter());
    }

    private IEnumerator TestCounter()
    {
        while (_running)
        {
            yield return new WaitForSeconds(5f);
            uiManager.CurrentUser.TestValue += 1;
            uiManager.testValue.text = uiManager.CurrentUser.TestValue.ToString();

            uiManager.SaveUser(uiManager.CurrentUser);
        }
    }
    
    public void StopEngineGame()
    {
        StopAllCoroutines();
    }
}