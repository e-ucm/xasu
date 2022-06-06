using System.Threading.Tasks;
using TinCan;

namespace Xasu.HighLevel
{
    public class StatementPromise
    {
        public Statement Statement { get; private set; }
        public Task<Statement> Promise { get; private set; }

        public StatementPromise(Statement statement, Task<Statement> task)
        {
            this.Statement = statement;
            this.Promise = task;
        }
    }
}
