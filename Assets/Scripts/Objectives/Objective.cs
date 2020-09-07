using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Objective : MonoBehaviour
{
    protected GUIStyle inactiveStyle;
    protected GUIStyle activeStyle;
    protected GUIStyle completeStyle;
    protected GUIStyle failedStyle;

    protected GUIStyle style =>
        this.failed ? this.failedStyle :
        this.complete ? this.completeStyle :
        this.active ? this.activeStyle :
        this.inactiveStyle;

    void Awake()
    {
        var baseStyle = new GUIStyle {
            fontSize = 30,
        };

        this.inactiveStyle = new GUIStyle(baseStyle);
        this.inactiveStyle.normal.textColor = Color.white;

        this.activeStyle = new GUIStyle(baseStyle);
        this.activeStyle.normal.textColor = new Color(0.4f, 0.5f, 1.0f);

        this.completeStyle = new GUIStyle(baseStyle);
        this.completeStyle.normal.textColor = new Color(0.4f, 1.0f, 0.5f);

        this.failedStyle = new GUIStyle(baseStyle);
        this.failedStyle.normal.textColor = new Color(1.0f, 0.5f, 0.5f);
    }

    public abstract Transform target { get; }
    public abstract float radius { get; }
    public abstract float amountRequired { get; }
    public abstract float amountDone { get; }
    public abstract bool required { get; }
    public abstract float score { get; }
    public abstract bool active { get; }
    public abstract GameObject uiAsset { get; }

    public virtual bool failed => false;
    public virtual bool complete => this.amountDone >= this.amountRequired;
}
