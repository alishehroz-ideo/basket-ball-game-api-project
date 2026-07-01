using UnityEngine;
using UnityEngine.UI;

public class ImageFillController : MonoBehaviour
{
    public Slider fillSlider; 
    [Range(0f, 1f)]
    public float targetFillValue = 1f; 
    public float fillingTime = 12f;

    private float initialFillValue; 
    private float fillSpeed; 
    private float fillStartTime; 
    private bool isFilling; 
    private void OnEnable()
    {

        initialFillValue = 0f; 
        fillSlider.value = initialFillValue;
        fillSpeed = Mathf.Abs(targetFillValue - initialFillValue) / fillingTime;
        isFilling = true;
        fillStartTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {

        if (isFilling && fillSlider.value != targetFillValue)
        {
            float timeElapsed = Time.time - fillStartTime;
            float interpolatedFillValue = Mathf.Lerp(initialFillValue, targetFillValue, timeElapsed / fillingTime);

            fillSlider.value = interpolatedFillValue;

            if (timeElapsed >= fillingTime)
            {

                fillSlider.value = targetFillValue;
                this.gameObject.SetActive(false);
                print("SHOW OPEN");
                isFilling = false;
            }
        }
    }
}
