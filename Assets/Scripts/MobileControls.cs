using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public class MobileControls : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern int SkyMarblesIsMobileBrowser();
    private static readonly bool mobileBrowser = SkyMarblesIsMobileBrowser() != 0;
#endif
    public static bool IsMobile => Application.isMobilePlatform
#if UNITY_WEBGL && !UNITY_EDITOR
        || mobileBrowser
#endif
        ;
    public static Vector2 Aim { get; private set; }
    public static bool JumpHeld { get; private set; }
    private static bool jumpPressed;
    private static readonly bool[] activePressed = new bool[2];
    private RectTransform joystick;
    private RectTransform knob;
    private int pointerId = int.MinValue;
    private GameObject root;
    public static bool ConsumeJump() { bool value = jumpPressed; jumpPressed = false; return value; }
    public static bool ConsumeActive(int index) { bool value = activePressed[index]; activePressed[index] = false; return value; }
    public static Vector2 DashDirection(int last) => Aim.sqrMagnitude > 0.04f ? Aim.normalized : last == 0 ? Vector2.up : Vector2.right * last;

    private void Start()
    {
        Aim = Vector2.zero; JumpHeld = false; jumpPressed = false;
        activePressed[0] = activePressed[1] = false;
        Canvas canvas = GameObject.Find("Coin Text").GetComponentInParent<Canvas>();
        root = new GameObject("Mobile Controls", typeof(RectTransform));
        RectTransform parent = root.GetComponent<RectTransform>();
        parent.SetParent(canvas.transform, false);
        parent.anchorMin = Vector2.zero; parent.anchorMax = Vector2.one; parent.offsetMin = parent.offsetMax = Vector2.zero;
        Transform pause = canvas.transform.Find("Pause Menu");
        if (pause != null) parent.SetSiblingIndex(pause.GetSiblingIndex());
        joystick = MakeImage(parent, "Joystick", new Vector2(0, 0), new Vector2(105, 110), new Vector2(150, 150), new Color(.08f,.13f,.25f,.65f));
        joystick.gameObject.AddComponent<MobileJoystick>().owner = this;
        knob = MakeImage(joystick, "Knob", new Vector2(.5f, .5f), Vector2.zero, new Vector2(65, 65), new Color(.7f,.87f,.95f,.8f));
        knob.GetComponent<Image>().raycastTarget = false;
        RectTransform jump = MakeImage(parent, "Jump", new Vector2(1, 0), new Vector2(-155, 85), new Vector2(105, 105), new Color(.25f,.55f,.69f,.85f));
        AddText(jump, "JUMP");
        var jumpInput = jump.gameObject.AddComponent<MobileAction>(); jumpInput.kind = 0;

    }
    public static void ConfigureActiveHud(RectTransform hud, int index)
    {
        hud.anchorMin = hud.anchorMax = new Vector2(1f, 0f);
        hud.pivot = new Vector2(0.5f, 0.5f);
        // Center the ring where the old action button was.
        hud.anchoredPosition = new Vector2(-64f, 185f - index * 95f - 12f * 0.7f);
        hud.localScale = Vector3.one * 0.7f;
        Image hitArea = hud.gameObject.AddComponent<Image>();
        hitArea.color = Color.clear;
        hitArea.raycastTarget = true;
        hud.gameObject.AddComponent<MobileAction>().kind = index + 1;
        Transform pause = hud.parent.Find("Pause Menu");
        if (pause != null) hud.SetSiblingIndex(pause.GetSiblingIndex());
    }

    private static RectTransform MakeImage(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
        go.GetComponent<Image>().color = color; return rect;
    }
    private static void AddText(RectTransform parent, string text)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        TMP_Text label = go.GetComponent<TMP_Text>(); label.text = text; label.fontSize = 17; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
    }
    public void OnPointerDown(PointerEventData e) { if (pointerId != int.MinValue) return; pointerId = e.pointerId; OnDrag(e); }
    public void OnDrag(PointerEventData e)
    {
        if (e.pointerId != pointerId) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(joystick, e.position, e.pressEventCamera, out Vector2 local);
        Aim = Vector2.ClampMagnitude(local / (joystick.rect.width * .5f), 1f); knob.anchoredPosition = Aim * 45f;
    }
    public void OnPointerUp(PointerEventData e) { if (e.pointerId != pointerId) return; pointerId = int.MinValue; Aim = Vector2.zero; knob.anchoredPosition = Vector2.zero; }
    public static void Press(int kind) { if (kind == 0) { JumpHeld = true; jumpPressed = true; } else activePressed[kind - 1] = true; }
    public static void Release(int kind) { if (kind == 0) JumpHeld = false; }
    private void OnApplicationFocus(bool focused)
    {
        if (focused) return;
        pointerId = int.MinValue;
        Aim = Vector2.zero;
        JumpHeld = false;
        jumpPressed = false;
        activePressed[0] = activePressed[1] = false;
        if (knob != null) knob.anchoredPosition = Vector2.zero;
    }
    private void OnDestroy() { Aim = Vector2.zero; JumpHeld = false; jumpPressed = false; activePressed[0] = activePressed[1] = false; if (root != null) Destroy(root); }
}

public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public MobileControls owner;
    public void OnPointerDown(PointerEventData e) => owner.OnPointerDown(e);
    public void OnDrag(PointerEventData e) => owner.OnDrag(e);
    public void OnPointerUp(PointerEventData e) => owner.OnPointerUp(e);
}
public class MobileAction : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public int kind;
    public void OnPointerDown(PointerEventData e) => MobileControls.Press(kind);
    public void OnPointerUp(PointerEventData e) => MobileControls.Release(kind);
}
