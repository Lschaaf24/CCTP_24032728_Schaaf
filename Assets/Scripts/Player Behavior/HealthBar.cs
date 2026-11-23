using UnityEngine;
using UnityEngine.UI;
public class HealthBar : MonoBehaviour
{
    public Slider slider;
    [SerializeField] private float smoothSpeed = 5f;
    private int targetFillAmount = 1;

    public void setMaxHealth(int health){
        slider.maxValue = health;
        slider.value = health;

    }

    private void Update()
    {

        slider.value = Mathf.Lerp(slider.value, targetFillAmount, smoothSpeed * Time.deltaTime);

    }



    public void setHealth(int health)
    {
        targetFillAmount = health;
    }
}
