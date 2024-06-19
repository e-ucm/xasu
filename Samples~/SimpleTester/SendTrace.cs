using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Xasu;
using Xasu.HighLevel;
using System.Threading.Tasks;
using UnityEngine.UI;

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
        await GameObjectTracker.Instance.Interacted("boton-principal");
        interacted.interactable = true;
    }

    public async void Progressed()
    {
        progressed.interactable = false;
        await CompletableTracker.Instance.Progressed("MyGame", CompletableTracker.CompletableType.Game, 0.5f);
        progressed.interactable = true;

    }
    public async void Completed()
    {
        completed.interactable = false;
        await CompletableTracker.Instance.Completed("MyGame", CompletableTracker.CompletableType.Game).WithSuccess(false);
        completed.interactable = true;
    }
    public async void Finalized()
    {
        finalized.interactable = false;
        await CompletableTracker.Instance.Progressed("MyGame", CompletableTracker.CompletableType.Game, 1f);
        await CompletableTracker.Instance.Completed("MyGame", CompletableTracker.CompletableType.Game).WithSuccess(true);
        buttons.interactable = false;
    }
}
