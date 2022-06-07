# Xasu - xAPI Analytics Supplier

The Xasu (xAPI Analytics Supplier) is a Unity (https://unity.com/) asset that simplifies use of [xAPI](https://xapi.com/) Learning Analytics from Unity with straightforward cmi5 support.

Xasu is a *tracker*, which, when integrated into a game, can send and/or store player interactions for later analysis. This is especially important when proving that a serious game is effective at its goal, be it teaching, training, or changing the players' perspective on real-world issues. As a tracker, Xasu simplifies the work of serious games developers (or of any application using LA) by providing both simple tracking and multipe quality-of-life utilities with which to configure the collection of xAPI traces in a flexible and robust way. Xasu has been developed by the [e-UCM group](https://www.e-ucm.es) and is part of the e-UCM ecosystem of tools for Learning Analytics (Simva, T-mon, Pumva, μfasa and Xasu).

# The "Super" in Xasu

The *su* in Xasu also stands for *super*, since it is:

- Super **Simple** (High-Level API): provides a high-level API that represents xAPI profiles for serious games and CMI-5. Using these APIs, the system will take care of setting up most of the trace structure automatically and with sane defaults. This reduces the learning curve with xAPI, while also allowing developers to refine the produced trace. 

- Super **Supportive** (multi-platform/protocol/cmi5): Xasu has been designed to run on Unity, respecting the nature of cross-platform games and environments (Windows, Mac, Linux, Android and WebGL, iOS incoming). In addition, Xasu supports multiple authorization protocols (basic/oauth/oauth2) and the cmi5 protocol for conducting courses and activities with xAPI in Learning Management Systems (LMSs).

- Super **Asyncronous** (uses async/await): Xasu's architecture provides a proper asynchronous queue to avoid interruptions that allows deveñpèrs to use the .NET asynchronous API (async/await) and to check on the result of sending synchronously-sent traces, even if the traces are sent in batches.

- Super **Flexible** (working modes/backups): Xasu can operate in different modes, including an online mode (connected to an LRS), offline mode (generating a local log file in xAPI or CSV), in fallback mode (hybrid online and local depending on connectivity), and in backup mode (generating and/or sending a single file with all traces at the end).

- Super **Reliable** (communication policies and error resiliency): Xasu uses retry policies with exponential backoff and circuit-breakers to provide recovery intervals and fallback mechanisms.

# Setting Up Xasu

Xasu requires at least **Unity 2019.4 (LTS)**.

## Installation

Xasu can be downloaded through the Unity Package Manager using the [repository link](https://github.com/e-ucm/xasu.git) of this project.

To add it to your proyect go to ``Window>Package Manager`` press the "+" icon and select ``Add package from git...``.
Insert ```https://github.com/e-ucm/xasu.git``` and press "Add".

If adding Xasu manually to your project (for example, by downloading the repository as a .zip), make sure you install also the NewtonSoft.JSON library using the Unity Package Manager.

## Setting up the configuration file

The Xasu configuration file contains settings for overall system tuning, LRS endpoint location, authorization protocols and options, and working mode selection. The tracker configuration can be provided either using the `StreamingAssets` folder (recommended) or via scripting. We recommend using the `StreamingAssets` folder to allow configuration to be changed after the game is exported, allowing simpler adaptation of the game to different scenarios without having to recompila the whole game.

### Minimal "tracker_config.json"

The configuration file must be present under

```path
Assets/StreamingAssets/tracker_config.json
```

The following configuration file represents the minimal tracker configuration required to send traces to an LRS using basic authentication.

```json
{
    "online": "true",
    "lrs_endpoint": "<your-lrs-endpoint>",
    "auth_protocol": "basic", 
    "auth_parameters": {
        "username": "<your-username>",
        "password": "<your-password>"
    }
}
```

### Authorization

When using Xasu with an LRS it is possible to use three different authorization protocols: Basic, OAuth (v1) and OAuth2. Each of the protocols have different configurations and may support multiple flows for different use cases. Each of these protocols will manipulate the requests made by Xazu to include the appropiate parameters (headers, query parameters, signatures, etc.) in the request to authorize it. 

For OAuth1 and some OAuth2 flows to work the game will prompt login windows and listen for the redirection at localhost:<random-port>. However, in WebGL, the website containing the game will be redirected to the SSO (Single-Sign-On) when the tracker initialization is started. Make sure you initialize Xasu at the start of your game or that you save the game state before initializing Xasu to prevent losing the player unsaved progress in WebGL scenarios. 

To continue the login process after the SSO returns back to the game just do the Initialization again and, if the SSO return parameters are detected, Xasu will continue with the rest of the process.

Details on each authorization protocol configuration can be found ahead.

#### No Authorization

Using the "none" auth protocol, Xasu will send the traces using no bearer at all. This mode can be used for testing purposes but it is discouraged in production.

```json
    "auth_protocol": "none"
```

#### Basic Authorization

The Basic authorization provides HTTP Authorization using username and password. This information is encoded and added in the "Authorization" header of every request.
Here's an example of the Basic protocol configuration:

```json
    "auth_protocol": "basic",
    "auth_parameters": {
        "username": "<your-username>",
        "password": "<your-password>"
    }
```

To authenticate the user using credentials obtained from the user you will need to introduce these settings using the alternative configuration setup.

#### OAuth1.0 

The OAuth1.0 authorization is the other alternative recommended in the LRS specification. This protocol requires the user to introduce its credentials in a single-sign-on (SSO) website and forward the authorization to the game. 
Here's an example of the OAuth1.0 protocol configuration:

```json
    "auth_protocol": "oauth"
    "auth_parameters" {
        "request_token_endpoint": "<request-token-endpoint>", // AKA "initiate" endpoint
        "authorize_endpoint": "<authorize-endpoint>",
        "access_token_endpoint": "<access-token-endpoint>",
        "oauth_consumer_key": "<your-consumer-key>",
        "oauth_consumer_secret": "<your-consumer-secret>",
        "oauth_signature_method": "RSA-SHA1"  // PLAINTEXT is also supported
    }
```

#### OAuth2.0

Althought OAuth2.0 is not yet officially supported in the LRS specification, OAuth2.0 is the most secure and flexible authorization protocol and we encourage using it when available. 

Xasu supports both "code" (AKA client) flow, "password" (AKA Resource Owned Password Credentials) flow, and the "refresh-token" flow.

##### Client "code" flow with PKCE

The "code" flow is the OAuth2.0 flow used in client applications, where the configuration is included inside of the final product. Although it is possible to use the client secret, we discourage it as it could be readed or decompiled from the final product. 
Xasu also supports (and encourages) the use of PKCE in the client flow to guarantee the authorization is safe.

Here's an example of the OAuth2.0 protocol configuration with PKCE enabled.

```json
    "auth_protocol": "oauth2"
    "auth_parameters": {
        "grant_type": "code",
        "auth_endpoint": "<your-authorize-endpoint>", // AKA /auth
        "token_endpoint": "<your-token-endpoint>", // AKA /token
        "client_id": "<your-client-id>",
        "code_challenge_method": "S256" // PKCE code challenge
    },
```

##### Resource Owned Password Credentials "password" flow (ROPC)

The "password" flow is the OAuth2.0 flow used when the client can safely manipulate the user credentials. However, this method is discouraged and may be deprecated in the future. This method is appropiate for testing purposes.
Since this authorizationmethod avoids prompting SSO pages it can be helpful when the web browser is not accessible.

```json
    "auth_protocol": "oauth2"
    "auth_parameters": {
        "grant_type": "password",
        "token_endpoint": "<your-token-endpoint>", // AKA /token
        "client_id": "<your-client-id>",
        "username": "<your-username>",
        "password": "<your-password>"
    },
```

To authenticate the user using credentials obtained from the user (in gameplay time) you will need to introduce these settings using the alternative configuration setup.

### Working modes

There are three working modes: online (with or without fallback), local and backup. These modes allow different use cases for different stages of the game development and cycle of life. 
Enabling any working mode creates a new trace processor for such purpose. In each processor, traces are handled independently and thus, failing traces in one mode do not interact with other modes.

#### Online mode

The online mode sends batches of enqueued traces to an LRS using the POST /statements API. To send the traces its to support resiliency features such as Retry Pollicies, Circuit Breakers and Fallback modes. The default behaviour will retry any statement request that results in an http error except if it is included the list of handled codes below. For instance, the system will retry any 412/5xx response using an exponential backoff timing up to 5 times, after which the tracker will log the exception and the circuit will open. This circuit will try to close automatically after a period of 15 seconds. 

The amount of traces in the batch is dynamic, normally starting with up to 256 traces. The size of the batch can be reduced when an HTTP Error happen if it belongs to the list of handled codes. Once one handled error happens the batch will decrease (by half) until it is accepted or a single trace gets rejected. Once the error is isolated, it is notified to the user. With each succesfull submission, the batch will duplicate in size until it reaches its maximum size again. 

The following list of http errors are isolated by the tracker and returned to the user. These cases follow the xAPI-Communication specification, and could retrieve the following errors:
* Bad Request (Status 400): An APIException(400) is thrown when the trace doesn't follow the specification rules, resulting in the trace being rejected by the LRS.
* Conflict (409): An APIException(409) is thrown when the trace id is in conflict with another trace.
* Request Entity Too Large (412): An APIException(412) is thrown when the trace is too large and the LRS rejects it.

##### Fallback in the online mode

The fallback mode is a special working mode for error-prone environments. If the fallback mode is enabled, it will store the traces locally while any circuit is open. Normally, circuits will open when the network fails or the server returns any unhandled status such as 5xx errors. 

For the user, when the fallback mode is working the tracker will notify the user that the traces are being sent once they get written in the fallback file. In this fallback file traces are written one per line. This fallback file is located in:
```path
%TEMP%/<Company-Name>/<Game Name>/fallback.log
```

When any circuit in the processor is closed and the connection is restored or the API is responsive, Xasu will try to send the traces in the fallback to the LRS. 
If any error happens while sending the fallback, the failing traces will be stored in a special file in the following path:
```path
AppData/LocalLow/<Company-Name>/<Game Name>/failed_traces.log
```

To enable the fallback mode you need to use the following property in the configuration file:

```json
    "online": "true",
    "fallback": "true"
```

#### Local mode

The local mode saves traces in a permanent local file in the selected format. At the current version it is only possible to store the traces in xAPI format. However, the CSV format is one planned feature.
Traces in the local mode are stored in the following file:
```path
AppData/LocalLow/<Company-Name>/<Game Name>/traces.log
```

To enable the local mode and configure the output format include the following configuration lines:
```json
    "local": "true",
    "format": "XAPI"
```

#### Backup mode

The backup mode saves the traces in a temporal local file location for later submission or recollection. When the Xasu tracker is finalized at the end of the game, this backup can optionally be send automatically to an endpoint.

To enable the backup mode include the following configuration lines:

```json
    "backup": "true",
    "backup-format": "XAPI"
```

To enable the uploading you have to add the following lines:

```json
    "backup-endpoint": "<your-backup-endpoint>"
```

Optionally, the backup can use authorization protocols by adding the following lines. Note that using two different authorization protocols that use redirection is not yet supported in WebGL.

```json
    "backup-authorization": "<authorization-name>", // Optional authorization
    "backup-authorization-parameters": { /* Optional authorization parameters */ }
```

In addition to the normal authorization keywords, the "same" keyword can be used to use the same authorization from the online mode in the backup upload.

```json
    "backup-authorization": "same", // Optional authorization
```

Finally, the backup request (method, headers, content-type, query parameters, etc.) can be configured by using the following configuration lines.

```json
    "backup_request_config": { 
        /* Optional configuration parameters */ 
        "method": "POST", // GET, PUT, REMOVE, etc...
        "content_type": "application/json",
        "headers": {
            "<your-custom-header>": "<your-custom-header-value>",
            ...
        }
        "query_parameters": {
            "<your-custom-query-param>": "<your-custom-query-param-value>",
            ...
        }
    }
```

## Adding Xasu to your scene

Once Xasu is installed and configured, to add Xasu to your game you just have to create a new GameObject in Unity and include the Xasu component.

# Initializing Xasu

By default, Xasu starts up in a Idle (Created) state. In this state, Xasu is still in a blank state and is not ready to handle any traces.

To initialize it, there are two possibilities:
* Using the configuration file (recommended)
* Using the scripting API

## Using the configuration file

To initialize Xasu using the configuration file located in the StreamingAssets folder you just have to call to.

```cs
    await Xasu.Instance.Init();
```

Note that "Init" is an asynchronous task and thus await is the preferred method to continue after initialization.

## Using the scripting API

To initialize Xasu using the scripting API you have to mannually create the TrackerConfig object and pass it to the Init function.

```cs
    await Xasu.Instance.Init(new TrackerConfig
    {
        Online = true,
        LRSEndpoint = "https://my.lrs.endpoint/",
        AuthProtocol = "basic",
        AuthParameters = new Dictionary<string, string>
        {
            { "username", "your-username" },
            { "password", "your-password" }
        }
    });
```

# Sending xAPI Traces using Xasu

Trace submission from Xasu is done using an asynchronous queue. This prevents the game from freezing and allows Xasu managing the trace submission in batches, reducing the connection load and handling the different errors. In addition, it is still possible for the game to know when a trace is submitted and if any (handled) error happened while sending the specific trace.

There are two possibilities when sending traces:
* Using the High-Level API: Xasu High-Level API simplifies the trace creation by using static templates from the Serious Game xAPI Profile and the CMI-5 Profile.
* Using the TinCan.NET API: Xasu uses the TinCan.NET library to model the traces. Thus, custom traces created using this API can be sent using Xasu too.

Details on each case are found bellow.

## Using the High Level API

The High Level API is a simpler way to send xAPI traces that reduces the learning curve and can potentially reduce errors.

There are 4 different APIs for the Serious Games Profile and 1 more API for CMI-5.

To use any of the APIs make sure you include the appropiate namespace in your .cs files.

```cs
using UnityTracker.HighLevel;
```

Here's an example of how can you send one trace using Xasu High-Level API:

```cs
    GameObjectTracker.Instance.Interacted("mesita");
```

Any High Level API will return a TraceTask structure, including the enqueued trace (modifable) and the Task associated to its submission.
Since the trace is sent asynchronously, it is possible to use the async/await C# interface to await until the trace is sent. Errors from the trace submission will be thrown as AggregateException (since there could be multiple errors from the different working modes).

Below you can see how to manipulate the trace, await for its result and retrieve errors.
```cs
        try
        {
            var statementPromise = GameObjectTracker.Instance.Interacted("mesita");
            
            // Doing any modifications to the Statement in traceTask.Statement property is safe.

            var statement = await statementPromise.Promise;
            Debug.Log("Completed statement sent with id: " + statementPromise.Statement.id);
        }
        catch (AggregateException aggEx)
        {
            Debug.Log("Failed! " + aggEx.GetType().ToString());
            // You can check the inner exceptions from each working mode.
            foreach (var ex in aggEx.InnerExceptions)
            {
                Debug.Log("Inner Exception: " + ex.GetType().ToString());
            }
        }
```

Below you can see the rest of the APIs.

### Serious Games APIs from the xAPI for SGs profile

There are four different Serious Games APIs for sending traces related to serious games. 

#### Game Object API

The Game Object API is used to send traces when the player performs an interaction. 
The Interacted verb should be the main interaction type, but when the element is consumed, Used is more appropiate.

Some examples are listed below:
```cs
    // Interacted traces are sent when the player interacts with something
    GameObjectTracker.Instance.Interacted("alarm-trigger");
    
    // Interacted traces are sent when the player uses (and consumes) something
    GameObjectTracker.Instance.Used("potion");

    // Types of the elements can be specified for instance:
    GameObjectTracker.Instance.Interacted("john", GameObjectTracker.TrackedGameObject.Npc);
    GameObjectTracker.Instance.Interacted("grenade", GameObjectTracker.TrackedGameObject.Item);
    GameObjectTracker.Instance.Interacted("demon", GameObjectTracker.TrackedGameObject.Enemy);
```

Full list of TrackerGameObject types:
```cs
TrackedGameObject.GameObject
TrackedGameObject.Npc
TrackedGameObject.Item
TrackedGameObject.Enemy
```

#### Accessible API

The Accessible API is used to send a trace whenever the player accesses a new screen.
Appart from the Accessed verb, it is possible to send also Skipped traces when the screen is manually skipped.

Some examples are listed below:
```cs
    // Accessed traces are sent when the player accesses an screen
    AccessibleTracker.Instance.Accessed("stage-1");
    
    // Skipped traces are sent when the player skips an screen
    AccessibleTracker.Instance.Skipped("tutorial");

    // Types of the elements can be specified for instance:
    AccessibleTracker.Instance.Accessed("main-menu", AccessibleTracker.AccessibleType.Screen);
    AccessibleTracker.Instance.Skipped("tutorial", AccessibleTracker.AccessibleType.Cutscene);
    AccessibleTracker.Instance.Accessed("storage-box-1", AccessibleTracker.AccessibleType.Inventory);
```

Full list of Accessible types:
```cs
AccessibleType.Accessible;
AccessibleType.Area;
AccessibleType.Cutscene;
AccessibleType.Inventory;
AccessibleType.Screen;
AccessibleType.Zone;
```

#### Alternative API

The Alternative API is used to send a trace whenever the player makes an election.
Appart from the Selected verb, it is possible to send also Unlocked traces when an new option is unlocked in the game.

Some examples are listed below:
```cs
    // Accessed traces are sent when the player accesses an screen
    AlternativeTracker.Instance.Selected("alternative-1", "option-2");
    
    // Skipped traces are sent when the player skips an screen
    AlternativeTracker.Instance.Unlocked("alternative-1", "super-secret-option-3");

    // Types of the elements can be specified for instance:
    AlternativeTracker.Instance.Selected("main-menu", "start-game", AlternativeTracker.AlternativeType.Menu);
    AlternativeTracker.Instance.Unlocked("stage-1", "secret-door", AlternativeTracker.AlternativeType.Path);
    AlternativeTracker.Instance.Selected("dialog-1", "option-1", AlternativeTracker.AlternativeType.Dialog);

    // In this tracker is also recommended to include the success extension by using the simplified sintax
    AlternativeTracker.Instance.Selected("alternative-1", "option-2").WithSuccess(true);
```

Full list of Alternative types:
```cs
AlternativeType.Alternative;
AlternativeType.Arena;
AlternativeType.Dialog;
AlternativeType.Menu;
AlternativeType.Path;
AlternativeType.Question;
```

#### Completable API

The Completable API is the most abstract of the SGs APIs. It can be used to track the different tasks the player has to do in the game. 

A Completable can be Initialized, Progressed and Completed (verbs).

Some examples are listed below:
```cs
    CompletableTracker.Instance.Initialized("tutorial");
    CompletableTracker.Instance.Progressed("tutorial", 0.5f); // 50% progress
    CompletableTracker.Instance.Completed("tutorial").WithSuccess(true).WithScore(0.8f); // completed successfully with 0.8/1.0 score
```

Full list of Completable types:
```cs
CompletableType.Game,
CompletableType.Session,
CompletableType.Level,
CompletableType.Quest,
CompletableType.Stage,
CompletableType.Combat,
CompletableType.StoryNode,
CompletableType.Race,
CompletableType.Completable,
CompletableType.DialogNode,
CompletableType.DialogFragment
```

### CMI5 API

The CMI5 High-Level API is explained later in section.

## Using the TinCan.NET API

The TinCan.NET API is the most flexible API for sending traces.

More details about the TinCan.NET API can be found in their repository at https://rusticisoftware.github.io/TinCan.NET/

To use this API you must create an Statement and enqueue it in Xasu as explained below:
```cs
    var actor = new Agent();
    actor.mbox = "mailto:info@tincanapi.com";

    var verb = new Verb();
    verb.id = new Uri ("http://adlnet.gov/expapi/verbs/experienced");
    verb.display = new LanguageMap();
    verb.display.Add("en-US", "experienced");

    var activity = new Activity();
    activity.id = "http://rusticisoftware.github.io/TinCan.NET";

    var statement = new Statement();
    statement.actor = actor;
    statement.verb = verb;
    statement.target = activity;

    await Xasu.Instance.Enqueue(statement);

    Debug.LogFormatted("Statement {0} sent!", statement.id);
```

## Trace completion

When using Xasu, it is possible to avoid fulfilling some parameters in the traces when they are not present.
These parameters include:

* ID: The trace id is added using the .NET Guid library.
* Actor: The trace actor is added using the Xasu.Instance.DefaultActor value.
* Context: The trace context is added using the Xasu.Instance.DefaultContext value. This context is automatically configured when using CMI5 so traces are CMI5 allowed.
* Timestamp: When the trace has no timestamp, DateTime.Now is setted as Timestamp.

# Finalizing the tracker

Before the game is closed, Xasu has to be finalized manually so its processors (online, offline or backup) perform their final tasks.

These tasks include:
* Flushing all the queues in all processors.
* Sending all the online/fallback pending traces to the LRS (forcely requires internet connection to continue).
* Submitting the trace backup to the backup endpoint.
* Closing all the opened logs and connections.

To finalize Xasu, the Finalize function is used. The finalization progress can be measured using the IProgress interface.
```cs
    var progress = new Progress<float>();
    progress.ProgressChanged += (_, p) =>
    {
        Debug.Log("Finalization progress: " + p);
    };
    await Xasu.Instance.Finalize(progress);
    Debug.Log("Tracker finalized");
    Application.Quit();
```

# CMI5 usage in Xasu

Games using Xasu can be compatible with the cmi5 standard. According to cmi5 standard, any content can be lauched through URLScheme protocol from the LMS. This is the API Xasu uses to receive the cmi5 parameters.

For cmi5 to work, Xasu creates a custom URLScheme for the game based on the Unity Player configuration bundle name.
```
    <bundle-name>://
```

To bind this URLScheme to the system, Xasu uses different techniques depending on each platform. In summary:
* Windows: Xasu automatically creates an installer in the ```Installers``` folder of the Unity Project that adds the URLScheme to the registry and also will include a Launcher.exe file that helps with the parameter management.
* Linux: - Not yet supported -
* Mac: Xasu adds the URLScheme to the ```/Contents/Info.plist``` file of the MacOs build using the ```CFBundleURLSchemes``` tags.
* Android: Xasu adds the URLScheme to the ```AndroidManifest.xml``` file.
* iOS: Similar to Mac, Xasu adds the URLScheme in the ```Info.plist``` file.
* WebGL: Since WebGL is a native web platform, Xasu will read the parameters from the URL. 

Once the game is built and installed in the system (when installation is required), cmi5 can launch the game by using the cmi5.xml package manifest.

## Enable cmi5 via configuration file

To enable it in the configuration file you can use the following minimum configuration:

```json
{
    "online":"true",
    "auth-protocol": "cmi5
}
```

Note that neither "lrs-endpoint" nor "auth-protocol-parameters" are required to use cmi5.

## Creating the cmi5 package

NOTE: This process will be automated in future versions.

To launch the game from a cmi5 the Assignable Unit (AU) has to point to the game url. This is normally defined in the cmi5.xml package manifest.

This cmi5.xml package manifest is included inside of a Zip file along with the required contents.

### Creating a cmi5 package for Windows, Mac, Linux, Android and iOS

In this platforms, the game MUST be installed in the system manually. Afterwards, the user can use the cmi5 link to launch the assignable unit.

This cmi5 link is defined in the ```<url>``` tag of the ```<au>```. For cmi5 it has to follow the next format: ```<bundle-name>://cmi5/<url-encoded-exe-name>```

A minimal cmi5.xml can be found below:

```xml
<?xml version="1.0" encoding="utf-8"?>
<courseStructure xmlns="https://w3id.org/xapi/profiles/cmi5/v1/CourseStructure.xsd">
  <course id="http://e-ucm/examples/game-course">
    <title><langstring lang="en-US">Course Title</langstring></title>
    <description><langstring lang="en-US">Course description</langstring></description>
  </course>
  <au id="http://e-ucm/examples/game-au" moveOn="CompletedOrPassed" masteryScore="1.0">
    <title><langstring lang="en-US">Game Title</langstring></title>
    <description><langstring lang="en-US">Game Description</langstring></description>
    <url>com.DefaultCompany.XasuGame://cmi5/Xasu%20Game</url> <!-- <bundle-name>://cmi5/<url-encoded-exe-name> -->
  </au>
</courseStructure>
```

This cmi5.xml file is then compressed alone in a zip file and uploaded to any cmi5 compatible platform.

As long as the game is installed in the platform the web browser will ask to open the game.

### Creating a cmi5 self-hosted package for WebGL

In contrast to the other platforms, WebGL content can be run inside of the Web browser. For this reason, it is also possible to include the WebGL content in the cmi5 package and let the LMS host the content.

A Minimal cmi5.xml for WebGL content is found below. In contrast to the previous example, the url just points to the index.html file:
```xml
<?xml version="1.0" encoding="utf-8"?>
<courseStructure xmlns="https://w3id.org/xapi/profiles/cmi5/v1/CourseStructure.xsd">
  <course id="http://e-ucm/examples/game-course">
    <title><langstring lang="en-US">Course Title</langstring></title>
    <description><langstring lang="en-US">Course description</langstring></description>
  </course>
  <au id="http://e-ucm/examples/game-au" moveOn="CompletedOrPassed" masteryScore="1.0">
    <title><langstring lang="en-US">Game Title</langstring></title>
    <description><langstring lang="en-US">Game Description</langstring></description>
    <url>./index.html</url>
  </au>
</courseStructure>
```

The cmi5 package is then created by adding the cmi5.xml file into the WebGL build folder root (at the same level of the index.html file), and zipping (.zip) all together.

An example of the "package.zip" folder structure could be:
```
./Build/
./StreamingAssets/
./TemplateData/
./cmi5.xml
./index.html
```

# Important links

Referenced repositories:

* xAPI and LRS Specification: https://github.com/adlnet/xAPI-Spec
* TinCan.NET library: https://rusticisoftware.github.io/TinCan.NET/
* Polly.NET Resiliency library: https://github.com/App-vNext/Polly
* WixSharp.NET library for creating Windows Installers: https://github.com/oleg-shilo/wixsharp
