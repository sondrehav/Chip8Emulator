#define DEBUG

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chip8.Control
{

    public class GLControlThreaded : GLControl
    {

        private static readonly object GLEventOnLoop = new object();
        private static readonly object GLEventOnInit = new object();
        private static readonly object GLEventOnCleanUp = new object();

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
#if DEBUG
            Console.WriteLine("init, thread " + Thread.CurrentThread.ManagedThreadId);
#endif
            Start();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            running = false;
            handleDestroy = base.OnHandleDestroyed;
            handleDestroyArgs = e;
#if DEBUG
            Console.WriteLine("destroyed, thread " + Thread.CurrentThread.ManagedThreadId);
#endif
        }

        private Action<EventArgs> handleDestroy;
        private EventArgs handleDestroyArgs;

        private bool running = false;

        private void Start()
        {
            running = true;
            if (this.Context.IsCurrent) this.Context.MakeCurrent(null);
            new Thread(Run).Start();
        }

        private Queue<Action> actionQueue = new Queue<Action>();

        private object lock_ = new object();

        /// <summary>
        /// Invokes an action in a thread safe manner. 
        /// </summary>
        /// <param name="action">The action to be invoked.</param>
        public void InvokeAction(Action action)
        {
            lock (lock_)
            {
                this.actionQueue.Enqueue(action);
                Monitor.Pulse(lock_);
            }
        }

        #region events

        /// <summary>
        /// Adds event that is called on initialization.
        /// 
        /// This method is called on it's own thread.
        /// </summary>
        /// 
        public event EventHandler GLInit
        {
            add { this.Events.AddHandler(GLEventOnInit, (Delegate) value); }
            remove { this.Events.RemoveHandler(GLEventOnInit, (Delegate) value); }
        }


        /// <summary>
        /// Adds event that is called on each loop iteration.
        /// 
        /// This method is called on it's own thread.
        /// </summary>
        public event EventHandler GLLoop
        {
            add { this.Events.AddHandler(GLEventOnLoop, (Delegate) value); }
            remove { this.Events.RemoveHandler(GLEventOnLoop, (Delegate) value); }
        }

        /// <summary>
        /// Adds event that is called on the cleanup of the GL control. Deleting of shaders, buffers etc. is done here.
        /// 
        /// This method is called on it's own thread.
        /// </summary>
        public event EventHandler GLCleanUp
        {
            add { this.Events.AddHandler(GLEventOnCleanUp, (Delegate) value); }
            remove { this.Events.RemoveHandler(GLEventOnCleanUp, (Delegate) value); }
        }

        private void OnInit(EventArgs e)
        {
            EventHandler eventHandler = (EventHandler) this.Events[GLEventOnInit];
            if (eventHandler != null) eventHandler((object) this, e);
        }

        private void OnLoop(EventArgs e)
        {
            EventHandler eventHandler = (EventHandler) this.Events[GLEventOnLoop];
            if (eventHandler != null) eventHandler((object) this, e);
        }

        private void OnCleanUp(EventArgs e)
        {
            EventHandler eventHandler = (EventHandler) this.Events[GLEventOnCleanUp];
            if (eventHandler != null) eventHandler((object) this, e);
        }

        #endregion

        private float fpsCap = 60.0f;

        /// <summary>
        /// Sets the maximum fps.
        /// </summary>
        [Description("Sets a limit of the frames per second for the loop.")]
        public float FPSCap
        {
            get { return fpsCap; }
            set { fpsCap = value; }
        }

        private float currentFps;

        /// <summary>
        /// The actual fps in the running loop.
        /// </summary>
        [Description("Gets the frames per seconds for the control loop.")]
        public float FPS
        {
            get { return currentFps; }
        }

        private void Run()
        {

            if (!running) throw new Exception("Can't run GL window.");
            if (!this.Context.IsCurrent) this.Context.MakeCurrent(this.WindowInfo);

#if DEBUG
            Console.WriteLine("started, thread " + Thread.CurrentThread.ManagedThreadId);
#endif

            OnInit(EventArgs.Empty);

            Stopwatch timer = new Stopwatch();

            while (running)
            {

                timer.Restart();

                QueuedActions();

                // Run gameloop here
#if DEBUG
                //Console.WriteLine("loop, thread " + Thread.CurrentThread.ManagedThreadId);
#endif
                OnLoop(EventArgs.Empty);

                // ...


                // Do not need to wait if action is added while sleeping
                long timeToWait;
                while ((timeToWait = (long) (1000f/fpsCap) - timer.ElapsedMilliseconds) > 0)
                {
                    QueuedActions();
                    lock (lock_)
                    {
                        Monitor.Wait(lock_, (int) timeToWait);
                    }
                }

                currentFps = 1000.0f/(float) timer.ElapsedMilliseconds;

            }
            QueuedActions();
            OnCleanUp(EventArgs.Empty);

            handleDestroy(handleDestroyArgs);
#if DEBUG
            Console.WriteLine("destroyed, thread " + Thread.CurrentThread.ManagedThreadId);
#endif

        }

        private Queue<Action> preparedActions = new Queue<Action>();

        private void QueuedActions()
        {
            if (this.actionQueue.Count > 0)
            {
                lock (lock_)
                {
                    while (this.actionQueue.Count > 0)
                    {
                        // Put actions in new queue so the actionQueue is ready as soon as possible
                        preparedActions.Enqueue(this.actionQueue.Dequeue());
                    }
                }
            }

            while (preparedActions.Count > 0) preparedActions.Dequeue().Invoke();
        }


        // Used for callbacks in right threads
        private EventQueue eventQueue = new EventQueue();

        public EventQueue CallbackQueue
        {
            get { return eventQueue; }
        }

        public class EventQueue
        {
            private Queue<Action> events = new Queue<Action>();

            public Action Poll()
            {
                if (events.Count > 0) return events.Dequeue();
                return null;
            }

            public void Push(Action e)
            {
                events.Enqueue(e);
            }

            public bool IsEmpty()
            {
                if (events.Count <= 0) return true;
                return false;
            }

        }

    }

}
