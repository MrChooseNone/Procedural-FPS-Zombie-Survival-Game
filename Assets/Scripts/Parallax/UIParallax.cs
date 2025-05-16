using UnityEngine;
using UnityEngine.UI;

public class UIParallax : MonoBehaviour
{
    [System.Serializable]
    public struct Layer
    {
        public RectTransform rect;  // the UI Image’s RectTransform
        public float speed;         // how fast it moves (0 = static, 1 = same as mouse delta)
    }

    public Layer[] layers;
    Vector2 screenCenter;

    void Awake()
    {
        screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    void Update()
    {
        // mouse offset normalized to [-1 .. +1]
        Vector2 diff = ( (Vector2)Input.mousePosition - screenCenter ) / screenCenter;

        // apply to each layer
        foreach (var layer in layers)
        {
            // multiply by speed and by some maximum movement (e.g. 50px)
            Vector2 move = diff * layer.speed;
            layer.rect.anchoredPosition = move;
        }
    }
}
