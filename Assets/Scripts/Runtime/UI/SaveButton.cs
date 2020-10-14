using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class SaveButton : MonoBehaviour
{
    public GameLogic gameLogic;
    public TMP_Text saveDescription;
    public Button deleteButton;

    int saveIndex;

    void Start()
    {
        this.saveIndex = Array.IndexOf(this.gameObject.transform.parent.GetComponentsInChildren<SaveButton>(), this);
        _ = this.UpdateStateAsync();
    }

    public async void LoadAsync() => await this.gameLogic.LoadGameAsync(this.saveIndex);

    public async void DeleteAsync()
    {
        await this.gameLogic.DeleteSaveAsync(this.saveIndex);
        await this.UpdateStateAsync();
    }

    public async Task UpdateStateAsync()
    {
        this.deleteButton.gameObject.SetActive(false);
        var metaData = await this.gameLogic.LoadMetadataAsync(this.saveIndex);
        if(metaData != null)
        {
            this.deleteButton.gameObject.SetActive(true);
            this.saveDescription.text = $"{metaData.systemName} @ {metaData.simTick / Time.fixedDeltaTime}";
        }
        else
        {
            this.saveDescription.text = "New";
        }
    }
}
