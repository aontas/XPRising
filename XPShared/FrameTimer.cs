using BepInEx.Logging;
using Bloodstone.Hooks;

namespace XPShared;

public class FrameTimer
    {
        private bool _enabled;
        private bool _isRunning;
        private bool _runOnce;
        private DateTime _executeAfter = DateTime.MinValue;
        private DateTime _lastExecution = DateTime.MinValue;
        private TimeSpan _delay;
        private Action _action;
        private Func<TimeSpan> _delayGenerator;

        public TimeSpan TimeSinceLastRun => DateTime.Now - _lastExecution;

        public FrameTimer Initialise(Action action, TimeSpan delay, bool runOnce = true)
        {
            _delayGenerator = null;
            _delay = delay;
            _executeAfter = DateTime.Now + delay;
            _action = action;
            _runOnce = runOnce;

            return this;
        }
        
        public FrameTimer Initialise(Action action, Func<TimeSpan> delayGenerator, bool runOnce = true)
        {
            _delayGenerator = delayGenerator;
            _delay = _delayGenerator.Invoke();
            _executeAfter = DateTime.Now + _delay;
            _action = action;
            _runOnce = runOnce;

            return this;
        }

        public void Start()
        {
            Refresh();
            
            if (!_enabled)
            {
                _lastExecution = DateTime.MinValue;
                GameFrame.OnUpdate += GameFrame_OnUpdate;
                _enabled = true;
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
                Plugin.Log(LogLevel.Error, $"Timer failed {ex.Message}");
            }
            finally
            {
                if (_runOnce)
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

        public void Stop()
        {
            if (_enabled)
            {
                GameFrame.OnUpdate -= GameFrame_OnUpdate;
                _enabled = false;
            }
        }
    }