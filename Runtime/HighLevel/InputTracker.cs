using System;
using System.Collections.Generic;
using Xasu.Exceptions;
using TinCan;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;
#endif


namespace Xasu.HighLevel
{
	public class InputTracker : AbstractSeriousGameHighLevelTracker<InputTracker>
	{
		/**********************
        *       Verbs
        * *******************/
		public enum Verb
		{
			Pressed,
			Released
		}

		public Dictionary<Enum, string> verbIds = new Dictionary<Enum, string>()
		{
			{ Verb.Pressed,   "https://w3id.org/xapi/seriousgames/verbs/pressed"  },
			{ Verb.Released,  "https://w3id.org/xapi/seriousgames/verbs/released" }
		};

		protected override Dictionary<Enum, string> VerbIds => verbIds;

		/**********************
        *   Input Types
        * *******************/

		public enum InputType
		{
			Screen,
			Touchscreen,
			Keyboard,
			Mouse,
			Button
		}

		private readonly Dictionary<Enum, string> typeIds = new Dictionary<Enum, string>
		{
			{ InputType.Screen,      "https://w3id.org/xapi/seriousgames/activity-types/screen"      },
			{ InputType.Touchscreen, "https://w3id.org/xapi/seriousgames/activity-types/touchscreen" },
			{ InputType.Keyboard,    "https://w3id.org/xapi/seriousgames/activity-types/keyboard"    },
			{ InputType.Mouse,       "https://w3id.org/xapi/seriousgames/activity-types/mouse"       },
            // Button does not appear in the official xAPI specification but is added as a common generic input type for game analytics.
            { InputType.Button,      "https://w3id.org/xapi/seriousgames/activity-types/button"      }
		};

		protected override Dictionary<Enum, string> TypeIds => typeIds;

		/**********************
        *   Extensions
        * *******************/

		public enum Extensions
		{
			// Empty, as no specific extensions were requested for inputs, but required by the abstract class.
		}

		private readonly Dictionary<Enum, string> extensionIds = new Dictionary<Enum, string>();

		protected override Dictionary<Enum, string> ExtensionIds => extensionIds;

		/**********************
        * Static attributes
        * *******************/

		private static Dictionary<string, DateTime> pressedTimes = new Dictionary<string, DateTime>();

		/// <summary>
		/// Player pressed an input. Defaults to Button.
		/// </summary>
		/// <param name="inputId">Input identifier.</param>
		public StatementPromise Pressed(string inputId)
		{
			return Pressed(inputId, InputType.Button);
		}

		/// <summary>
		/// Player pressed an input.
		/// </summary>
		/// <param name="inputId">Input identifier.</param>
		/// <param name="type">Input type.</param>
		public StatementPromise Pressed(string inputId, InputType type)
		{
			bool addPressedTime = true;
			if (pressedTimes.ContainsKey(inputId))
			{
				if (XasuTracker.Instance.TrackerConfig.StrictMode)
				{
					throw new XApiException($"The pressed statement for the specified id '{inputId}' has already been sent!");
				}
				else
				{
					if(XasuTracker.Instance.EnableDebugLogging)
                        Debug.Log($"[XASU][Warning] The pressed statement for the specified id '{inputId}' has already been sent!");
					addPressedTime = false;
				}
			}

			if (addPressedTime)
			{
				pressedTimes.Add(inputId, DateTime.Now);
			}

			return Enqueue(new Statement
			{
				verb = GetVerb(Verb.Pressed),
				target = GetTargetActivity(inputId, type)
			});
		}

		/// <summary>
		/// Player released an input. Defaults to Button.
		/// </summary>
		/// <param name="inputId">Input identifier.</param>
		public StatementPromise Released(string inputId)
		{
			return Released(inputId, InputType.Button, false, 0f);
		}

		/// <summary>
		/// Player released an input.
		/// </summary>
		/// <param name="inputId">Input identifier.</param>
		/// <param name="type">Input type.</param>
		public StatementPromise Released(string inputId, InputType type)
		{
			return Released(inputId, type, false, 0f);
		}

		/// <summary>
		/// Player released an input after a specific duration. Defaults to Button.
		/// </summary>
		/// <param name="inputId">Input identifier.</param>
		/// <param name="durationInSeconds">Duration the input was held.</param>
		public StatementPromise Released(string inputId, float durationInSeconds)
		{
			return Released(inputId, InputType.Button, true, durationInSeconds);
		}

		/// <summary>
		/// Player released an input after a specific duration.
		/// </summary>
		/// <param name="inputId">Input identifier.</param>
		/// <param name="type">Input type.</param>
		/// <param name="durationInSeconds">Duration the input was held.</param>
		public StatementPromise Released(string inputId, InputType type, float durationInSeconds)
		{
			return Released(inputId, type, true, durationInSeconds);
		}

