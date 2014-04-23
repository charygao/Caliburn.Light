﻿using System;
using System.Threading.Tasks;

namespace Caliburn.Light
{
    /// <summary>
    /// A couroutine that encapsulates an <see cref="System.Threading.Tasks.Task"/>.
    /// </summary>
    internal class TaskDecoratorCoTask : CoTask
    {
        private readonly Task _innerTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskDecoratorCoTask"/> class.
        /// </summary>
        /// <param name="task">The task.</param>
        public TaskDecoratorCoTask(Task task)
        {
            if (task == null)
                throw new ArgumentNullException("task");

            _innerTask = task;
        }

        /// <summary>
        /// Executes the coTask using the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public override void BeginExecute(CoroutineExecutionContext context)
        {
            if (_innerTask.IsCompleted)
                OnCompleted(_innerTask);
            else
                _innerTask.ContinueWith(OnCompleted,
                    UIContext.CheckAccess() ? UIContext.TaskScheduler : TaskScheduler.Current);
        }

        /// <summary>
        /// Called when the asynchronous task has completed.
        /// </summary>
        /// <param name="task">The completed task.</param>
        protected virtual void OnCompleted(Task task)
        {
            OnCompleted(new CoTaskCompletedEventArgs(task.Exception, task.IsCanceled));
        }
    }

    /// <summary>
    /// A couroutine that encapsulates an <see cref="System.Threading.Tasks.Task&lt;TResult&gt;"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of the coTask.</typeparam>
    internal sealed class TaskDecoratorCoTask<TResult> : TaskDecoratorCoTask, ICoTask<TResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskDecoratorCoTask&lt;TResult&gt;"/> class.
        /// </summary>
        /// <param name="task">The task.</param>
        public TaskDecoratorCoTask(Task<TResult> task)
            : base(task)
        {
        }

        /// <summary>
        /// Gets the coTask of the asynchronous operation.
        /// </summary>
        public TResult Result { get; private set; }

        /// <summary>
        /// Called when the asynchronous task has completed.
        /// </summary>
        /// <param name="task">The completed task.</param>
        protected override void OnCompleted(Task task)
        {
            if (!task.IsFaulted && !task.IsCanceled)
                Result = ((Task<TResult>) task).Result;

            base.OnCompleted(task);
        }
    }
}
