using UnityEngine;
using UnityEngine.UI;

public class Hitmarker : MonoBehaviour
{
    [SerializeField] private Image [] hitmarkerArms;

    [SerializeField] private float hitmarkerDuration = 0.1f;

    [SerializeField] private float hitmarkerFadeSpeed = 1f;
    [SerializeField] private float hitmarkerVelocity = 0.1f;

    [SerializeField] private float hitmarkerDeceleration = 0.1f;

    [SerializeField] private Color color = Color.white;
    
    
    private bool initialized = false;

    public void Inititialise(Color color, float damageRatio)
    {
        this.color = color;
        // Change length of hitmarker arms based on damage dealt
        foreach (Image hitmarkerArm in hitmarkerArms)
        {
            RectTransform rectTransform = hitmarkerArm.rectTransform;
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x * damageRatio, rectTransform.sizeDelta.y);
        }
        foreach (Image hitmarkerArm in hitmarkerArms)
        {
            hitmarkerArm.color = color;
        }
        initialized = true;
        Destroy(gameObject, hitmarkerDuration);
    }
    void Update()
    {
        if (!initialized) return;
        foreach (Image hitmarkerArm in hitmarkerArms)
        {
            RectTransform rectTransform = hitmarkerArm.rectTransform;
            Vector2 direction = rectTransform.right;
            rectTransform.anchoredPosition += hitmarkerVelocity * Time.deltaTime * direction;
            hitmarkerVelocity -= hitmarkerVelocity * hitmarkerDeceleration * Time.deltaTime;
            hitmarkerArm.color = new Color(color.r, color.g, color.b, hitmarkerArm.color.a - hitmarkerFadeSpeed * Time.deltaTime);
        }
    }
}
