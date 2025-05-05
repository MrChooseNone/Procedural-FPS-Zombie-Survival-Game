using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DamageEffect : MonoBehaviour
{
    public Image damageOverlay;
    public float fadeSpeed = 2f;

    void Start()
    {
        damageOverlay.color = new Color(1, 0, 0, 0); // Start fully transparent
    }

    public void ShowDamage()
    {
        damageOverlay.color = new Color(1, 0, 0, 0.5f); // Flash red
        Invoke("FadeOut", 0.2f);
    }

    void FadeOut()
    {
        StartCoroutine(FadeEffect());
    }

    IEnumerator FadeEffect()
    {
        while (damageOverlay.color.a > 0)
        {
            damageOverlay.color = new Color(1, 0, 0, damageOverlay.color.a - Time.deltaTime * fadeSpeed);
            yield return null;
        }
    }
}

