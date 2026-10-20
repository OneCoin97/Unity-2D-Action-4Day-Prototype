using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TextMesh))]
public sealed class FloatingMessage : MonoBehaviour
{
    private FloatingMessagePool floatingMessagePool;
    private TextMesh messageText;
    private Vector2 startPosition;
    private Color messageColor;
    private float duration;
    private float floatingDistance;
    private float elapsedTime;

    public void initialize(FloatingMessagePool targetFloatingMessagePool,Font font,int fontSize,float characterSize,int sortingOrder)
    {
        floatingMessagePool = targetFloatingMessagePool;
        messageText = GetComponent<TextMesh>();
        messageText.font = font;
        messageText.fontSize = fontSize;
        messageText.characterSize = characterSize;
        messageText.anchor = TextAnchor.MiddleCenter;
        messageText.alignment = TextAlignment.Center;

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = font.material;
        meshRenderer.sortingOrder = sortingOrder;
    }

    public void play(string message,Color color,Vector2 position,float targetDuration,float targetFloatingDistance)
    {
        startPosition = position;
        messageColor = color;
        duration = targetDuration;
        floatingDistance = targetFloatingDistance;
        elapsedTime = 0f;

        transform.position = position;
        messageText.text = message;
        messageText.color = color;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsedTime / duration);

        transform.position = startPosition + Vector2.up * floatingDistance * progress;
        Color currentColor = messageColor;
        currentColor.a = messageColor.a * (1f - progress);
        messageText.color = currentColor;

        if (progress >= 1f)
        {
            floatingMessagePool.release(this);
        }
    }
}
