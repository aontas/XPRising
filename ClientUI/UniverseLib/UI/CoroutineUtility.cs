using System.Collections;
using System.Collections.Concurrent;

namespace ClientUI.UniverseLib.UI;

public static class CoroutineUtility
{
    private static ConcurrentBag<IEnumerator> _nextFrameRoutines = new();
    private static ConcurrentBag<IEnumerator> _thisFrameRoutines = new();
    public static void StartCoroutine(IEnumerator coroutine)
    {
        _nextFrameRoutines.Add(coroutine);
    }

    public static void TickRoutines()
    {
        // Next frame is now!
        (_thisFrameRoutines, _nextFrameRoutines) = (_nextFrameRoutines, _thisFrameRoutines);
        while (!_thisFrameRoutines.IsEmpty)
        {
            // Take out the next item
            if (!_thisFrameRoutines.TryTake(out var routine)) continue;
            if (routine.MoveNext())
            {
                // If the routine has not reached the end, pass it back to be run next frame.
                _nextFrameRoutines.Add(routine);
            }
        }
    }
}