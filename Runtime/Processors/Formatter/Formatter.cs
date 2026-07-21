using Newtonsoft.Json.Linq;
using System;
using TinCan;
using Xasu.Config;

namespace Xasu.Processors.Formatter
{
    public static class TraceFormatter
    {
        public static string Format(Statement statement, TraceFormats format, TCAPIVersion version)
        {
            switch (format)
            {
                case TraceFormats.XAPI:
                    var jObject = statement.ToJObject(version);
                    StripEmptyResult(jObject);
                    return jObject.ToString();
                default:
                case TraceFormats.CSV:
                    throw new NotSupportedException("CSV is not available in this version!");
            }
        }

        private static void StripEmptyResult(JObject jObject)
        {
            if (jObject["result"] is JObject resultObj
                && resultObj.Count == 1
                && resultObj["extensions"] is JObject extObj
                && !extObj.HasValues)
            {
                jObject.Remove("result");
            }
        }
    }
}
