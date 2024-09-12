using O2DESNet;
using O2DESNet.Standard;
using WSC_SimChallenge_2024_Net.PortSimulation;

namespace WSC_SimChallenge_2024_Net.Activity
{
    public class BaseActivity<T> : Sandbox
    {
        #region statics
        public TimeSpan BASE_ACTIVITY_DEFAULT_TIMESPAN = TimeSpan.FromSeconds(1);
        public int BASE_ACTIVITY_DEFAULT_CAPACITY = int.MaxValue;

        public TimeSpan TimeSpan { get; set; }
        public int Capacity { get; set; }
        protected bool _debugMode;
        public string ActivityName { get; set; }
        public bool NeedExtTryStart { get; set; } = false;
        public bool NeedExtTryFinish { get; set; } = false;
        #endregion

        #region Dynamics
        public int CapacityOccupied 
        {
            get 
            {
                return ProcessingList.Count + CompletedList.Count + ReadyToDepartList.Count;
            }
            private set { }
        }
        public List<T> PendingList { get; protected set; }
        public List<T> ReadyToStartList { get; set; }  // Used when Start rely on external condition
        public List<T> ProcessingList { get; private set; }
        public List<T> CompletedList { get; set; }
        public List<T> ReadyToFinishList { get; set; }  // Used when Finish rely on external condition
        public List<T> ReadyToDepartList { get; private set; }
        public HourCounter HourCounter { get; private set; }    //public Dictionary<T, QCLineList> MapCondition;
        #endregion

        public BaseActivity(int seed = 0) : base(seed)
        {
            ActivityName = this.GetType().Name;
            TimeSpan = BASE_ACTIVITY_DEFAULT_TIMESPAN;
            Capacity = BASE_ACTIVITY_DEFAULT_CAPACITY;
            PendingList = new List<T>();
            ReadyToStartList = new List<T>();
            ProcessingList = new List<T>();
            CompletedList = new List<T>();
            ReadyToFinishList = new List<T>();
            ReadyToDepartList = new List<T>();
            HourCounter = AddHourCounter();
        }
        public BaseActivity(string name, bool debugMode = false, int seed = 0) : this(seed)
        {
            ActivityName = name;
            _debugMode = debugMode;
        }

        #region Events
        virtual public void RequestToStart(T load)
        {
            if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.RequestToStart({load})");
            PendingList.Add(load);
            Schedule(() => AttemptToStart(), TimeSpan.FromMicroseconds(1)); // Caution: New added load and AttemptToStart load may not be the same
            //AttemptToStart(); // Caution: New added load and AttemptToStart load may not be the same
        }

        /// <summary>
        ///  * Must override this function if NeedExtTryStart is true
        /// </summary>
        /// <param name="obj"></param>
        virtual public void TryStart(Object obj)
        {
            if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryStart({obj})");
            bool condition = obj is T;  // Caution: Need to specify the condition according to certain logic about obj
            T? load = PendingList.Count > 0 ? PendingList[0] : default; // Caution: Need to sepcify a load according to certain logic about obj
            if (condition && load != null)
            {
                ReadyToStartList.Add(load);
            }
            AttemptToStart();
        }
        virtual public void AttemptToStart() // function without parameters: Get the 1st eligible one in PendingList
        {
            if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.AttemptToStart()");
            if (PendingList.Count > 0 && CapacityOccupied < Capacity)
            {
                if (!NeedExtTryStart) // if no need to check external condition, go straight to start
                {
                    Start(PendingList[0]);
                }
                else // if need to check external condition, find out the load that met the external condition
                {
                    for (int i = 0; i < PendingList.Count; i++)
                    {
                        if (ReadyToStartList.Contains(PendingList[i]))
                        {
                            Start(PendingList[i]);
                            break;
                        }
                    }
                }
            }
        }
        virtual public void Start(T load)
        {
            if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.Start({load})");
            HourCounter.ObserveChange(1);
            ProcessingList.Add(load);
            PendingList.Remove(load);
            ReadyToStartList.Remove(load);
            Schedule(() => Complete(load), TimeSpan);
            EmitOnStart(load);
        }
        virtual protected void Complete(T load)
        {
            if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.Complete({load})");
            CompletedList.Add(load);
            ProcessingList.Remove(load);
            AttemptToFinish(load);
        }

