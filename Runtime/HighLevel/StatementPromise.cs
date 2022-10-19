using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TinCan;
using UnityEngine;

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

        public StatementPromise WithScore(double score)
        {
            Statement.result.score = new Score
            {
                scaled = score
            };
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
                var key = new System.Uri(stateExtension.Key).ToString();
                if (jObject.ContainsKey(key))
                {
                    var valueType = jObject.GetValue(key).Type;
                    // In case it stores the same string value
                    if (valueType == Newtonsoft.Json.Linq.JTokenType.String
                        && jObject.Value<string>(key).Equals(stateExtension.Value.ToString())) 
                    {
                        continue;
                    }

                    // In case it stores the same int value
                    if (valueType == Newtonsoft.Json.Linq.JTokenType.Integer
                        && jObject.Value<int>(key).ToString().Equals(stateExtension.Value.ToString()))
                    {
                        continue;
                    }
                }

                MoveToOldIfPresent(jObject, key);

                if (stateExtension.Value is int)
                {
                    jObject.Add(key, (int)stateExtension.Value);
                }
                else
                {
                    jObject.Add(key, stateExtension.Value.ToString());
                }
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
                    Debug.LogWarning("[STRICT=OFF] Error adding extensions to trace. Ignoring...");
                    Debug.LogException(ex);
                }
                return traceExtensions;
            }
        }

        private void MoveToOldIfPresent(Newtonsoft.Json.Linq.JObject jObject, string key)
        {
            if (jObject.ContainsKey(key))
            {
                var value = jObject.GetValue(key);
                var newKey = key + "_old/";
                jObject.Remove(key);
                MoveToOldIfPresent(jObject, newKey);
                jObject[newKey] = value;
            }
        }
    }
}
