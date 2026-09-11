using System;
using System.Collections;
using System.Collections.Generic;

namespace CardBattle.Core
{
    public enum BattleActionResult { Pending, Successful, Failed, Cancelled }

    /// <summary>One execution's commit/result boundary. Does not own damage, statuses or turn rules.</summary>
    public sealed class BattleActionExecution
    {
        public bool IsCommitted { get; private set; }
        public BattleActionResult Result { get; private set; } = BattleActionResult.Pending;
        public string Reason { get; private set; } = string.Empty;
        public bool IsComplete => Result != BattleActionResult.Pending;
        private bool cancellationRequested;

        public void Cancel(string reason)
        {
            cancellationRequested = true;
            Complete(BattleActionResult.Cancelled, reason);
        }

        public void Commit()
        {
            if (!IsComplete)
                IsCommitted = true;
        }

        public bool Complete(BattleActionResult result, string reason = "")
        {
            if (IsComplete || result == BattleActionResult.Pending)
                return false;
            if (result == BattleActionResult.Successful && !IsCommitted)
                return false;
            Result = result;
            Reason = reason;
            return true;
        }

        /// <summary>
        /// Unity otherwise runs yielded enumerators separately, leaving parent waits stranded on exceptions.
        /// Walk this action's nested enumerators so errors have a result and finally blocks are disposed.
        /// This does not time out player choices or change gameplay effects.
        /// </summary>
        public IEnumerator Run(IEnumerator routine, Action<Exception> reportError)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(routine);
            try
            {
                while (stack.Count > 0 && !cancellationRequested && Result != BattleActionResult.Cancelled && Result != BattleActionResult.Failed)
                {
                    var current = stack.Peek();
                    bool moved = false;
                    object yielded = null;
                    Exception error = null;
                    try
                    {
                        moved = current.MoveNext();
                        if (moved) yielded = current.Current;
                    }
                    catch (Exception exception) { error = exception; }

                    if (error != null)
                    {
                        Complete(BattleActionResult.Failed, error.Message);
                        reportError?.Invoke(error);
                        yield break;
                    }
                    if (!moved)
                    {
                        stack.Pop();
                        try { (current as IDisposable)?.Dispose(); }
                        catch (Exception exception)
                        {
                            Complete(BattleActionResult.Failed, exception.Message);
                            reportError?.Invoke(exception);
                            yield break;
                        }
                    }
                    else if (yielded is IEnumerator nested)
                        stack.Push(nested);
                    else
                        yield return yielded;
                }
            }
            finally
            {
                while (stack.Count > 0)
                {
                    try { (stack.Pop() as IDisposable)?.Dispose(); }
                    catch (Exception exception)
                    {
                        Complete(BattleActionResult.Failed, exception.Message);
                        reportError?.Invoke(exception);
                    }
                }
            }
        }
    }
}