        /// <summary>
        ///  * Must override this function if NeedExtTryFinish is true
        /// </summary>
        virtual public void TryFinish(Object obj)
        {
            if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.TryFinish({obj})");
            bool condition = obj is T;  // Caution: Need to specify alternative condition according to certain logic about obj
            T? load = CompletedList.Count > 0 ? CompletedList[0] : default; // Caution: Need to sepcify a load according to certain logic about obj
            if (condition && load != null)
            {
                ReadyToFinishList.Add(load);
            }
            AttemptToFinish(load);
        }
        virtual public void AttemptToFinish(T load)
        {
            if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.AttemptToFinish({load})");
            if (CompletedList.Contains(load) && (!NeedExtTryFinish || ReadyToFinishList.Contains(load)))
            {
                Finish(load);
            }
        }
        virtual public void Finish(T load)
        {
            if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.Finish({load})");
            ReadyToDepartList.Add(load);
            CompletedList.Remove(load);
            ReadyToFinishList.Remove(load);
            EmitOnReadyToDepart(load);
        }
        virtual public void Depart(T load)
        {
            if (ReadyToDepartList.Contains(load) )
            {
                if (_debugMode) Console.WriteLine($"{ClockTime.ToString("yyyy-MM-dd HH:mm:ss")}  {ActivityName}.Depart({load})");
                HourCounter.ObserveChange(-1);
                ReadyToDepartList.Remove(load);
                //Schedule(AttemptToStart, TimeSpan);
                AttemptToStart();
            }
        }
        #endregion

        // Emit Signals
        protected void EmitOnStart(T load)
        {
            OnStart.Invoke(load);
        }
        protected void EmitOnReadyToDepart(T load)
        {
            OnReadyToDepart.Invoke(load);
        }

        #region Output Events
        // Inform the request to latter activity (may have other usage besides this)
        public event Action<T> OnReadyToDepart = load => { };
        // Confirm the arrival to former activity (may have other usage besides this)
        public event Action<T> OnStart = load => { };
        #endregion

        public BaseActivity<T> FlowTo(BaseActivity<T> nextActivity)
        {
            this.OnReadyToDepart += nextActivity.RequestToStart;
            nextActivity.OnStart += Depart;
            return nextActivity;
        }

        public BaseActivity<T> FlowToBranch(BaseActivity<T> nextActivity, Func<T, bool> condition)
        {           
            this.OnReadyToDepart += (T) =>
            {
                if (condition(T))
                {
                    //Console.WriteLine($"Check point 1: {ActivityName},{nextActivity.ActivityName}");
                    nextActivity.RequestToStart(T);
                }
                    
            };
            nextActivity.OnStart += (T) =>
            {
                if (condition(T))
                {
                    //Console.WriteLine($"Check Point 2: {ClockTime}, {ActivityName},{nextActivity.ActivityName}");
                    this.Depart(T);
                }
                    
            };
            return nextActivity;
        }

        // No need to define FlowToMerge since no difference in functionality with common FlowTo
        public BaseActivity<T> FlowToMerge(BaseActivity<T> nextActivity, Func<T, bool> condition)
        {
            this.OnReadyToDepart += nextActivity.RequestToStart;
            nextActivity.OnStart += (T) =>
            {
                if (condition(T))
                    this.Depart(T);
            };
            return nextActivity;
        }

        /// Use in the last activity before Ternimator
        public void Terminate()
        {
            OnReadyToDepart += Depart;
        }
    }
}
