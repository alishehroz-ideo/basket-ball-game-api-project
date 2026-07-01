using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    public static Timer Instance;

    public bool isTime;
    public float totalTime;
    public Text timeLeftt;

    private void Awake()
    {
        if(Instance is null)
        {
            Instance = this;
        }
        Invoke("TimeStart", 1.5f);
    }
    void TimeStart()
    {
        isTime = true;
    }
    void Update()
    {
        if (isTime)
        {

            if (totalTime > 0)
            {
                timeLeftt.text = totalTime.ToString();
                totalTime -= Time.deltaTime;
                updateTimer(totalTime);
            }
            else if (totalTime <= 0)
            {
                totalTime = 0;
                GameManager.instance.OnGameEnd();
                isTime = false;
            }
        }
    }
    void updateTimer(float currentTime)
    {
        currentTime += 1;
        float minutes = Mathf.FloorToInt(currentTime / 60);
        float seconds = Mathf.FloorToInt(currentTime % 60);
        timeLeftt.gameObject.GetComponent<Text>().text = string.Format("{00:00}:{01:00}", minutes, seconds);
    }
}
