using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class NewBehaviourScript : MonoBehaviour
{
    public Text endgameText;
    public Text timeText;
   
    private float leftTime;
    private bool isGameover;

    void Start()
    {
        leftTime = 10;
        isGameover = false;
    }


    void Update()
    {
        if( leftTime <= 0 )
        {
            isGameover=true;
        }
        if (!isGameover)
        {
            leftTime -= Time.deltaTime;
            timeText.text = (int)leftTime/60 + " : " + (int)leftTime%60;
        }
        else
        {
            SceneManager.LoadScene("EndScene");
        }
    }
}
