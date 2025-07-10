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

        public StatementPromise WithSuccess(bool success)
        {
            Statement.result.success = success;
            return this;
        }

        public StatementPromise WithScore(Dictionary<string, double> scores)
        {
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
            if (Statement.result.score == null)
            {
                Statement.result.score = new Score();
            }
            Statement.result.score.raw = score;
            return this;
        }

        public StatementPromise WithScoreMin(double score)
        {
            if (Statement.result.score == null)
            {
                Statement.result.score = new Score();
            }
            Statement.result.score.min = score;
            return this;
        }

        public StatementPromise WithScoreMax(double score)
        {
            if (Statement.result.score == null)
            {
                Statement.result.score = new Score();
            }
            Statement.result.score.max = score;
            return this;
        }

        public StatementPromise WithScoreScaled(double score)
        {
            if (Statement.result.score == null)
            {
                Statement.result.score = new Score();
            }
            Statement.result.score.scaled = score;
            return this;
        }

        public StatementPromise WithCompletion(bool completion)
        {
            Statement.result.completion = completion;
            return this;
        }

        public StatementPromise WithDuration(DateTime init, DateTime end)
        {
            TimeSpan duration = end - init;
            Statement.result.duration = duration;
            return this;
        }

        public StatementPromise WithResponse(string response)
        {
            Statement.result.response = response;
            return this;
        }

        public StatementPromise WithResultExtension(string key, object value)
        {
            Dictionary<string, object> extensions = new Dictionary<string, object>();
            extensions.Add(key, value);
            Statement.result.extensions = AddExtensions(Statement.result.extensions, extensions);
            return this;
        }

        public StatementPromise WithResultExtensions(Dictionary<string, object> extensions)
        {
            Statement.result.extensions = AddExtensions(Statement.result.extensions, extensions);
            return this;
        }

        
        public StatementPromise WithContextExtensions(Dictionary<string, object> extensions)
        {
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
