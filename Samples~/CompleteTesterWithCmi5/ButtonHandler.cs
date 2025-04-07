using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Xasu;
using Xasu.CMI5;
using Xasu.HighLevel;

public class ButtonHandler : MonoBehaviour
{
    float initTime;
    public Slider slider;
    public CanvasGroup cmi5Stuff;

    private async void Start()
    {
        initTime = Time.realtimeSinceStartup;

        if (!XasuTracker.Instance.AutoStart)
        {
            await XasuTracker.Instance.Init();
        }

        while (XasuTracker.Instance.Status.State == TrackerState.Uninitialized)
        {
            await Task.Yield();
        }

        if (!Cmi5Helper.IsEnabled)
        {
            XasuTracker.Instance.DefaultActor = new TinCan.Agent
            {
                name = "TestUser",
                account = new TinCan.AgentAccount
                {
                    homePage = "https://example.org",
                    name = new Guid().ToString()
                }
            };
        }

    }

    public void Update()
    {
        cmi5Stuff.interactable = Cmi5Helper.IsEnabled;
    }

    public async void SendOk()
    {
        await SendOkTask();
    }

    private async Task SendOkTask()
    {
        try
        {
            await Task.Yield();
            var sp = GameObjectTracker.Instance.Used("mesita");
            var statement = await sp.Promise;
            Debug.Log("Completed statement sent with id: " + sp.Statement.id);
        }
        catch (AggregateException apiEx)
        {
            Debug.Log("Failed! " + apiEx.GetType().ToString());
            foreach (var ex in apiEx.InnerExceptions)
            {
                Debug.Log("Inner! " + ex.GetType().ToString());
            }
        }
    }

    public async void Send25Ok()
    {
        var tasks = new List<Task>();
        for (int i = 0; i < 25; i++)
        {
            tasks.Add(SendOkTask());
        }
        await Task.WhenAll(tasks);

        Debug.Log("Done sending 25 traces!");
    }
    public async void Send500Ok()
    {
        var tasks = new List<Task>();
        for (int i = 0; i < 500; i++)
        {
            tasks.Add(SendOkTask());
        }
        await Task.WhenAll(tasks);

        Debug.Log("Done sending 500 traces!");
    }

    public async void Send24OkAnd1Bad()
    {
        var tasks = new List<Task>();
        for (int i = 0; i < 24; i++)
        {
            tasks.Add(SendOkTask());
        }
        tasks.Add(SendBadTask());
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (AggregateException apiEx)
        {
            Debug.Log("Failed! " + apiEx.GetType().ToString());
            foreach (var ex in apiEx.InnerExceptions)
            {
                Debug.Log("Inner! " + ex.GetType().ToString());
            }
        }

        Debug.Log("Done sending 24 traces + 1 Bad!");
    }

    public async void Send499OkAnd1Bad()
    {
        var tasks = new List<Task>();
        for (int i = 0; i < 499; i++)
        {
            tasks.Add(SendOkTask());
        }
        tasks.Add(SendBadTask());
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (AggregateException apiEx)
        {
            Debug.Log("Failed! " + apiEx.GetType().ToString());
            foreach (var ex in apiEx.InnerExceptions)
            {
                Debug.Log("Inner! " + ex.GetType().ToString());
            }
        }

        Debug.Log("Done sending 499 traces + 1 Bad!");
    }

    public async void Send25Random()
    {
        var tasks = new List<Task>();
        for (int i = 0; i < 25; i++)
        {
            if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
            {
                tasks.Add(SendOkTask());
            }
            else
            {
                tasks.Add(SendBadTask());
            }
        }
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (AggregateException apiEx)
        {
            Debug.Log("Failed! " + apiEx.GetType().ToString());
            foreach (var ex in apiEx.InnerExceptions)
            {
                Debug.Log("Inner! " + ex.GetType().ToString());
            }
        }

        Debug.Log("Done sending 25 random traces!");
    }

    public async void Send500Random()
    {
        var tasks = new List<Task>();
        for (int i = 0; i < 500; i++)
        {
            if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
            {
                tasks.Add(SendOkTask());
            }
            else
            {
                tasks.Add(SendBadTask());
            }
        }
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (AggregateException apiEx)
        {
            Debug.Log("Failed! " + apiEx.GetType().ToString());
            foreach (var ex in apiEx.InnerExceptions)
            {
                Debug.Log("Inner! " + ex.GetType().ToString());
            }
        }

        Debug.Log("Done sending 500 random traces!");
    }

    public async void SendBad()
    {
        await SendBadTask();
    }

    private async Task SendBadTask()
    {
        try
        {
            await Task.Yield();
            var sp = GameObjectTracker.Instance.Interacted("mesita");
            sp.Statement.actor = new TinCan.Agent { name = "naruto " };
            var statement = await sp.Promise;
            Debug.Log("Completed statement sent with id: " + sp.Statement.id);
        }
        catch (AggregateException apiEx)
        {
            Debug.Log("Failed! " + apiEx.GetType().ToString());
            foreach (var ex in apiEx.InnerExceptions)
            {
                Debug.Log("Inner! " + ex.GetType().ToString());
            }
        }
    }

    public async void SendCompleted()
    {
        var statement = await Cmi5Tracker.Instance.Completed(Time.realtimeSinceStartup - initTime);

        Debug.Log("Completed statement sent with id: " + statement.id);
    }

    public async void SendPassedOrFailed()
    {
        var score = slider.value;
        if (score < Cmi5Helper.MasteryScore)
        {
            var statement = await Cmi5Tracker.Instance.Failed(score, Time.realtimeSinceStartup - initTime);
            Debug.Log("Failed statement sent with id: " + statement.id);
        }
        else
        {
            var statement = await Cmi5Tracker.Instance.Passed(score, Time.realtimeSinceStartup - initTime);
            Debug.Log("Passed statement sent with id: " + statement.id);
        }
    }

    public async void SendFailed()
    {
        var statement = await Cmi5Tracker.Instance.Failed(0.1f, Time.realtimeSinceStartup - initTime);

        Debug.Log("Failed statement sent with id: " + statement.id);
    }

    public async void SendPassed()
    {
        var statement = await Cmi5Tracker.Instance.Passed(1f, Time.realtimeSinceStartup - initTime);
        Debug.Log("Passed statement sent with id: " + statement.id);
    }

    public async void SendTrace()
    {
        var statement = await FindObjectOfType<XasuTracker>().Enqueue(new TinCan.Statement
        {
            verb = new TinCan.Verb
            {
                id = new System.Uri("http://placeholder.test/boton_puslado")
            },
            target = new TinCan.Activity
            {
                id = "http://juego_de_test"
            }
        });

        Debug.Log("Statement sent with id: " + statement.id);
    }

    public async void FinalizeTracker()
    {
        var progress = new Progress<float>();
        progress.ProgressChanged += (_, p) =>
        {
            Debug.Log("Finalization progress: " + p);
        };
        await FindObjectOfType<XasuTracker>().Finalize(progress);
        Debug.Log("Tracker finalized");
        if (Application.isEditor) {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
        } else {
                Application.Quit();
        }
    }
}
