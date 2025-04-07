using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Xasu;
using Xasu.HighLevel;

public class SendTrace : MonoBehaviour
{
    public CanvasGroup buttons;
    public Button interacted, progressed, completed, finalized;

    public async void Start()
    {
        await Task.Yield();
        buttons.interactable = false;
        while(XasuTracker.Instance.Status.State == TrackerState.Uninitialized)
        {
            await Task.Yield();
        }

        Debug.Log("Sending Initialized trace");

        await CompletableTracker.Instance.Initialized("MyGame", CompletableTracker.CompletableType.Game);

        Debug.Log("Done!");

        interacted.onClick.AddListener(Interacted);
        progressed.onClick.AddListener(Progressed);
        completed.onClick.AddListener(Completed);
        finalized.onClick.AddListener(Finalized);

        buttons.interactable = true;
    }

    public async void Interacted()
    {
        interacted.interactable = false;
        Debug.Log("Sending Interacted trace");
        await GameObjectTracker.Instance.Interacted("boton-principal");
        Debug.Log("Done!");
        interacted.interactable = true;
    }

    public async void Progressed()
    {
        progressed.interactable = false;
        Debug.Log("Sending Progressed trace");
        await CompletableTracker.Instance.Progressed("MyGame", CompletableTracker.CompletableType.Game, 0.5f);
        Debug.Log("Done!");
        progressed.interactable = true;

    }
    public async void Completed()
    {
        completed.interactable = false;
        Debug.Log("Sending Completed trace");
        await CompletableTracker.Instance.Completed("MyGame", CompletableTracker.CompletableType.Game).WithSuccess(false);
        Debug.Log("Done!");
        completed.interactable = true;
    }
    public async void Finalized()
    {
        finalized.interactable = false;
        Debug.Log("Sending Finalize trace");
        await CompletableTracker.Instance.Progressed("MyGame", CompletableTracker.CompletableType.Game, 1f);
        await CompletableTracker.Instance.Completed("MyGame", CompletableTracker.CompletableType.Game).WithSuccess(true);
        Debug.Log("Done!");
        buttons.interactable = false;
        var progress = new Progress<float>();
        progress.ProgressChanged += (_, p) =>
        {
            Debug.Log("Finalization progress: " + p);
        };
        await Xasu.XasuTracker.Instance.Finalize(progress);
        Debug.Log("Tracker finalized, game is now ready to close...");
        if (Application.isEditor) {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        } else {
            Application.Quit();
        }
    }
}
