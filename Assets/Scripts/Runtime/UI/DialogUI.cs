using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogUI : MonoBehaviour
{
    [Flags]
    public enum Buttons
    {
        None = 0,
        Okay = 1 << 0,
        Cancel = 1 << 1,
        OkayCancel = Okay | Cancel
    }
    public TMP_Text content;
    public Button okayButton;
    public Button cancelButton;

    [NonSerialized]
    public Buttons result;

    public async Task<Buttons> Show(string content, Buttons buttons = Buttons.Okay)
    {
        if (buttons.HasFlag(Buttons.Okay))
        {
            this.okayButton.onClick.AddListener(() => this.result = Buttons.Okay);
            this.okayButton.gameObject.SetActive(true);
        }
        if (buttons.HasFlag(Buttons.Cancel))
        {
            this.cancelButton.onClick.AddListener(() => this.result = Buttons.Cancel);
            this.cancelButton.gameObject.SetActive(true);
        }
        this.content.text = content;
        this.result = Buttons.None;
        await new WaitUntil(() => this.result != Buttons.None);

        return this.result;
    }
}
