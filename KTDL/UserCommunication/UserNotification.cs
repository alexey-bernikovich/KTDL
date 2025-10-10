using KTDL.Common;

namespace KTDL.UserCommunication
{
    internal class UserNotification
    {
        //TODO: Setup from the config file
        private readonly int[] _allowedSteps;
        private Dictionary<int, List<string>> _messages;
        private readonly object _stepLock = new object();
        private Dictionary<int, int> _lastStep;

        public UserNotification(int[] allowedSteps) 
        {
            _allowedSteps = allowedSteps;
            _messages = new Dictionary<int, List<string>>();
            _lastStep = new Dictionary<int, int>();
        }

        public void AddMessage(int id)
        {
            _messages[id] = new List<string>();
        }

        public void RemoveMessage(int id)
        {
            //TODO: double check
            _messages.Remove(id);
        }

        public string? GetUpdateMessage(ProgressInfo progressInfo, int messageId)
        {
            if (_messages.ContainsKey(messageId))
            {
                var messages = _messages[messageId];
                switch (progressInfo.Stage)
                {
                    case PipelineStepStage.Initialized:
                        messages.Add(progressInfo.Message);
                        _lastStep.Add(messageId, -1);
                        break;
                    case PipelineStepStage.Executing:
                        int percent = 0;
                        if (TryGetProgressStep(progressInfo.Processed, progressInfo.Total,
                            messageId, out percent))
                        {
                            messages.Remove(messages.ElementAt(messages.Count - 1));
                            messages.Add(string.Format(progressInfo.Message, percent));
                        }
                        else
                        {
                            return null;
                        }
                        break;
                    case PipelineStepStage.Completed:
                        messages.Remove(messages.ElementAt(messages.Count - 1));
                        messages.Add(progressInfo.Message);
                        _lastStep.Remove(messageId);
                        break;
                    default:
                        return null;
                }
                return string.Join("\n", messages);
            }
            return null;
        }

        public bool TryUpdateMessage(ProgressInfo progressInfo, int messageId, out string? updatedMessage)
        {
            updatedMessage = null;
            if((updatedMessage = GetUpdateMessage(progressInfo, messageId)) != null)
            {
                return true;
            }
            return false;
        }

        private bool TryGetProgressStep(int processed, int total, int messageId, out int percent)
        {
            percent = 0;
            if (total != 0)
            {
                var current = processed * 100 / total;
                var step = _allowedSteps.LastOrDefault(s => current >= s);

                lock (_stepLock)
                {
                    if (step == _lastStep[messageId])
                    {
                        return false;
                    }
                    _lastStep[messageId] = step;
                }

                percent = step;
                return true;
            }
            return false;            
        }
    }
}
