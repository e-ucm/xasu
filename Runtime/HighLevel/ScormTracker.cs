using System;
using System.Collections.Generic;
using TinCan;
using Xasu.CMI5;
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
            Initialized,
            Suspended,
            Resumed,
            Terminated,
            Progressed,
            Passed,
            Failed,
            Scored
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
            SCO,
            Course,
            Module,
            Assessment,
            Interaction,
            Objective,
            Attempt
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


        protected enum ContextActivity
        {
            Scorm
        }

        protected Dictionary<Enum, string> contextIds = new Dictionary<Enum, string>()
        {
            { ContextActivity.Scorm, "https://w3id.org/xapi/scorm/v/2" },
        };

        protected override Dictionary<Enum, string> ContextActivityIds => contextIds;

        /**********************
        * Static attributes
        * *******************/

        private static Dictionary<string, DateTime> initializedTimes = new Dictionary<string, DateTime>();

        private static Dictionary<string, DateTime> suspendedTimes = new Dictionary<string, DateTime>();

        #region Initialized
        /// <summary>
        /// Player initialized a lesson.
        /// Type : SCO
        /// </summary>
        /// <param name="scoId">Identifier.</param>
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

            if(addInitializedTime)
                initializedTimes.Add(scoId, DateTime.Now);
            return Enqueue(new Statement
            {
                verb = GetVerb(Verb.Initialized),
                target = GetTargetActivity(scoId, ScormType.SCO),
                context= GetContext(ContextActivity.Scorm)
            });
        }
        #endregion

        #region Suspended
        /// <summary>
        /// Player Suspended a lesson.
        /// Type : SCO
        /// </summary>
        /// <param name="scoId">Identifier.</param>
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


            if(addSuspendedTime)
                suspendedTimes.Add(scoId, DateTime.Now);
            
            TimeSpan duration = suspendedTimes[scoId] - initializedTimes[scoId];
            return Enqueue(new Statement
            {
                verb = GetVerb(Verb.Suspended),
                target = GetTargetActivity(scoId, ScormType.SCO),
                context= GetContext(ContextActivity.Scorm),
                result = new Result { duration = duration }
            });
        }
        #endregion

         #region Resumed
        /// <summary>
        /// Player Resumed a lesson.
        /// Type : SCO
        /// </summary>
        /// <param name="scoId">Identifier.</param>
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

            if(addResumedTime)
                initializedTimes.Remove(scoId);
                initializedTimes.Add(scoId, DateTime.Now);
                suspendedTimes.Remove(scoId);
            return Enqueue(new Statement
            {
                verb = GetVerb(Verb.Resumed),
                target = GetTargetActivity(scoId, ScormType.SCO),
                context= GetContext(ContextActivity.Scorm)
            });
        }
        #endregion

        #region Progressed
        /// <summary>
        /// Player progressed.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="value">New value for the progress.</param>
        /// <param name="type">Type.</param>
        public StatementPromise Progressed(string id, ScormType type, float value)
        {
            return Enqueue(new Statement
            {
                verb = GetVerb(Verb.Progressed),
                target = GetTargetActivity(id, type),
                result = new Result {
                    score = new Score { scaled = value,}
                },
                context= GetContext(ContextActivity.Scorm)
            });
        }
        #endregion

        
        #region Terminated
        /// <summary>
        /// Player terminated a lesson.
        /// Type = SCO 
        /// </summary>
        /// <param name="id">AU id.</param>
        /// <param name="type">AU type.</param>
        public StatementPromise Terminated(string id,bool hasDuration=false, long durationInSeconds=0)
        {
            if (!initializedTimes.ContainsKey(id))
            {
                throw new XApiException("The Terminated statement for the specified id has not been initialized!");
            }

            // Get the initialized statement time to calculate the duration
            DateTime ticks = initializedTimes[id];
            initializedTimes.Remove(id);

            TimeSpan duration = hasDuration ? TimeSpan.FromSeconds(durationInSeconds) : DateTime.Now - ticks;
            return Enqueue(new Statement
            {
                verb = GetVerb(Verb.Terminated),
                target = GetTargetActivity(id, ScormType.SCO),
                context = GetContext(ContextActivity.Scorm),
                result= new Result {
                    duration = duration,
                }
            });
        }
        #endregion


        #region Passed

        /// <summary>
        /// The learner attempted and succeeded in a judged activity in the AU.
        /// </summary>
        /// <param name="id">AU id.</param>
        /// <param name="type">AU type.</param>
        /// <param name="score">The score scaled.</param>
        public StatementPromise Passed(string id, float score, double durationInSeconds)
        {
            return Enqueue(new Statement
            {
                verb = GetVerb(Verb.Passed),
                target = GetTargetActivity(id, ScormType.SCO),
                context =  GetContext(ContextActivity.Scorm),
                result = new Result
                {
                    success = true,
                    score = float.IsNaN(score) ? null : new Score { scaled = score },
                    duration = TimeSpan.FromSeconds(durationInSeconds)
                }
            });
        }

        #endregion

        #region Failed
        /// <summary>
        /// The learner attempted and failed in a judged activity in the AU.
        /// The (scaled) score MUST be lower than the "masteryScore"
        /// indicated in the LMS Launch Data.
        /// </summary>
        /// <param name="id">AU id.</param>
        /// <param name="type">AU type.</param>
        /// <param name="score">The score scaled.</param>
        public StatementPromise Failed(string id, float score, double durationInSeconds)
        {
            return Enqueue(new Statement
            {
                verb = GetVerb(Verb.Failed),
                target = GetTargetActivity(id, ScormType.SCO),
                context = GetContext(ContextActivity.Scorm),
                result = new Result
                {
                    success = false,
                    score = score == float.NaN ? null : new Score { scaled = score },
                    duration = TimeSpan.FromSeconds(durationInSeconds)
                }
            });
        }

        #endregion

        #region Scored
        /// <summary>
        /// Player scored.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="value">New value for the score.</param>
        /// <param name="type">Type.</param>
        public StatementPromise Scored(string id, ScormType type, float value)
        {
            return Enqueue(new Statement
            {
                verb = GetVerb(Verb.Scored),
                target = GetTargetActivity(id, type),
                result = new Result {
                    score = new Score { scaled = value, }
                },
                context= GetContext(ContextActivity.Scorm)
            });
        }
        #endregion


    }
}