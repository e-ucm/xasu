using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TinCan;
using UnityEngine;
using Xasu.Util;

namespace Xasu.HighLevel
{
    public class StatementPromise
    {
        public Statement Statement { get; private set; }
        public Task<Statement> Promise { get; private set; }

        public TaskAwaiter<Statement> GetAwaiter() { return Promise.GetAwaiter(); }

        public StatementPromise(Statement statement, Task<Statement> task)
        {
            this.Statement = statement;
            this.Promise = task;
        }

        public StatementPromise CreateAndAddContextGroupingActivity(string id, string name, string description, Uri type)
        {
            Activity act = new Activity
            {
                id = id,
                definition = new ActivityDefinition
                {
                    //name = new LanguageMap {
                    //    "en-US"= name,
                    //},
                    //description= new LanguageMap  {
                    //	"en-US"=description,
                    //},
                    type = type,
                }
            };
            return this.AddContextGroupingActivity(act);
        }

        StatementPromise AddContextGroupingActivity(Activity contextActivity) {
            if (Statement.context== null)
            {
                Statement.context = new Context();
            }
            if (Statement.context.contextActivities == null)
            {
                Statement.context.contextActivities = new ContextActivities();
            }
            if(Statement.context.contextActivities.grouping == null) {
                Statement.context.contextActivities.grouping = new List<Activity>();
            }
            Statement.context.contextActivities.grouping.Add(contextActivity);
            return this;
        }
        
        StatementPromise AddContextRegistration(Guid registrationId) {
            if (Statement.context== null)
            {
                Statement.context = new Context();
            }
            if (Statement.context.registration == null)
            {
                Statement.context.registration = registrationId;
            }
            return this;
        }
        
         public StatementPromise CreateAndAddContextParentActivity(string id, string name, string description, Uri type)
        {
            Activity act = new Activity
            {
                id = id,
                definition = new ActivityDefinition
                {
                    //name = new LanguageMap {
                    //    "en-US"= name,
                    //},
                    //description= new LanguageMap  {
                    //	"en-US"=description,
                    //},
                    type = type,
                }
            };
            return this.AddContextParentActivity(act);
        }

        StatementPromise AddContextParentActivity(Activity contextActivity) {
            if (Statement.context== null)
            {
                Statement.context = new Context();
            }
            if (Statement.context.contextActivities == null)
            {
                Statement.context.contextActivities = new ContextActivities();
            }
            if(Statement.context.contextActivities.parent == null) {
                Statement.context.contextActivities.parent = new List<Activity>();
            }
            Statement.context.contextActivities.parent.Add(contextActivity);
            return this;
        }

        public StatementPromise CreateAndAddContextCategoryProfileActivity(string id)
        {
            return this.CreateAndAddContextCategoryActivity(id, new Uri("http://adlnet.gov/expapi/activities/profile"));
        }

        public StatementPromise CreateAndAddContextCategoryActivity(string id, Uri typeUri = null)
        {
            Activity activity = new Activity { id = id };
            if (typeUri != null)
            {
                activity.definition = new ActivityDefinition
                {
                    type = typeUri,
                };
            }
            return this.AddContextCategoryActivity(activity);
        }

        public StatementPromise AddContextCategoryActivity(Activity activity)
        {
            if (Statement.context== null)
            {
                Statement.context = new Context();
            }
            if (Statement.context.contextActivities == null)
            {
                Statement.context.contextActivities = new ContextActivities();
            }
            if (Statement.context.contextActivities.category == null)
            {
                Statement.context.contextActivities.category = new List<Activity>();
            }
            Statement.context.contextActivities.category.Add(activity);
            return this;
        }

        public StatementPromise WithSuccess(bool success)
        {
            if (Statement.result== null)
            {
                Statement.result = new Result();
            }
            Statement.result.success = success;
            return this;
        }

