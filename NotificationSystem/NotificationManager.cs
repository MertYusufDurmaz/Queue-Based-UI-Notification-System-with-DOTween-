using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    [Header("Referanslar")]
    [SerializeField] private GameObject notificationPrefab;
    [SerializeField] public GameObject gorevHatirlatmaPrefab;

    [Header("Ekran Dışı (Off-Screen) Ayarları")]
    [Tooltip("Bildirimlerin ekran dışında saklanacağı güvenli mesafeler (Çözünürlüğe göre artırın)")]
    [SerializeField] private float offScreenLeftX = -2000f;
    [SerializeField] private float offScreenRightX = 2000f;
    [SerializeField] private float offScreenBottomY = -800f;

    [Header("Sol Taraf (Görev) Ayarları")]
    [SerializeField] private float taskVisibleXPosition = -920f;
    [SerializeField] private float startYOffset = 400f;
    [SerializeField] private float spacingBetweenTasks = 15f;

    [Header("Sol Taraf (Normal İpucu) Ayarları")]
    [SerializeField] private float hintTargetX = -1068f;
    [SerializeField] private float hintTargetY = -270f;

    [Header("Sağ Taraf (Pil vb.) Ayarları")]
    [SerializeField] private float batteryTargetX = 920f;
    [SerializeField] private float batteryTargetY = -270f;

    [Header("Genel Ayarlar")]
    [SerializeField] private string notificationCanvasName = "UI_NotificationCanvas";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private Canvas notificationCanvas;
    private Queue<NotificationData> notificationQueue = new Queue<NotificationData>();
    private bool isShowing = false;

    private Dictionary<string, GameObject> activeTaskNotifications = new Dictionary<string, GameObject>();

    #region Singleton ve Sahne Yönetimi

    void Awake()
    {
        gorevHatirlatmaPrefab = Resources.Load<GameObject>("NotificationTaskPanel");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    private void Start() { InitializeSceneReferences(); }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeSceneReferences();
    }

    private void InitializeSceneReferences()
    {
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
        {
            ResetManager();
            return;
        }
        ResetManager();

        GameObject canvasObj = GameObject.Find(notificationCanvasName);
        if (canvasObj != null) notificationCanvas = canvasObj.GetComponent<Canvas>();
    }

    private void ResetManager()
    {
        if (activeTaskNotifications != null)
        {
            foreach (var notif in activeTaskNotifications.Values)
                if (notif != null) Destroy(notif);
        }
        notificationQueue.Clear();
        activeTaskNotifications.Clear();
        StopAllCoroutines();
        isShowing = false;
        notificationCanvas = null;
    }

    #endregion

    public void ForceCloseAllNotifications() { ResetManager(); }

    public void ShowNotification(NotificationType type, string message, KeyCode closeKey = KeyCode.None)
    {
        if (SceneManager.GetActiveScene().name == mainMenuSceneName) return;
        notificationQueue.Enqueue(new NotificationData(type, message, closeKey));
        if (!isShowing) StartCoroutine(ProcessQueue());
    }

    public void CloseTaskNotification(string taskId)
    {
        if (activeTaskNotifications.TryGetValue(taskId, out GameObject notif))
        {
            activeTaskNotifications.Remove(taskId);

            if (notif != null)
            {
                notif.transform.DOLocalMoveX(offScreenLeftX, 0.5f).SetEase(Ease.InQuad).OnComplete(() => {
                    Destroy(notif);
                    RealignNotifications();
                });
            }
        }
    }

    public bool UpdateTaskNotificationVisuals(string originalMessage, string newText)
    {
        if (activeTaskNotifications.TryGetValue(originalMessage, out GameObject notif))
        {
            if (notif != null)
            {
                var textComp = notif.GetComponentInChildren<TextMeshProUGUI>();
                if (textComp != null)
                {
                    textComp.text = newText;
                    LayoutRebuilder.ForceRebuildLayoutImmediate(notif.GetComponent<RectTransform>());
                    RealignNotifications();
                    return true;
                }
            }
        }
        return false;
    }

    private void RealignNotifications()
    {
        float currentY = startYOffset;
        foreach (var notif in activeTaskNotifications.Values)
        {
            if (notif != null)
            {
                RectTransform rect = notif.GetComponent<RectTransform>();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                notif.transform.DOLocalMoveY(currentY, 0.3f).SetEase(Ease.OutQuad);
                currentY -= (rect.sizeDelta.y + spacingBetweenTasks);
            }
        }
    }

    private float CalculateNextYPosition()
    {
        float currentY = startYOffset;
        foreach (var notif in activeTaskNotifications.Values)
        {
            if (notif != null)
            {
                RectTransform rect = notif.GetComponent<RectTransform>();
                currentY -= (rect.sizeDelta.y + spacingBetweenTasks);
            }
        }
        return currentY;
    }

    // --- KOD TEKRARINI ÖNLEYEN YARDIMCI METOT ---
    private IEnumerator WaitForKeyOrTime(KeyCode key, float defaultTime)
    {
        if (key != KeyCode.None)
        {
            yield return new WaitUntil(() => Input.GetKeyDown(key));
        }
        else
        {
            yield return new WaitForSeconds(defaultTime);
        }
    }

    // --- KUYRUK İŞLEYİCİ ---
    private IEnumerator ProcessQueue()
    {
        while (notificationQueue.Count > 0)
        {
            isShowing = true;
            NotificationData data = notificationQueue.Dequeue();

            if (notificationCanvas == null) { isShowing = false; yield break; }

            GameObject prefab = data.type == NotificationType.GorevHatirlatma ? gorevHatirlatmaPrefab : notificationPrefab;
            if (prefab == null) { isShowing = false; yield break; }

            GameObject notif = Instantiate(prefab, notificationCanvas.transform);
            TextMeshProUGUI text = notif.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = data.message;

            LayoutRebuilder.ForceRebuildLayoutImmediate(notif.GetComponent<RectTransform>());

            // DURUMLARA GÖRE ALT METOTLARA YÖNLENDİR
            switch (data.type)
            {
                case NotificationType.Warning:
                case NotificationType.Uyari:
                case NotificationType.Bilgilendirme:
                    yield return StartCoroutine(HandleBottomNotification(notif, text, data));
                    break;

                case NotificationType.GorevHatirlatma:
                    HandleTaskNotification(notif, data);
                    break;

                case NotificationType.PilBildirimi:
                    yield return StartCoroutine(HandleSideNotification(notif, data, true)); // true = Sağ taraf
                    break;

                case NotificationType.Ipucu:
                default:
                    yield return StartCoroutine(HandleSideNotification(notif, data, false)); // false = Sol taraf
                    break;
            }

            // Görev bildirimi değilse kuyruktaki diğer öğeye geçmeden önce yarım saniye es ver
            if (data.type != NotificationType.GorevHatirlatma) 
                yield return new WaitForSeconds(0.5f);

            isShowing = false;
        }
    }

    #region Bildirim Animasyon Mantıkları

    private IEnumerator HandleBottomNotification(GameObject notif, TextMeshProUGUI text, NotificationData data)
    {
        RectTransform rect = notif.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        notif.transform.localPosition = new Vector3(0, offScreenBottomY, 0);

        if (text != null) 
            text.color = (data.type == NotificationType.Warning || data.type == NotificationType.Uyari) ? Color.red : Color.white;

        notif.transform.DOLocalMoveY(-350f, 0.5f).SetEase(Ease.OutBack);

        yield return StartCoroutine(WaitForKeyOrTime(data.closeKey, 4f));

        if (notif != null) 
            notif.transform.DOLocalMoveY(offScreenBottomY, 0.5f).SetEase(Ease.InBack).OnComplete(() => Destroy(notif));
    }

    private void HandleTaskNotification(GameObject notif, NotificationData data)
    {
        if (activeTaskNotifications.ContainsKey(data.message))
        {
            Destroy(activeTaskNotifications[data.message]);
            activeTaskNotifications.Remove(data.message);
        }
        
        float targetY = CalculateNextYPosition();
        notif.transform.localPosition = new Vector3(offScreenLeftX, targetY, 0);
        activeTaskNotifications[data.message] = notif;
        notif.transform.DOLocalMoveX(taskVisibleXPosition, 0.5f).SetEase(Ease.OutQuad);
    }

    private IEnumerator HandleSideNotification(GameObject notif, NotificationData data, bool isRightSide)
    {
        RectTransform rect = notif.GetComponent<RectTransform>();
        
        float startX = isRightSide ? offScreenRightX : offScreenLeftX;
        float targetX = isRightSide ? batteryTargetX : hintTargetX;
        float targetY = isRightSide ? batteryTargetY : hintTargetY;

        rect.pivot = isRightSide ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        notif.transform.localPosition = new Vector3(startX, targetY, 0);

        notif.transform.DOLocalMoveX(targetX, 0.5f).SetEase(Ease.OutQuad);

        yield return StartCoroutine(WaitForKeyOrTime(data.closeKey, 3f));

        if (notif != null) 
            notif.transform.DOLocalMoveX(startX, 0.5f).SetEase(Ease.InQuad).OnComplete(() => Destroy(notif));
    }

    #endregion
}

public struct NotificationData
{
    public NotificationType type;
    public string message;
    public KeyCode closeKey;

    public NotificationData(NotificationType type, string message, KeyCode closeKey = KeyCode.None)
    {
        this.type = type;
        this.message = message;
        this.closeKey = closeKey;
    }
}

public enum NotificationType
{
    Ipucu,
    GorevHatirlatma,
    Uyari,
    Warning, // Not: "Uyari" ile tamamen aynı işlevi görüyor, projede birini silmek isteyebilirsin.
    Bilgilendirme,
    PilBildirimi
}
