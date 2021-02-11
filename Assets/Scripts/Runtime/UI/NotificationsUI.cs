using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class NotificationsUI : MonoBehaviour
{
    public int maxOnScreen = 5;
    public float expiryTime = 5;
    public TMP_Text label;

    private readonly List<(string text, float expiryTime)> notifications = new List<(string msg, float expiryTime)>();

    //private static NotificationsUI instance;
    private static readonly List<string> pendingNotifications = new List<string>();

    //private void Awake() => instance = ComponentCache..FindObjectOfType<NotificationsUI>();

    public static void Add(string text) => pendingNotifications.Add(text);

    private void Update()
    {
        this.notifications.RemoveAll(t => t.expiryTime < Time.time);
        while (this.notifications.Count < this.maxOnScreen && pendingNotifications.Count > 0)
        {
            this.notifications.Add((pendingNotifications.First(), Time.time + this.expiryTime));
            pendingNotifications.RemoveAt(0);
        }

        this.label.text = string.Join("\n", this.notifications.Select(t => t.text));
    }
}