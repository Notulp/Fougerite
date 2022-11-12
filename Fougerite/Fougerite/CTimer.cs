using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Fougerite
{
    internal class CTimerHandler : MonoBehaviour
    {
        private void FixedUpdate()
        {
            CTimer.OnUpdate();
        }
    }
    
    /* Examples: 

	private void TestTimerCall(Player player) 
	{
		// Do something
	}
	
	private void TestTimerCall2() 
	{
		// Do something
	}

	public void OnPlayerConnected(Player player) 
	{
		CTimer.SetTimer(() => TestTimerCall(player), 1000, 1);
		
		// Normal without parameters //
		CTimer.SetTimer(TestTimerCall2, 1000, 1);
		
		// Without existing method //
		CTimer.SetTimer(() =>
		{
		    
		}, 1000, 0);
	}
    */

    
    /// <summary>
    /// Sorted list of timers.
    /// </summary>
    public class CTimer
    {
        private static readonly List<CTimer> timer = new List<CTimer>();
        private static readonly List<CTimer> InsertAfterList = new List<CTimer>();
        private static Stopwatch Stopwatch = new Stopwatch();
        private ulong _executeAtMs;
        private bool _willberemoved = false;
        
        /// <summary>
        /// Returns how many executes are left for the timer. Use 0 for infinite.
        /// </summary>
        public uint ExecutesLeft;
        /// <summary>
        /// If the Timer should handle exceptions with a try-catch-finally. Can be changed dynamically.
        /// </summary>
        public bool HandleException;
        /// <summary>
        /// The Action getting called by the Timer. Can be changed dynamically.
        /// </summary>
        public Action Func;
        /// <summary>
        /// After how many milliseconds (after the last execution) the timer should get called. Can be changed dynamically.
        /// </summary>
        public uint ExecuteAfterMs;
 
        public bool IsRunning
        {
            get { return !_willberemoved; }
        }

        internal static void StartWatching()
        {
            Stopwatch.Start();
        }
 
        /// <summary>
        /// Constructor used to create the Timer.
        /// </summary>
        /// <param name="thefunc">The Action which you want to get called.</param>
        /// <param name="executeafterms">Execute the Action after milliseconds. If executes is more than one, this gets added to executeatms.</param>
        /// <param name="executeatms">Execute at milliseconds.</param>
        /// <param name="executes">How many times to execute. 0 for infinitely.</param>
        /// <param name="handleexception">If try-catch-finally should be used when calling the Action</param>
        private CTimer(Action thefunc, uint executeafterms, ulong executeatms, uint executes, bool handleexception)
        {
            Func = thefunc;
            ExecuteAfterMs = executeafterms;
            _executeAtMs = executeatms;
            ExecutesLeft = executes;
            HandleException = handleexception;
        }
 
        /// <summary>
        /// Use this method to create the Timer.
        /// </summary>
        /// <param name="thefunc">The Action which you want to get called.</param>
        /// <param name="executeafterms">Execute after milliseconds.</param>
        /// <param name="executes">Amount of executes. Use 0 for infinitely.</param>
        /// <param name="handleexception">If try-catch-finally should be used when calling the Action</param>
        /// <returns></returns>
        public static CTimer SetTimer(Action thefunc, uint executeafterms, uint executes = 1,
            bool handleexception = true)
        {
            ulong executeatms = executeafterms + GetTick();
            CTimer thetimer = new CTimer(thefunc, executeafterms, executeatms, executes, handleexception);
            InsertAfterList.Add(thetimer);
            return thetimer;
        }
 
        public void Kill()
        {
            _willberemoved = true;
        }
 
        private void ExecuteMe()
        {
            Func();
            if (ExecutesLeft == 1)
            {
                ExecutesLeft = 0;
                _willberemoved = true;
            }
            else
            {
                if (ExecutesLeft != 0)
                {
                    ExecutesLeft--;
                }

                _executeAtMs += ExecuteAfterMs;
                InsertAfterList.Add(this);
            }
        }
 
        private void ExecuteMeSafe()
        {
            try
            {
                Func();
            }
            catch (Exception ex)
            {
                Logger.LogError($"[CTimer] Failed at calling {Func.Method.Name} Error: {ex}");
            }
            finally
            {
                if (ExecutesLeft == 1)
                {
                    ExecutesLeft = 0;
                    _willberemoved = true;
                }
                else
                {
                    if (ExecutesLeft != 0)
                    {
                        ExecutesLeft--;
                    }

                    _executeAtMs += ExecuteAfterMs;
                    InsertAfterList.Add(this);
                }
            }
        }
 
        public void Execute(bool changeexecutems = true)
        {
            if (changeexecutems)
            {
                _executeAtMs = GetTick();
            }
 
            if (HandleException)
            {
                ExecuteMeSafe();
            }
            else
            {
                ExecuteMe();
            }
        }
 
        private void InsertSorted()
        {
            bool putin = false;
            for (int i = timer.Count - 1; i >= 0 && !putin; i--)
            {
                if (_executeAtMs <= timer[i]._executeAtMs)
                {
                    timer.Insert(i + 1, this);
                    putin = true;
                }
            }

            if (!putin)
            {
                timer.Insert(0, this);
            }
        }
        
        private static ulong GetTick()
        {
            return (ulong) Stopwatch.ElapsedMilliseconds;
        }
 
        public static void OnUpdate()
        {
            ulong tick = GetTick();
            for (int i = timer.Count - 1; i >= 0; i--)
            {
                if (!timer[i]._willberemoved)
                {
                    if (timer[i]._executeAtMs <= tick)
                    {
                        CTimer thetimer = timer[i];
                        timer.RemoveAt(i);
                        if (thetimer.HandleException)
                        {
                            thetimer.ExecuteMeSafe();
                        }
                        else
                        {
                            thetimer.ExecuteMe();
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    timer.RemoveAt(i);
                }
            }
 
            if (InsertAfterList.Count > 0)
            {
                bool newv = false;
                // It would take a lot of time to reach long's length, but let's make sure we are infinite. =)
                if (GetTick() > 120000000)
                {
                    Stopwatch.Stop();
                    Stopwatch = new Stopwatch();
                    Stopwatch.Start();
                    newv = true;
                }
                foreach (CTimer timer in InsertAfterList)
                {
                    if (newv)
                    {
                        timer._executeAtMs = timer.ExecuteAfterMs;
                    }

                    timer.InsertSorted();
                }
                InsertAfterList.Clear();
            }
        }
    }
}