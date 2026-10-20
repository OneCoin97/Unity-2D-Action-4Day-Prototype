using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FloatingMessagePool : MonoBehaviour
{
    #region Inspector

    [Header("Text")]
    [SerializeField] private Font font;
    [SerializeField, Min(1)] private int fontSize = 32;
    [SerializeField, Min(0.01f)] private float characterSize = 0.1f;
    [SerializeField] private int sortingOrder = 100;

    [Header("Motion")]
    [SerializeField, Min(0.01f)] private float duration = 0.8f;
    [SerializeField, Min(0f)] private float floatingDistance = 1f;

    [Header("Position")]
    [SerializeField] private Vector2 offset = new Vector2(0f,1f);
    [SerializeField] private Vector2 randomPositionRange = new Vector2(0.25f,0.1f);

    #endregion

    private static FloatingMessagePool currentInstance;

    public static FloatingMessagePool instance
    {
        get
        {
            if (currentInstance == null)
            {
                GameObject poolObject = new GameObject(nameof(FloatingMessagePool));
                currentInstance = poolObject.AddComponent<FloatingMessagePool>();
            }

            return currentInstance;
        }
    }

    private readonly Stack<FloatingMessage> floatingMessagePool = new Stack<FloatingMessage>();
    private int floatingMessageCount;

    private void Awake()
    {
        currentInstance = this;
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }

    public void play(string message,Color color,Vector2 position)
    {
        FloatingMessage floatingMessage = floatingMessagePool.Count > 0
            ? floatingMessagePool.Pop()
            : createFloatingMessage();

        Vector2 randomPosition = new Vector2(
            Random.Range(-randomPositionRange.x,randomPositionRange.x),
            Random.Range(-randomPositionRange.y,randomPositionRange.y));
        Vector2 messagePosition = position + offset + randomPosition;
        floatingMessage.play(message,color,messagePosition,duration,floatingDistance);
    }

    public void play(int value,Color color,Vector2 position)
    {
        play(value.ToString(),color,position);
    }

    public void release(FloatingMessage floatingMessage)
    {
        floatingMessage.gameObject.SetActive(false);
        floatingMessage.transform.SetParent(transform,false);
        floatingMessagePool.Push(floatingMessage);
    }

    private FloatingMessage createFloatingMessage()
    {
        GameObject floatingMessageObject = new GameObject($"FloatingMessage_{floatingMessageCount++}");
        floatingMessageObject.transform.SetParent(transform,false);
        floatingMessageObject.AddComponent<MeshRenderer>();
        floatingMessageObject.AddComponent<TextMesh>();

        FloatingMessage floatingMessage = floatingMessageObject.AddComponent<FloatingMessage>();
        floatingMessage.initialize(this,font,fontSize,characterSize,sortingOrder);
        return floatingMessage;
    }

    private void OnDestroy()
    {
        if (currentInstance == this)
        {
            currentInstance = null;
        }

        floatingMessagePool.Clear();
    }

    private void OnValidate()
    {
        fontSize = Mathf.Max(1,fontSize);
        characterSize = Mathf.Max(0.01f,characterSize);
        duration = Mathf.Max(0.01f,duration);
        floatingDistance = Mathf.Max(0f,floatingDistance);
        randomPositionRange.x = Mathf.Max(0f,randomPositionRange.x);
        randomPositionRange.y = Mathf.Max(0f,randomPositionRange.y);
    }
}