		/// <summary>
		/// Private helper to process the release and calculate the duration held.
		/// </summary>
		private StatementPromise Released(string inputId, InputType type, bool hasDuration, float durationInSeconds)
		{
			if (!hasDuration && !pressedTimes.ContainsKey(inputId))
			{
				if (XasuTracker.Instance.TrackerConfig.StrictMode)
				{
					throw new XApiException($"The released statement for the specified id '{inputId}' has not been pressed!");
				}
				else
				{
					hasDuration = true;
					durationInSeconds = 0f;

                    if (XasuTracker.Instance.EnableDebugLogging)
                        Debug.Log($"[XASU][Warning] The released statement for the specified id '{inputId}' has not been pressed and therefore the duration is going to be 0.");
				}
			}

			// Get the pressed statement time to calculate how long the input was held down
			TimeSpan duration = hasDuration ? TimeSpan.FromSeconds(durationInSeconds) : DateTime.Now - pressedTimes[inputId];
			if (pressedTimes.ContainsKey(inputId))
			{
				pressedTimes.Remove(inputId);
			}

			return Enqueue(new Statement
			{
				verb = GetVerb(Verb.Released),
				target = GetTargetActivity(inputId, type)
			})
			// Attach how long the input was held for analytics
			.WithTimeSpanDuration(duration);
		}
	}

#if ENABLE_INPUT_SYSTEM
	public static class InputTrackerExtensions
    {
		private static readonly Dictionary<InputAction, Action<CallbackContext>> registeredPresses = new Dictionary<InputAction, Action<CallbackContext>>();
        private static readonly Dictionary<InputAction, Action<CallbackContext>> registeredReleases = new Dictionary<InputAction, Action<CallbackContext>>();
        private static readonly Dictionary<InputAction, Action<StatementPromise>> onTraceSentCallbacks = new Dictionary<InputAction, Action<StatementPromise>>();

        /// <summary>
        /// Extension method to easily add input tracking to Unity's new Input System actions.
        /// </summary>
        public static void RegisterAnalytics(this InputAction inputAction, Action<StatementPromise> onTraceSent = null)
        {
			if(registeredPresses.ContainsKey(inputAction) || registeredReleases.ContainsKey(inputAction))
			{
				throw new InvalidOperationException($"The input action '{inputAction.name}' is already registered for analytics. Please unregister it before registering again.");
            }

			inputAction.started += registeredPresses[inputAction] = SendPressed;
            inputAction.canceled += registeredReleases[inputAction] = SendReleased;
			onTraceSentCallbacks[inputAction] = onTraceSent;
        }

		public static void RegisterAnalytics(this InputAction inputAction, string name, Action<StatementPromise> onTraceSent = null)
        {
            if (registeredPresses.ContainsKey(inputAction) || registeredReleases.ContainsKey(inputAction))
            {
                throw new InvalidOperationException($"The input action '{inputAction.name}' is already registered for analytics. Please unregister it before registering again.");
            }
			inputAction.started += registeredPresses[inputAction] = (context) => SendPressed(context, name);
            inputAction.canceled += registeredReleases[inputAction] = (context) => SendReleased(context, name);
            onTraceSentCallbacks[inputAction] = onTraceSent;

        }

		public static void UnregisterAnalytics(this InputAction inputAction)
        {
			if(!registeredPresses.ContainsKey(inputAction) || !registeredReleases.ContainsKey(inputAction))
            {
                throw new InvalidOperationException($"The input action '{inputAction.name}' is not registered for analytics. Please register it before trying to unregister.");
            }

            inputAction.started -= registeredPresses[inputAction];
			registeredPresses.Remove(inputAction);
            inputAction.canceled -= registeredReleases[inputAction];
            registeredReleases.Remove(inputAction);

			if(onTraceSentCallbacks.ContainsKey(inputAction))
                onTraceSentCallbacks.Remove(inputAction);
        }

        private static void SendPressed(CallbackContext context) => SendPressed(context, context.action.name);
        private static void SendPressed(CallbackContext context, string name)
        {
            var promise = InputTracker.Instance.Pressed(name, InputTypeFromControl(context.control));
			if (onTraceSentCallbacks.ContainsKey(context.action) && onTraceSentCallbacks[context.action] != null)
            {
                onTraceSentCallbacks[context.action].Invoke(promise);
            }
        }

        private static void SendReleased(CallbackContext context) => SendReleased(context, context.action.name);
        private static void SendReleased(CallbackContext context, string name)
        {
            var promise = InputTracker.Instance.Released(name, InputTypeFromControl(context.control));
            if (onTraceSentCallbacks.ContainsKey(context.action) && onTraceSentCallbacks[context.action] != null)
            {
                onTraceSentCallbacks[context.action].Invoke(promise);
            }
        }

        private static InputTracker.InputType InputTypeFromControl(InputControl control)
        {
            if (control is Keyboard)
                return InputTracker.InputType.Keyboard;
            if (control is Mouse)
                return InputTracker.InputType.Mouse;
            if (control is Touchscreen)
                return InputTracker.InputType.Touchscreen;
            // Default to Button if the control type is not recognized
            return InputTracker.InputType.Button;
        }
    }
#endif
}