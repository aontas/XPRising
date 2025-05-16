using BepInEx.Logging;

namespace XPShared;

public class FrameTimer
    {
        private bool _enabled;
        private bool _isRunning;
        private int _runCount;
        private int _maxRunCount;
        private DateTime _executeAfter = DateTime.MinValue;
        private DateTime _lastExecution = DateTime.MinValue;
        private DateTime _startTime = DateTime.MinValue;
        private TimeSpan _delay;
        private Action _action;
        private Func<TimeSpan> _delayGenerator;

        public TimeSpan TimeSinceLastRun => DateTime.Now - _lastExecution;
        public int RunCount => _runCount;
        public TimeSpan TimeSinceStart => _enabled ? TimeSpan.Zero : DateTime.Now - _startTime;
        public bool Enabled => _enabled;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action">the action that is performed</param>
        /// <param name="delay">the delay between calls. This will not be called more frequently than this delay, but it might be called longer if the delay is greater than the time between frames</param>
        /// <param name="runCount">the amount of times to perform the action. A count less than 1 will cause it to run indefinitely</param>
        /// <returns></returns>
        public FrameTimer Initialise(Action action, TimeSpan delay, int runCount = 1)
        {
            _delayGenerator = null;
            _delay = delay;
            _executeAfter = DateTime.Now + delay;
            _action = action;
            _maxRunCount = runCount;

            return this;
        }
        
        public FrameTimer Initialise(Action action, Func<TimeSpan> delayGenerator, int runCount = 1)
        {
            _delayGenerator = delayGenerator;
            _delay = _delayGenerator.Invoke();
            _executeAfter = DateTime.Now + _delay;
            _action = action;
            _maxRunCount = runCount;

            return this;
        }

        public void Start()
        {
            Refresh();
            
            if (!_enabled)
            {
                _startTime = DateTime.Now;
                _runCount = 0;
                _lastExecution = DateTime.MinValue;
                GameFrame.OnUpdate += GameFrame_OnUpdate;
                _enabled = true;
            }
        }

        public void Stop()
        {
            if (_enabled)
            {
                GameFrame.OnUpdate -= GameFrame_OnUpdate;
                _enabled = false;
            }
        }

        private void Refresh()
        {
            if (_delayGenerator != null) _delay = _delayGenerator.Invoke();
            _executeAfter = DateTime.Now + _delay;
        }

        private void GameFrame_OnUpdate()
        {
            Update();
        }
        
        private void Update()
        {
            if (!_enabled || _isRunning)
            {
                return;
            }

            if (_executeAfter >= DateTime.Now)
            {
                return;
            }

            _isRunning = true;
            try
            {
                _action.Invoke();
                _lastExecution = DateTime.Now;
            }
            catch (Exception ex)
            {
                Plugin.Log(LogLevel.Error, $"Timer failed {ex.Message}\n{ex.StackTrace}");
                // Stop running the timer as it will likely continue to fail.
                Stop();
            }
            finally
            {
                _runCount++;
                if (_maxRunCount > 0 && _runCount >= _maxRunCount)
                {
                    Stop();
                }
                else
                {
                    Refresh();
                }
                
                _isRunning = false;
            }
        }
    }