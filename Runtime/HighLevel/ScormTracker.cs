using System;
using System.Collections.Generic;
using TinCan;
using Xasu.Exceptions;
using Xasu.Processors;

namespace Xasu.HighLevel
{

    public class ScormTracker : AbstractHighLevelTracker<ScormTracker>
    {
        /**********************
        *       Verbs
        * *******************/
        public enum Verb
        {
            Initialized, // Player initialized a lesson.
            Suspended,   // Player suspended a lesson.
            Resumed,    // Player resumed a lesson.
            Terminated, // Player terminated a lesson.
            Progressed, // Player progressed in a lesson.
            Passed,     // Learner attempted and succeeded in a judged activity.
            Failed,      // Learner attempted and failed in a judged activity.
            Scored      // Player scored in a lesson.
        }

        public Dictionary<Enum, string> verbIds = new Dictionary<Enum, string>()
        {
            { Verb.Initialized,   "http://adlnet.gov/expapi/verbs/initialized" },
            { Verb.Suspended,     "http://adlnet.gov/expapi/verbs/suspended"   },
            { Verb.Resumed,       "http://adlnet.gov/expapi/verbs/resumed"     },
            { Verb.Terminated,    "http://adlnet.gov/expapi/verbs/terminated"  },
            { Verb.Progressed,    "http://adlnet.gov/expapi/verbs/progressed"  },
            { Verb.Passed,        "http://adlnet.gov/expapi/verbs/passed"      },
            { Verb.Failed,        "http://adlnet.gov/expapi/verbs/failed"      },
            { Verb.Scored,        "http://adlnet.gov/expapi/verbs/scored"      }
        };
        protected override Dictionary<Enum, string> VerbIds => verbIds;

        public enum ScormType
        {
            SCO,         // Sharable Content Object Reference Model
            Course,      // A course or curriculum.
            Module,      // A module within a course or curriculum.
            Assessment,  // An assessment or quiz.
            Interaction, // An interactive element, such as a game or simulation.
            Objective,   // A learning objective or goal.
            Attempt     // An attempt or submission of an activity.
        }

        /**********************
        *   Activity Types 
        * *******************/
        public Dictionary<Enum, string> typeIds = new Dictionary<Enum, string>()
            {
                { ScormType.SCO,             "http://adlnet.gov/expapi/activities/lesson"        },
                { ScormType.Course,          "http://adlnet.gov/expapi/activities/course"        },
                { ScormType.Module,          "http://adlnet.gov/expapi/activities/module"        },
                { ScormType.Assessment,      "http://adlnet.gov/expapi/activities/assessment"    },
                { ScormType.Interaction,     "http://adlnet.gov/expapi/activities/interaction"   },
                { ScormType.Objective,       "http://adlnet.gov/expapi/activities/objective"     },
                { ScormType.Attempt,         "http://adlnet.gov/expapi/activities/attempt"       },
            };
        protected override Dictionary<Enum, string> TypeIds => typeIds;

        protected override Dictionary<Enum, string> ExtensionIds => null;

        /**********************
        * Static attributes
        * *******************/

        private static Dictionary<string, DateTime> initializedTimes = new Dictionary<string, DateTime>();

        private static Dictionary<string, DateTime> suspendedTimes = new Dictionary<string, DateTime>();

        #region Initialized
        /// <summary>
        /// Initializes a SCORM lesson.
        /// </summary>
        /// <param name="scoId">The ID of the lesson to initialize.</param>
        public StatementPromise Initialized(string scoId)
        {
            bool addInitializedTime = true;
            if (initializedTimes.ContainsKey(scoId))
            {
                if (XasuTracker.Instance.TrackerConfig.StrictMode)
                {
                    throw new XApiException("The initialized statement for the specified id has already been sent!");
                }
                else
                {
                    XasuTracker.Instance.LogWarning("The initialized statement for the specified id has already been sent!");
                    addInitializedTime = false;
                }
            }

            if (addInitializedTime)
                initializedTimes.Add(scoId, DateTime.Now);
            return Enqueue(new Statement
            {
                verb = GetVerb(Verb.Initialized),
                target = GetTargetActivity(scoId, ScormType.SCO),
                context = XasuTracker.Instance.DefaultContext
            });
        }
        #endregion

        #region Suspended
        /// <summary>
        /// Suspends a SCORM lesson.
        /// </summary>
        /// <param name="scoId">Identifier of the suspended lesson.</param>
        public StatementPromise Suspended(string scoId)
        {
            bool addSuspendedTime = true;
            if (suspendedTimes.ContainsKey(scoId))
            {
                if (XasuTracker.Instance.TrackerConfig.StrictMode)
                {
                    throw new XApiException("The suspended statement for the specified id has already been sent!");
                }
                else
                {
                    XasuTracker.Instance.LogWarning("The suspended statement for the specified id has already been sent!");
                    addSuspendedTime = false;
                }
            }
            if (!initializedTimes.ContainsKey(scoId))
            {
                if (XasuTracker.Instance.TrackerConfig.StrictMode)
                {
                    throw new XApiException("The Suspended statement for the specified id has not been initialized!");
                }
                else
                {
                    XasuTracker.Instance.LogWarning("The Suspended statement for the specified id has not been initialized!");
                    addSuspendedTime = false;
                }
            }


            if (addSuspendedTime)
                suspendedTimes.Add(scoId, DateTime.Now);

            return Enqueue(new Statement
            {
                verb = GetVerb(Verb.Suspended),
                target = GetTargetActivity(scoId, ScormType.SCO),
                context = XasuTracker.Instance.DefaultContext
            }).WithDuration(initializedTimes[scoId], suspendedTimes[scoId]);
        }
        #endregion