        public StatementPromise WithScore(Dictionary<string, double> scores)
        {
            if (Statement.result== null)
            {
                Statement.result = new Result();
            }
            if (Statement.result.score == null)
            {
                Statement.result.score = new Score();
            }
            if (scores.ContainsKey("raw")) {
                Statement.result.score.raw = scores["raw"];
            }
            if (scores.ContainsKey("min")) {
                Statement.result.score.min = scores["min"];
            }
            if (scores.ContainsKey("max")) {
                Statement.result.score.max = scores["max"];
            }
            if (scores.ContainsKey("scaled")) {
                Statement.result.score.scaled = scores["scaled"];
            }
            return this;
        }

        public StatementPromise WithScoreRaw(double score)
        {
            if (Statement.result== null)
            {
                Statement.result = new Result();
            }
            if (Statement.result.score == null)
            {
                Statement.result.score = new Score();
            }
            Statement.result.score.raw = score;
            return this;
        }

        public StatementPromise WithScoreMin(double score)
        {
            if (Statement.result== null)
            {
                Statement.result = new Result();
            }
            if (Statement.result.score == null)
            {
                Statement.result.score = new Score();
            }
            Statement.result.score.min = score;
            return this;
        }

        public StatementPromise WithScoreMax(double score)
        {
            if (Statement.result== null)
            {
                Statement.result = new Result();
            }
            if (Statement.result.score == null)
            {
                Statement.result.score = new Score();
            }
            Statement.result.score.max = score;
            return this;
        }

        public StatementPromise WithScoreScaled(double score)
        {
            if (Statement.result== null)
            {
                Statement.result = new Result();
            }
            if (Statement.result.score == null)
            {
                Statement.result.score = new Score();
            }
            Statement.result.score.scaled = score;
            return this;
        }

        public StatementPromise WithCompletion(bool completion)
        {
            if (Statement.result== null)
            {
                Statement.result = new Result();
            }
            Statement.result.completion = completion;
            return this;
        }

        public StatementPromise WithDuration(DateTime init, DateTime end)
        {
            if (Statement.result== null)
            {
                Statement.result = new Result();
            }
            TimeSpan duration = end - init;
            Statement.result.duration = duration;
            return this;
        }

        public StatementPromise WithTimeSpanDuration(TimeSpan duration)
        {
            if (Statement.result== null)
            {
                Statement.result = new Result();
            }
            Statement.result.duration = duration;
            return this;
        }

        public StatementPromise WithResponse(string response)
        {
            if (Statement.result== null)
            {
                Statement.result = new Result();
            }
            Statement.result.response = response;
            return this;
        }

        public StatementPromise WithResultExtension(string key, object value)
        {
            if (Statement.result== null)
            {
                Statement.result = new Result();
            }
            Dictionary<string, object> extensions = new Dictionary<string, object>();
            extensions.Add(key, value);
            Statement.result.extensions = AddExtensions(Statement.result.extensions, extensions);
            return this;
        }

        public StatementPromise WithResultExtensions(Dictionary<string, object> extensions)
        {
            if (Statement.result== null)
            {
                Statement.result = new Result();
            }
            Statement.result.extensions = AddExtensions(Statement.result.extensions, extensions);
            return this;
        }

        
        public StatementPromise WithContextExtensions(Dictionary<string, object> extensions)
        {
            if (Statement.context== null)
            {
                Statement.context = new Context();
            }
            Statement.context.extensions = AddExtensions(Statement.result.extensions, extensions);
            return this;
        }

        private Extensions AddExtensions(Extensions traceExtensions, Dictionary<string, object> extensions)
        {
            var jObject = traceExtensions.ToJObject(TinCan.TCAPIVersion.V103);
            foreach (var stateExtension in extensions)
            {
                ExtensionUtil.AddExtensionToJObject(stateExtension, jObject);
            }

            try
            {
                return new TinCan.Extensions(jObject);
            }
            catch(System.Exception ex)
            {
                if (XasuTracker.Instance.TrackerConfig.StrictMode)
                {
                    throw;
                }
                else
                {
                    XasuTracker.Instance.LogWarning("[STRICT=OFF] Error adding extensions to trace. Ignoring...");
                    Debug.LogException(ex);
                }
                return traceExtensions;
            }
        }
    }
}
