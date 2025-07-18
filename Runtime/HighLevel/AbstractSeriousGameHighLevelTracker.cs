using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using TinCan;

namespace Xasu.HighLevel
{
    public abstract class AbstractSeriousGameHighLevelTracker<T> : AbstractHighLevelTracker<T>
        where T : class, new()
    {
        protected static StatementPromise Enqueue(Statement statement)
        {
            return AbstractHighLevelTracker<T>.Enqueue(statement).CreateAndAddContextCategoryProfileActivity(AbstractHighLevelTracker<T>.ContextActivityIds["SeriousGames"]);
        }
    }
}
