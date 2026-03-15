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

    [Header("Sol Taraf (Görev) Ayarları")]
    [SerializeField] private float taskVisibleXPosition = -920f;
    [SerializeField] private float startYOffset = 400f;
    [SerializeField] private float spacingBetweenTasks = 15f;

    [Header("Sol Taraf (Normal İpucu) Ayarları")]
    [SerializeField] private float hintTargetX = -1068f;
    [SerializeField] private float hintTargetY = -270f;

    [Header("Sağ Taraf (Pil vb.) Ayarları")]
    [Tooltip("Ekranın sağında duracağı X konumu (Pozitif olmalı, örn: 800-900)")]
    [SerializeField] private float batteryTargetX = 920f;
    [Tooltip("Ekranın sağında duracağı Y konumu")]
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

        GameObject canvasObj = GameObject.Find("UI_NotificationCanvas");
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
        if (activeTaskNotifications.ContainsKey(taskId))
        {
            var notif = activeTaskNotifications[taskId];
            activeTaskNotifications.Remove(taskId);

            if (notif != null)
            {
                notif.transform.DOLocalMoveX(-1500, 0.5f).SetEase(Ease.InQuad).OnComplete(() => {
                    Destroy(notif);
                    RealignNotifications();
                });
            }
        }
    }

    public bool UpdateTaskNotificationVisuals(string originalMessage, string newText)
    {
        if (activeTaskNotifications.ContainsKey(originalMessage))
        {
            GameObject notif = activeTaskNotifications[originalMessage];
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
            Image panel = notif.GetComponent<Image>();
            TextMeshProUGUI text = notif.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = data.message;

            LayoutRebuilder.ForceRebuildLayoutImmediate(notif.GetComponent<RectTransform>());

            // --- TİP KONTROLLERİ ---

            // 1. UYARI (WARNING) - ORTA ALT
            if (data.type == NotificationType.Warning)
            {
                RectTransform rect = notif.GetComponent<RectTransform>();
                rect.pivot = new Vector2(0.5f, 0.5f);
                notif.transform.localPosition = new Vector3(0, -600, 0);

                if (text != null) text.color = Color.red;

                notif.transform.DOLocalMoveY(-350f, 0.5f).SetEase(Ease.OutBack);

                if (data.closeKey != KeyCode.None)
                {
                    while (!Input.GetKeyDown(data.closeKey)) { yield return null; }
                }
                else
                {
                    yield return new WaitForSeconds(4f);
                }

                if (notif != null) notif.transform.DOLocalMoveY(-700f, 0.5f).SetEase(Ease.InBack).OnComplete(() => Destroy(notif));
            }

            // 2. GÖREV (QUEST) - SOL LİSTE
            else if (data.type == NotificationType.GorevHatirlatma)
            {
                if (activeTaskNotifications.ContainsKey(data.message))
                {
                    Destroy(activeTaskNotifications[data.message]);
                    activeTaskNotifications.Remove(data.message);
                }
                float targetY = CalculateNextYPosition();
                notif.transform.localPosition = new Vector3(-1500, targetY, 0);
                activeTaskNotifications[data.message] = notif;
                notif.transform.DOLocalMoveX(taskVisibleXPosition, 0.5f).SetEase(Ease.OutQuad);
            }

            // 3. BİLGİLENDİRME (TUTORIAL)
            else if (data.type == NotificationType.Bilgilendirme)
            {
                RectTransform rect = notif.GetComponent<RectTransform>();
                rect.pivot = new Vector2(0.5f, 0.5f);
                notif.transform.localPosition = new Vector3(0, -600, 0);
                if (text != null) text.color = Color.white;
                notif.transform.DOLocalMoveY(-350f, 0.5f).SetEase(Ease.OutBack);

                if (data.closeKey != KeyCode.None)
                {
                    while (!Input.GetKeyDown(data.closeKey)) { yield return null; }
                }
                else
                {
                    yield return new WaitForSeconds(4f);
                }

                if (notif != null) notif.transform.DOLocalMoveY(-700f, 0.5f).SetEase(Ease.InBack).OnComplete(() => Destroy(notif));
            }

            // 4. PİL BİLDİRİMİ (SAĞ TARAFTAN ÇIKAN) --- YENİ ---
            else if (data.type == NotificationType.PilBildirimi)
            {
                // Pivot'u Sağ-Üst (1, 1) yapıyoruz ki yazı uzadıkça sola doğru büyüsün, ekran dışına taşmasın.
                RectTransform rect = notif.GetComponent<RectTransform>();
                rect.pivot = new Vector2(1f, 1f);

                // Başlangıç: Ekranın SAĞ dışında (1600)
                notif.transform.localPosition = new Vector3(1600, batteryTargetY, 0);

                // Harekete geç: Ekranın SAĞ tarafındaki hedefe (batteryTargetX, örn: 900)
                notif.transform.DOLocalMoveX(batteryTargetX, 0.5f).SetEase(Ease.OutQuad);

                if (data.closeKey != KeyCode.None)
                {
                    while (!Input.GetKeyDown(data.closeKey)) { yield return null; }
                }
                else
                {
                    yield return new WaitForSeconds(3f);
                }

                // Geri dön: Tekrar sağa (1600)
                if (notif != null) notif.transform.DOLocalMoveX(1600, 0.5f).SetEase(Ease.InQuad).OnComplete(() => Destroy(notif));
            }

            // 5. NORMAL İPUCU (SOL TARAFTAN ÇIKAN)
            else
            {
                notif.transform.localPosition = new Vector3(-1600, hintTargetY, 0);
                notif.transform.DOLocalMoveX(hintTargetX, 0.5f).SetEase(Ease.OutQuad);

                if (data.closeKey != KeyCode.None)
                {
                    while (!Input.GetKeyDown(data.closeKey)) { yield return null; }
                }
                else
                {
                    yield return new WaitForSeconds(3f);
                }

                if (notif != null) notif.transform.DOLocalMoveX(-1600, 0.5f).SetEase(Ease.InQuad).OnComplete(() => Destroy(notif));
            }

            if (data.type != NotificationType.GorevHatirlatma) yield return new WaitForSeconds(0.5f);

            isShowing = false;
        }
    }
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