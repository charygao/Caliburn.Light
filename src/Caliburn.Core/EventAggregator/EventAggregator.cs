﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Weakly;

namespace Caliburn.Light
{
    /// <summary>
    /// Enables loosely-coupled publication of and subscription to events.
    /// </summary>
    public sealed class EventAggregator : IEventAggregator
    {
        private readonly NamedObject _staticTarget = new NamedObject("StaticTarget");
        private readonly List<IEventAggregatorHandler> _handlers = new List<IEventAggregatorHandler>();

        // ReSharper disable once UnusedParameter.Local
        private static void VerifyTarget(object target)
        {
            if (target == null)
                throw new ArgumentNullException("target");
        }

        // ReSharper disable once UnusedParameter.Local
        private static void VerifyDelegate(Delegate weakHandler)
        {
            if (weakHandler == null)
                throw new ArgumentNullException("weakHandler");
            if (weakHandler.Target != null)
                throw new ArgumentException("An instance method or closure cannot be used to subscribe.", "weakHandler");
        }

        private void AddHandler(IEventAggregatorHandler handler)
        {
            lock (_handlers)
            {
                _handlers.RemoveAll(h => h.IsDead);
                _handlers.Add(handler);
            }
        }

        /// <summary>
        /// Subscribes the specified handler for messages of type <typeparamref name="TMessage" />.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="weakHandler">The message handler to register.</param>
        /// <param name="threadOption">Specifies on which Thread the <paramref name="weakHandler" /> is executed.</param>
        /// <returns>The <see cref="IEventAggregatorHandler" />.</returns>
        public IEventAggregatorHandler Subscribe<TMessage>(Action<TMessage> weakHandler, ThreadOption threadOption)
        {
            VerifyDelegate(weakHandler);

            var handler = new EventAggregatorHandler<object, TMessage>(_staticTarget, (t, m) => weakHandler(m), threadOption);
            AddHandler(handler);
            return handler;
        }

        /// <summary>
        /// Subscribes the specified handler for messages of type <typeparamref name="TMessage" />.
        /// </summary>
        /// <typeparam name="TTarget">The type of the handler target.</typeparam>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="target">The message handler target.</param>
        /// <param name="weakHandler">The message handler to register.</param>
        /// <param name="threadOption">Specifies on which Thread the <paramref name="weakHandler" /> is executed.</param>
        /// <returns>The <see cref="IEventAggregatorHandler" />.</returns>
        public IEventAggregatorHandler Subscribe<TTarget, TMessage>(TTarget target, Action<TTarget, TMessage> weakHandler, ThreadOption threadOption)
            where TTarget : class
        {
            VerifyTarget(target);
            VerifyDelegate(weakHandler);

            var handler = new EventAggregatorHandler<TTarget, TMessage>(target, weakHandler, threadOption);
            AddHandler(handler);
            return handler;
        }

        /// <summary>
        /// Subscribes the specified handler for messages of type <typeparamref name="TMessage" />.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="weakHandler">The message handler to register.</param>
        /// <param name="threadOption">Specifies on which Thread the <paramref name="weakHandler" /> is executed.</param>
        /// <returns>The <see cref="IEventAggregatorHandler" />.</returns>
        public IEventAggregatorHandler Subscribe<TMessage>(Func<TMessage, Task> weakHandler, ThreadOption threadOption)
        {
            VerifyDelegate(weakHandler);

            var handler = new EventAggregatorHandler<object, TMessage>(_staticTarget, (t, m) => weakHandler(m).ObserveException().Watch(), threadOption);
            AddHandler(handler);
            return handler;
        }

        /// <summary>
        /// Subscribes the specified handler for messages of type <typeparamref name="TMessage" />.
        /// </summary>
        /// <typeparam name="TTarget">The type of the handler target.</typeparam>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="target">The message handler target.</param>
        /// <param name="weakHandler">The message handler to register.</param>
        /// <param name="threadOption">Specifies on which Thread the <paramref name="weakHandler" /> is executed.</param>
        /// <returns>The <see cref="IEventAggregatorHandler" />.</returns>
        public IEventAggregatorHandler Subscribe<TTarget, TMessage>(TTarget target, Func<TTarget, TMessage, Task> weakHandler, ThreadOption threadOption)
            where TTarget : class
        {
            VerifyTarget(target);
            VerifyDelegate(weakHandler);

            var handler = new EventAggregatorHandler<TTarget, TMessage>(target, (t, m) => weakHandler(t, m).ObserveException().Watch(), threadOption);
            AddHandler(handler);
            return handler;
        }

        /// <summary>
        /// Unsubscribes the specified handler.
        /// </summary>
        /// <param name="handler">The handler to unsubscribe.</param>
        public void Unsubscribe(IEventAggregatorHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            lock (_handlers)
            {
                _handlers.RemoveAll(h => h.IsDead || ReferenceEquals(h, handler));
            }
        }

        /// <summary>
        /// Publishes a message.
        /// </summary>
        /// <param name="message">The message instance.</param>
        public void Publish(object message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            List<IEventAggregatorHandler> selectedHandlers;
            lock (_handlers)
            {
                _handlers.RemoveAll(h => h.IsDead);
                selectedHandlers = _handlers.FindAll(h => h.CanHandle(message));
            }

            if (selectedHandlers.Count == 0) return;
            var isUIThread = UIContext.CheckAccess();

            selectedHandlers
                .Where(h => h.ThreadOption == ThreadOption.PublisherThread ||
                            isUIThread && h.ThreadOption == ThreadOption.UIThread)
                .ForEach(h => h.Handle(message));

            if (!isUIThread)
            {
                var uiThreadHandlers = selectedHandlers.FindAll(h => h.ThreadOption == ThreadOption.UIThread);
                if (uiThreadHandlers.Count > 0)
                    UIContext.Run(() => uiThreadHandlers.ForEach(h => h.Handle(message))).ObserveException();
            }

            var backgroundHandlers = selectedHandlers.FindAll(h => h.ThreadOption == ThreadOption.BackgroundThread);
            if (backgroundHandlers.Count > 0)
                Task.Run(() => backgroundHandlers.ForEach(h => h.Handle(message))).ObserveException();
        }
    }
}