        #region Resumed
        /// <summary>
        /// Resumes a suspended SCORM lesson.
        /// </summary>
        /// <param name="scoId">Identifier of the resumed lesson.</param>
        public StatementPromise Resumed(string scoId)
        {
            bool addResumedTime = true;
            if (!suspendedTimes.ContainsKey(scoId))
            {
                if (XasuTracker.Instance.TrackerConfig.StrictMode)
                {
                    throw new XApiException("The resumed statement for the specified id cannot be sent before a suspend!");
                }
                else
                {
                    XasuTracker.Instance.LogWarning("The resumed statement for the specified id cannot be sent before a suspend!");
                    addResumedTime = false;
                }
            }

            if (addResumedTime)
                initializedTimes.Remove(scoId);
            initializedTimes.Add(scoId, DateTime.Now);
            suspendedTimes.Remove(scoId);
            return Enqueue(new Statement
            {
                verb = GetVerb(Verb.Resumed),
                target = GetTargetActivity(scoId, ScormType.SCO),
                context = XasuTracker.Instance.DefaultContext
            });
        }
        #endregion

        #region Progressed
        /// <summary>
        /// Tracks progress in a SCORM activity.
        /// </summary>
        /// <param name="id">The ID of the activity being progressed.</param>
        /// <param name="type">Type of the activity.</param>
        /// <param name="value">New value for the progress.</param>
        public StatementPromise Progressed(string id, ScormType type, float value)
        {
            return Enqueue(new Statement
            {
                verb = GetVerb(Verb.Progressed),
                target = GetTargetActivity(id, type),
                context = XasuTracker.Instance.DefaultContext
            }).WithScoreScaled(value);
        }
        #endregion


        #region Terminated
        /// <summary>
        /// Terminates a SCORM lesson.
        /// </summary>
        /// <param name="scoId">Identifier of the terminated lesson.</param>
        /// <param name="hasDuration">Whether the duration should be included in the statement.</param>
        /// <param name="durationInSeconds">The duration of the lesson in seconds (optional).</param>
        public StatementPromise Terminated(string scoId, bool hasDuration = false, long durationInSeconds = 0)
        {
            if (!initializedTimes.ContainsKey(scoId))
            {
                throw new XApiException("The Terminated statement for the specified id has not been initialized!");
            }

            // Get the initialized statement time to calculate the duration
            DateTime ticks = initializedTimes[scoId];
            initializedTimes.Remove(scoId);

            TimeSpan duration = hasDuration ? TimeSpan.FromSeconds(durationInSeconds) : DateTime.Now - ticks;
            return Enqueue(new Statement
            {
                verb = GetVerb(Verb.Terminated),
                target = GetTargetActivity(scoId, ScormType.SCO),
                context = XasuTracker.Instance.DefaultContext
            }).WithTimeSpanDuration(duration);
        }
        #endregion


        #region Passed

        /// <summary>
        /// The learner attempted and succeeded in a SCORM lesson.
        /// </summary>
        /// <param name="scoId">Identifier of the passed lesson.</param>
        /// <param name="score">The score scaled.</param>
        /// <param name="durationInSeconds">The duration of the lesson in seconds (optional).</param>
        public StatementPromise Passed(string scoId, float score, double durationInSeconds)
        {
            return Enqueue(new Statement
            {
                verb = GetVerb(Verb.Passed),
                target = GetTargetActivity(scoId, ScormType.SCO),
                context = XasuTracker.Instance.DefaultContext
            }).WithSuccess(true)
            .WithScoreScaled(score)
            .WithTimeSpanDuration(TimeSpan.FromSeconds(durationInSeconds));;
        }

        #endregion

        #region Failed
        /// <summary>
        /// The learner attempted and failed in a judged SCORM lesson.
        /// </summary>
        /// <param name="scoId">Identifier of the failed lesson.</param>
        /// <param name="score">The score scaled.</param>
        /// <param name="durationInSeconds">The duration of the lesson in seconds (optional).</param>
        public StatementPromise Failed(string scoId, float score, double durationInSeconds)
        {
            return Enqueue(new Statement
            {
                verb = GetVerb(Verb.Failed),
                target = GetTargetActivity(scoId, ScormType.SCO),
                context = XasuTracker.Instance.DefaultContext
            }).WithSuccess(false)
            .WithScoreScaled(score)
            .WithTimeSpanDuration(TimeSpan.FromSeconds(durationInSeconds));
        }

        #endregion

        #region Scored
        /// <summary>
        /// The player scored.
        /// </summary>
        /// <param name="id">Identifier of the activity being scored.</param>
        /// <param name="type">Type of the activity.</param>
        /// <param name="value">New value for the score.</param>
        public StatementPromise Scored(string id, ScormType type, float value)
        {
            return Enqueue(new Statement
            {
                verb = GetVerb(Verb.Scored),
                target = GetTargetActivity(id, type),
                context = XasuTracker.Instance.DefaultContext
            }).WithScoreScaled(value);
        }
        #endregion
        
        protected static StatementPromise Enqueue(Statement statement)
        {
            return AbstractHighLevelTracker<ScormTracker>.Enqueue(statement).CreateAndAddContextCategoryProfileActivity(AbstractHighLevelTracker<ScormTracker>.ContextActivityIds["Scorm"]);
        }
    }
}